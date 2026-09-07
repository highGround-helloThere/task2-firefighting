import sys
import time
import unittest
from pathlib import Path
from unittest.mock import patch

import numpy as np


PROJECT_ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(PROJECT_ROOT))

import main as perception_main  # noqa: E402


class FakeSource:
    def __init__(self, _config):
        self.closed = False

    def read(self):
        return time.time(), np.zeros((480, 640, 3), dtype=np.uint8)

    def close(self):
        self.closed = True


class FakeFireDetector:
    def process(self, _frame):
        return {
            "detected": False,
            "candidate_detected": False,
            "target": "red_ball",
            "frame_width": 640,
            "frame_height": 480,
        }


class FakeTelemetry:
    def __init__(self, _config):
        self.detections = []

    def emit_event(self, *_args, **_kwargs):
        pass

    def emit_detection(self, result):
        self.detections.append(dict(result))


class FakeUdpTelemetry(FakeTelemetry):
    latest_instance = None

    def __init__(self, config):
        super().__init__(config)
        FakeUdpTelemetry.latest_instance = self

    def emit_status(self, *_args, **_kwargs):
        pass

    def close(self):
        pass


class FakeSnapshotServer:
    def __init__(self, _config):
        pass

    def start(self):
        pass

    def publish(self, _frame):
        pass

    def close(self):
        pass


class MainAutonomyIntegrationTests(unittest.TestCase):
    def test_course_main_adds_autonomy_to_existing_detection_result(self):
        config = {
            "project": {"mode": "test"},
            "video": {"url": "fake"},
            "video_proxy": {},
            "detection": {},
            "autonomy": {"enabled": True},
            "telemetry": {},
        }
        perception_main.STOP_REQUESTED = False
        with (
            patch.object(perception_main, "MjpegVideoSource", FakeSource),
            patch.object(
                perception_main, "build_detector", lambda _config: FakeFireDetector()
            ),
            patch.object(perception_main, "JsonLineTelemetry", FakeTelemetry),
            patch.object(perception_main, "UdpPerceptionTelemetry", FakeUdpTelemetry),
            patch.object(perception_main, "SnapshotServer", FakeSnapshotServer),
        ):
            processed = perception_main.run(config, max_frames=1)

        self.assertEqual(1, processed)
        sent = FakeUdpTelemetry.latest_instance.detections[0]
        self.assertIn("autonomy_valid", sent)
        self.assertIn("auto_v", sent)
        self.assertIn("auto_steer", sent)
        self.assertNotIn("junction_id", sent)


if __name__ == "__main__":
    unittest.main()
