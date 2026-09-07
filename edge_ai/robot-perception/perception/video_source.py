"""Reconnecting OpenCV reader for the TonyPi MJPEG endpoint."""

import time

import cv2


class MjpegVideoSource:
    def __init__(self, config):
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

    @property
    def url(self):
        return self.urls[self._url_index]

    def _advance_url(self):
        self._url_index = (self._url_index + 1) % len(self.urls)

    def _open(self):
        self.close()
        self._capture = cv2.VideoCapture(self.url)
        self._capture.set(cv2.CAP_PROP_BUFFERSIZE, 1)
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
