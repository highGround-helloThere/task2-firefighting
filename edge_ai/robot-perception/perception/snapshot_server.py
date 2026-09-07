"""HTTP snapshot server backed by the latest Orange Pi perception frame."""

import json
import threading
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import parse_qs, urlparse

import cv2


class LatestJpegFrame:
    def __init__(self, jpeg_quality=85):
        quality = int(jpeg_quality)
        if not 1 <= quality <= 100:
            raise ValueError("jpeg_quality must be between 1 and 100")
        self._encode_parameters = [int(cv2.IMWRITE_JPEG_QUALITY), quality]
        self._condition = threading.Condition()
        self._jpeg = None
        self._version = 0

    def publish(self, frame):
        ok, encoded = cv2.imencode(".jpg", frame, self._encode_parameters)
        if not ok:
            return False
        data = encoded.tobytes()
        with self._condition:
            self._jpeg = data
            self._version += 1
            self._condition.notify_all()
        return True

    def get(self):
        with self._condition:
            return self._jpeg

    def wait_for_next(self, last_version, timeout=1.0):
        with self._condition:
            self._condition.wait_for(
                lambda: self._version > last_version,
                timeout=timeout,
            )
            return self._version, self._jpeg

    def wake_all(self):
        with self._condition:
            self._condition.notify_all()


class SnapshotServer:
    def __init__(self, config):
        self.enabled = bool(config.get("enabled", False))
        self.listen_host = str(config.get("listen_host", "0.0.0.0"))
        self.listen_port = int(config.get("listen_port", 8080))
        if not 0 <= self.listen_port <= 65535:
            raise ValueError("snapshot listen_port is invalid")
        self.frames = LatestJpegFrame(config.get("jpeg_quality", 85))
        self._server = None
        self._thread = None
        self._stop_event = threading.Event()

    @property
    def bound_port(self):
        return self._server.server_address[1] if self._server is not None else None

    def start(self):
        if not self.enabled or self._server is not None:
            return

        frames = self.frames
        stop_event = self._stop_event

        class Handler(BaseHTTPRequestHandler):
            def do_GET(self):
                parsed = urlparse(self.path)
                query = parse_qs(parsed.query)
                if parsed.path == "/health":
                    available = frames.get() is not None
                    data = json.dumps({"video_available": available}).encode("utf-8")
                    self.send_response(200)
                    self.send_header("Content-Type", "application/json")
                    self.send_header("Content-Length", str(len(data)))
                    self.end_headers()
                    self.wfile.write(data)
                    return

                if parsed.path != "/":
                    self.send_error(404)
                    return

                action = query.get("action")
                if action == ["stream"]:
                    self._serve_stream()
                    return
                if action != ["snapshot"]:
                    self.send_error(404)
                    return

                jpeg = frames.get()
                if jpeg is None:
                    self.send_error(503, "video frame unavailable")
                    return

                self.send_response(200)
                self.send_header("Content-Type", "image/jpeg")
                self.send_header("Content-Length", str(len(jpeg)))
                self.send_header("Cache-Control", "no-store")
                self.end_headers()
                self.wfile.write(jpeg)

            def _serve_stream(self):
                self.send_response(200)
                self.send_header(
                    "Content-Type",
                    "multipart/x-mixed-replace; boundary=frame",
                )
                self.send_header("Cache-Control", "no-store")
                self.send_header("Connection", "close")
                self.end_headers()

                last_version = -1
                try:
                    while not stop_event.is_set():
                        version, jpeg = frames.wait_for_next(last_version, timeout=1.0)
                        if version == last_version or jpeg is None:
                            continue

                        last_version = version
                        header = (
                            b"--frame\r\n"
                            b"Content-Type: image/jpeg\r\n"
                            + "Content-Length: {}\r\n\r\n".format(len(jpeg)).encode("ascii")
                        )
                        self.wfile.write(header)
                        self.wfile.write(jpeg)
                        self.wfile.write(b"\r\n")
                        self.wfile.flush()
                except (BrokenPipeError, ConnectionResetError, OSError):
                    return

            def log_message(self, _format, *_args):
                return

        self._server = ThreadingHTTPServer((self.listen_host, self.listen_port), Handler)
        self._thread = threading.Thread(
            target=self._server.serve_forever,
            name="Perception-Snapshot-Server",
            daemon=True,
        )
        self._thread.start()

    def publish(self, frame):
        if not self.enabled:
            return False
        return self.frames.publish(frame)

    def close(self):
        if self._server is not None:
            self._stop_event.set()
            self.frames.wake_all()
            self._server.shutdown()
            self._server.server_close()
        if self._thread is not None:
            self._thread.join(timeout=2.0)
        self._server = None
        self._thread = None
        self._stop_event.clear()
