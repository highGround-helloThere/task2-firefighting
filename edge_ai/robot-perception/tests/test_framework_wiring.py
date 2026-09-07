import sys
import unittest
from pathlib import Path

import yaml


REPOSITORY_ROOT = Path(__file__).resolve().parents[3]
PROJECT_ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(PROJECT_ROOT))


class ExistingFrameworkWiringTests(unittest.TestCase):
    def test_no_extra_control_or_perception_port_was_added(self):
        perception_config = yaml.safe_load(
            (PROJECT_ROOT / "config.yaml").read_text(encoding="utf-8")
        )
        gateway_config = yaml.safe_load(
            (
                REPOSITORY_ROOT
                / "autonomy"
                / "robot-edge-gateway"
                / "config.yaml"
            ).read_text(encoding="utf-8")
        )
        self.assertEqual(6101, perception_config["telemetry"]["udp"]["destination_port"])
        self.assertEqual(5075, gateway_config["control_proxy"]["listen_port"])
        self.assertEqual(5075, gateway_config["control_proxy"]["upstream_port"])
        self.assertTrue(gateway_config["safety"]["transparent_forwarding_only"])
        self.assertFalse(gateway_config["safety"]["generated_motion_commands_allowed"])

    def test_unity_reuses_robot_sync_manager(self):
        message_source = (
            REPOSITORY_ROOT
            / "unity"
            / "Perception"
            / "PerceptionTelemetryMessage.cs"
        ).read_text(encoding="utf-8")
        robot_source = (REPOSITORY_ROOT / "unity" / "RobotSyncManager.cs").read_text(
            encoding="utf-8"
        )
        bridge_source = (
            REPOSITORY_ROOT / "unity" / "Perception" / "AutonomyControlBridge.cs"
        ).read_text(encoding="utf-8")
        self.assertIn("public float auto_v;", message_source)
        self.assertIn("ApplyAutonomousTelemetry", robot_source)
        self.assertIn("autonomyWatchdogSeconds", robot_source)
        self.assertIn("PerceptionUdpReceiver", bridge_source)
        self.assertIn("RobotSyncManager", bridge_source)
        self.assertNotIn("6102", bridge_source)
        self.assertNotIn("6103", bridge_source)


if __name__ == "__main__":
    unittest.main()
