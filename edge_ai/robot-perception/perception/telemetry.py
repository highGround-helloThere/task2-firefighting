"""Rate-limited JSON Lines telemetry written to stdout."""

import json
import sys
import time


class JsonLineTelemetry:
    def __init__(self, config, stream=None):
        self.minimum_interval = float(config.get("minimum_interval_seconds", 0.25))
        self.heartbeat_interval = float(config.get("heartbeat_interval_seconds", 2.0))
        self.stream = stream or sys.stdout
        self._last_detection_emit = 0.0
        self._last_heartbeat_emit = 0.0
        self._last_detected = None
        self._last_event_emit = {}

    def _write(self, payload):
        self.stream.write(json.dumps(payload, ensure_ascii=False, sort_keys=True) + "\n")
        self.stream.flush()

    def emit_event(self, event, fields, rate_limit_key=None):
        now = time.monotonic()
        if rate_limit_key:
            last = self._last_event_emit.get(rate_limit_key, 0.0)
            if now - last < self.heartbeat_interval:
                return
            self._last_event_emit[rate_limit_key] = now
        payload = {"event": event, "timestamp": time.time()}
        payload.update(fields)
        self._write(payload)

    def emit_detection(self, result):
        now = time.monotonic()
        detected = bool(result.get("detected"))
        state_changed = self._last_detected is None or detected != self._last_detected
        interval_elapsed = now - self._last_detection_emit >= self.minimum_interval
        heartbeat_due = now - self._last_heartbeat_emit >= self.heartbeat_interval
        if not (state_changed or interval_elapsed or heartbeat_due):
            return
        self._write(result)
        self._last_detection_emit = now
        if heartbeat_due:
            self._last_heartbeat_emit = now
        self._last_detected = detected
