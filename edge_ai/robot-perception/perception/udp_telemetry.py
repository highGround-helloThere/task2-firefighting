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
        if not 1 <= self.destination_port <= 65535:
            raise ValueError("telemetry udp destination_port is invalid")

        self.minimum_interval = 1.0 / send_hz
        self.session_id = session_id or uuid.uuid4().hex
        self._socket = sock
        self._owns_socket = sock is None
        self._last_emit = 0.0
        self._sequence = 0
        self._pending_extinguish = False
        self.last_error = None

        if self.enabled and self._socket is None:
            self._socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    @staticmethod
    def _number(value, default=-1.0):
        return float(value) if value is not None else float(default)

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

    def emit_detection(self, result, force=False):
        detected = bool(result.get("detected"))
        candidate = bool(result.get("candidate_detected"))
        self._pending_extinguish = (
            self._pending_extinguish
            or bool(result.get("auto_extinguish", False))
        )
        state = "detected" if detected else ("candidate" if candidate else "clear")
        payload = {
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
                result.get("processing_delay_ms"), 0.0
            ),
            "autonomy_valid": bool(result.get("autonomy_valid", False)),
            "auto_v": self._number(result.get("auto_v"), 0.0),
            "auto_steer": self._number(result.get("auto_steer"), 0.0),
            "auto_extinguish": self._pending_extinguish,
            "auto_state": str(result.get("auto_state", "disabled")),
            "dynamic_obstacle": bool(result.get("dynamic_obstacle", False)),
            "obstacle_direction": str(result.get("obstacle_direction", "none")),
            "motion_ratio": self._number(result.get("motion_ratio"), 0.0),
            "clearance_left": self._number(result.get("clearance_left"), 0.0),
            "clearance_front": self._number(result.get("clearance_front"), 0.0),
            "clearance_right": self._number(result.get("clearance_right"), 0.0),
        }
        sent = self._send(payload, force=force)
        if sent and payload["auto_extinguish"]:
            self._pending_extinguish = False
        return sent

    def emit_status(self, state, video_ok, force=False):
        # Never carry a stale action across startup, video loss, or shutdown.
        self._pending_extinguish = False
        return self._send(
            {
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
                "autonomy_valid": False,
                "auto_v": 0.0,
                "auto_steer": 0.0,
                "auto_extinguish": False,
                "auto_state": str(state),
                "dynamic_obstacle": False,
                "obstacle_direction": "none",
                "motion_ratio": 0.0,
                "clearance_left": 0.0,
                "clearance_front": 0.0,
                "clearance_right": 0.0,
            },
            force=force,
        )

    def close(self):
        if self._owns_socket and self._socket is not None:
            self._socket.close()
        self._socket = None
