import json
import sys
import unittest
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(PROJECT_ROOT))

from perception.udp_telemetry import UdpPerceptionTelemetry  # noqa: E402


class FakeSocket:
    def __init__(self):
        self.sent = []

    def sendto(self, data, destination):
        self.sent.append((data, destination))


class UdpAutonomyFieldsTests(unittest.TestCase):
    def test_reuses_course_udp_6101_packet(self):
        fake = FakeSocket()
        telemetry = UdpPerceptionTelemetry(
            {
                "udp": {
                    "enabled": True,
                    "destination_host": "192.168.137.1",
                    "destination_port": 6101,
                }
            },
            sock=fake,
            session_id="test-session",
        )
        telemetry.emit_detection(
            {
                "detected": False,
                "autonomy_valid": True,
                "auto_v": 0.45,
                "auto_steer": 0.0,
                "auto_extinguish": False,
                "auto_state": "explore_forward",
                "dynamic_obstacle": False,
                "obstacle_direction": "none",
                "clearance_left": 0.5,
                "clearance_front": 0.8,
                "clearance_right": 0.4,
            },
            force=True,
        )
        self.assertEqual(1, len(fake.sent))
        data, destination = fake.sent[0]
        self.assertEqual(("192.168.137.1", 6101), destination)
        packet = json.loads(data)
        self.assertEqual("perception", packet["type"])
        self.assertEqual(1, packet["schema_version"])
        self.assertTrue(packet["autonomy_valid"])
        self.assertEqual(0.45, packet["auto_v"])
        self.assertEqual("explore_forward", packet["auto_state"])


if __name__ == "__main__":
    unittest.main()
