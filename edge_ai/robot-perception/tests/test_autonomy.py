import sys
import unittest
from pathlib import Path

import cv2
import numpy as np


PROJECT_ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(PROJECT_ROOT))

from perception.autonomy import CameraAutonomyController  # noqa: E402


def controller(**overrides):
    config = {
        "enabled": True,
        "processing_width": 320,
        "processing_height": 240,
        "clearance_threshold": 0.45,
        "minimum_motion_area_ratio": 0.005,
        "maximum_motion_area_ratio": 0.35,
        "extinguish_retry_frames": 5,
    }
    config.update(overrides)
    return CameraAutonomyController(config)


def blank_frame():
    return np.zeros((480, 640, 3), dtype=np.uint8)


class CameraAutonomyTests(unittest.TestCase):
    def test_disabled_controller_emits_safe_zero_command(self):
        result = CameraAutonomyController({"enabled": False}).process(
            blank_frame(), {"detected": False}
        )
        self.assertFalse(result["autonomy_valid"])
        self.assertEqual(0.0, result["auto_v"])
        self.assertEqual(0.0, result["auto_steer"])

    def test_processes_the_existing_camera_frame_without_a_map(self):
        result = controller().process(blank_frame(), {"detected": False})
        self.assertTrue(result["autonomy_valid"])
        self.assertIn(
            result["auto_state"],
            {"explore_turn_left", "explore_forward", "explore_turn_right"},
        )
        self.assertNotIn("junction_id", result)

    def test_uses_existing_fire_detector_geometry(self):
        fire = {
            "detected": True,
            "center_x": 100.0,
            "center_y": 240.0,
            "radius": 20.0,
            "frame_width": 640,
            "frame_height": 480,
        }
        result = controller().process(blank_frame(), fire)
        self.assertEqual("align_fire_left", result["auto_state"])
        self.assertLess(result["auto_steer"], 0.0)

    def test_extinguishing_is_a_rate_limited_one_shot(self):
        fire = {
            "detected": True,
            "center_x": 320.0,
            "center_y": 240.0,
            "radius": 80.0,
            "frame_width": 640,
            "frame_height": 480,
        }
        autonomy = controller()
        first = autonomy.process(blank_frame(), fire)
        second = autonomy.process(blank_frame(), fire)
        self.assertTrue(first["auto_extinguish"])
        self.assertFalse(second["auto_extinguish"])

    def test_detects_localized_motion_and_turns_away(self):
        autonomy = controller()
        autonomy.process(blank_frame(), {"detected": False})
        moving_frame = blank_frame()
        cv2.rectangle(moving_frame, (20, 200), (180, 420), (255, 255, 255), -1)
        result = autonomy.process(moving_frame, {"detected": False})
        self.assertTrue(result["dynamic_obstacle"])
        self.assertEqual("left", result["obstacle_direction"])
        self.assertGreaterEqual(result["auto_steer"], 0.0)


if __name__ == "__main__":
    unittest.main()
