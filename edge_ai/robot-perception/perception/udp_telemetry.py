"""One-way UDP perception telemetry for Unity.

The sender never listens for commands. Datagram loss is acceptable because the
latest perception state is sent repeatedly at a fixed rate.
"""

import json
import socket
import time
import uuid


class UdpPerceptionTelemetry:
    def __init__(self, telemetry_config, sock=None, session_id=None):
        config = telemetry_config.get("udp", {})
        self.enabled = bool(config.get("enabled", False))
        self.destination_host = str(config.get("destination_host", "127.0.0.1"))
        self.destination_port = int(config.get("destination_port", 6101))
        self.schema_version = int(config.get("schema_version", 1))
        self.source = str(config.get("source", "orange_pi"))
        send_hz = float(config.get("send_hz", 10.0))
        if send_hz <= 0:
            raise ValueError("telemetry udp send_hz must be greater than zero")
        if self.schema_version not in (1, 2):
            raise ValueError("telemetry udp schema_version must be 1 or 2")
        if not 1 <= self.destination_port <= 65535:
            raise ValueError("telemetry udp destination_port is invalid")

        self.minimum_interval = 1.0 / send_hz
        self.session_id = session_id or uuid.uuid4().hex
        self._socket = sock
        self._owns_socket = sock is None
        self._last_emit = 0.0
        self._sequence = 0
        self.last_error = None

        if self.enabled and self._socket is None:
            self._socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    @staticmethod
    def _number(value, default=-1.0):
        return float(value) if value is not None else float(default)

    @property
    def sequence(self):
        return self._sequence

    def _send(self, payload, force=False):
        if not self.enabled or self._socket is None:
            return False
        now = time.monotonic()
        if not force and now - self._last_emit < self.minimum_interval:
            return False

        self._sequence += 1
        payload.update(
            {
                "type": "perception",
                "schema_version": self.schema_version,
                "source": self.source,
                "session_id": self.session_id,
                "seq": self._sequence,
                "sent_at_ms": int(time.time() * 1000.0),
            }
        )
        data = json.dumps(
            payload, ensure_ascii=False, separators=(",", ":"), sort_keys=True
        ).encode("utf-8")
        try:
            self._socket.sendto(
                data, (self.destination_host, self.destination_port)
            )
        except OSError as exc:
            self.last_error = "{}: {}".format(type(exc).__name__, exc)
            return False

        self.last_error = None
        self._last_emit = now
        return True

    def _v1_detection_payload(self, result):
        detected = bool(result.get("detected"))
        candidate = bool(result.get("candidate_detected"))
        state = "detected" if detected else ("candidate" if candidate else "clear")
        return {
            "state": state,
            "video_ok": True,
            "detected": detected,
            "candidate_detected": candidate,
            "target": str(result.get("target", "red_ball")),
            "center_x": self._number(result.get("center_x")),
            "center_y": self._number(result.get("center_y")),
            "radius": self._number(result.get("radius")),
            "score": self._number(result.get("score"), 0.0),
            "circularity": self._number(result.get("circularity"), 0.0),
            "aspect_ratio": self._number(result.get("aspect_ratio"), 0.0),
            "frame_width": int(result.get("frame_width", 0)),
            "frame_height": int(result.get("frame_height", 0)),
            "processing_ms": self._number(
                result.get("processing_ms", result.get("processing_delay_ms")), 0.0
            ),
        }

    def _v2_detection_payload(self, result):
        return {
            "video_ok": True,
            "fire_detected": bool(
                result.get("fire_detected", result.get("detected", False))
            ),
            "fire_center_x": self._number(
                result.get("fire_center_x", result.get("center_x"))
            ),
            "fire_center_y": self._number(
                result.get("fire_center_y", result.get("center_y"))
            ),
            "fire_confidence": self._number(
                result.get("fire_confidence", result.get("score")), 0.0
            ),
            "fire_direction": str(result.get("fire_direction", "none")),
            "fire_aligned": bool(result.get("fire_aligned", False)),
            "dynamic_obstacle": bool(result.get("dynamic_obstacle", False)),
            "obstacle_direction": str(result.get("obstacle_direction", "none")),
            "obstacle_confidence": self._number(
                result.get("obstacle_confidence"), 0.0
            ),
            "free_left": bool(result.get("free_left", True)),
            "free_front": bool(result.get("free_front", True)),
            "free_right": bool(result.get("free_right", True)),
            "frame_width": int(result.get("frame_width", 0)),
            "frame_height": int(result.get("frame_height", 0)),
            "processing_ms": self._number(
                result.get("processing_ms", result.get("processing_delay_ms")), 0.0
            ),
        }

    def emit_detection(self, result, force=False):
        payload = (
            self._v1_detection_payload(result)
            if self.schema_version == 1
            else self._v2_detection_payload(result)
        )
        return self._send(payload, force=force)

    def emit_status(self, state, video_ok, force=False):
        if self.schema_version == 1:
            payload = {
                "state": str(state),
                "video_ok": bool(video_ok),
                "detected": False,
                "candidate_detected": False,
                "target": "red_ball",
                "center_x": -1.0,
                "center_y": -1.0,
                "radius": -1.0,
                "score": 0.0,
                "circularity": 0.0,
                "aspect_ratio": 0.0,
                "frame_width": 0,
                "frame_height": 0,
                "processing_ms": 0.0,
            }
        else:
            payload = {
                "video_ok": bool(video_ok),
                "fire_detected": False,
                "fire_center_x": -1.0,
                "fire_center_y": -1.0,
                "fire_confidence": 0.0,
                "fire_direction": "none",
                "fire_aligned": False,
                "dynamic_obstacle": False,
                "obstacle_direction": "none",
                "obstacle_confidence": 0.0,
                "free_left": True,
                "free_front": True,
                "free_right": True,
                "frame_width": 0,
                "frame_height": 0,
                "processing_ms": 0.0,
                "status": str(state),
            }
        return self._send(payload, force=force)

    def close(self):
        if self._owns_socket and self._socket is not None:
            self._socket.close()
        self._socket = None
