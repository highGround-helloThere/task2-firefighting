"""OpenCV video sources for MJPEG, local files, and webcams."""

import time

import cv2


def _new_capture(source, config):
    capture = cv2.VideoCapture()
    open_timeout = int(config.get("open_timeout_milliseconds", 3000))
    read_timeout = int(config.get("read_timeout_milliseconds", 2000))
    if hasattr(cv2, "CAP_PROP_OPEN_TIMEOUT_MSEC"):
        capture.set(cv2.CAP_PROP_OPEN_TIMEOUT_MSEC, open_timeout)
    if hasattr(cv2, "CAP_PROP_READ_TIMEOUT_MSEC"):
        capture.set(cv2.CAP_PROP_READ_TIMEOUT_MSEC, read_timeout)
    capture.open(source)
    capture.set(cv2.CAP_PROP_BUFFERSIZE, 1)
    return capture


class MjpegVideoSource:
    def __init__(self, config):
        self.config = config
        self.urls = [str(url) for url in config.get("urls", [config["url"]])]
        if not self.urls:
            raise ValueError("at least one video URL is required")
        self._url_index = 0
        self.reconnect_delay = float(config.get("reconnect_delay_seconds", 1.0))
        self.failure_limit = max(
            1, int(config.get("consecutive_read_failures_before_reconnect", 5))
        )
        self._capture = None
        self._failures = 0
        self.exhausted = False

    @property
    def url(self):
        return self.urls[self._url_index]

    def _advance_url(self):
        self._url_index = (self._url_index + 1) % len(self.urls)

    def _open(self):
        self.close()
        self._capture = _new_capture(self.url, self.config)
        if not self._capture.isOpened():
            self.close()
            self._advance_url()
            return False
        self._failures = 0
        return True

    def read(self):
        if self._capture is None and not self._open():
            time.sleep(self.reconnect_delay)
            return None
        ok, frame = self._capture.read()
        if ok and frame is not None:
            self._failures = 0
            return time.time(), frame
        self._failures += 1
        if self._failures >= self.failure_limit:
            self.close()
            self._advance_url()
            time.sleep(self.reconnect_delay)
        return None

    def close(self):
        if self._capture is not None:
            self._capture.release()
            self._capture = None


class LocalVideoSource:
    def __init__(self, config):
        self.config = config
        self.path = str(config.get("local_path", ""))
        if not self.path:
            raise ValueError("video local_path is required for local source")
        self.loop = bool(config.get("loop_local_video", False))
        self._capture = None
        self.exhausted = False

    def _open(self):
        self.close()
        self._capture = _new_capture(self.path, self.config)
        if not self._capture.isOpened():
            self.close()
            raise RuntimeError("cannot open local video: {}".format(self.path))

    def read(self):
        if self._capture is None:
            self._open()
        ok, frame = self._capture.read()
        if ok and frame is not None:
            return time.time(), frame
        if self.loop:
            self._capture.set(cv2.CAP_PROP_POS_FRAMES, 0)
            ok, frame = self._capture.read()
            if ok and frame is not None:
                return time.time(), frame
        self.exhausted = True
        return None

    def close(self):
        if self._capture is not None:
            self._capture.release()
            self._capture = None


class WebcamVideoSource:
    def __init__(self, config):
        self.config = config
        self.camera_index = int(config.get("camera_index", 0))
        self.reconnect_delay = float(config.get("reconnect_delay_seconds", 1.0))
        self._capture = None
        self.exhausted = False

    def _open(self):
        self.close()
        self._capture = _new_capture(self.camera_index, self.config)
        if not self._capture.isOpened():
            self.close()
            return False
        return True

    def read(self):
        if self._capture is None and not self._open():
            time.sleep(self.reconnect_delay)
            return None
        ok, frame = self._capture.read()
        if ok and frame is not None:
            return time.time(), frame
        self.close()
        time.sleep(self.reconnect_delay)
        return None

    def close(self):
        if self._capture is not None:
            self._capture.release()
            self._capture = None


def build_video_source(config):
    source = str(config.get("source", "mjpeg")).lower()
    if source == "mjpeg":
        return MjpegVideoSource(config)
    if source in ("local", "file"):
        return LocalVideoSource(config)
    if source in ("webcam", "camera"):
        return WebcamVideoSource(config)
    raise ValueError("unknown video source: {}".format(source))
