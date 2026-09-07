"""Camera-based autonomous navigation that runs inside robot-perception.

It consumes the same TonyPi camera frame already opened by main.py and returns
control fields that are appended to the existing UDP 6101 telemetry packet.
No map, checkpoint coordinate, extra socket, or external perception schema is
required.
"""

import cv2
import numpy as np


class CameraAutonomyController:
    def __init__(self, config):
        self.enabled = bool(config.get("enabled", False))
        self.processing_width = int(config.get("processing_width", 320))
        self.processing_height = int(config.get("processing_height", 240))
        self.roi_top_ratio = float(config.get("roi_top_ratio", 0.50))
        self.blocked_edge_density = float(
            config.get("blocked_edge_density", 0.16)
        )
        self.clearance_threshold = float(config.get("clearance_threshold", 0.45))
        self.motion_pixel_threshold = int(config.get("motion_pixel_threshold", 28))
        self.minimum_motion_area_ratio = float(
            config.get("minimum_motion_area_ratio", 0.008)
        )
        self.maximum_motion_area_ratio = float(
            config.get("maximum_motion_area_ratio", 0.35)
        )
        self.motion_compensation = bool(config.get("motion_compensation", True))
        self.fire_left_boundary = float(config.get("fire_left_boundary", 0.42))
        self.fire_right_boundary = float(config.get("fire_right_boundary", 0.58))
        self.extinguish_radius_ratio = float(
            config.get("extinguish_radius_ratio", 0.12)
        )
        self.forward_speed = float(config.get("forward_speed", 0.45))
        self.backward_speed = float(config.get("backward_speed", -0.30))
        self.turn_speed = float(config.get("turn_speed", 0.55))
        self.extinguish_retry_frames = max(
            1, int(config.get("extinguish_retry_frames", 30))
        )

        self._previous_gray = None
        self._extinguish_cooldown = 0

    def process(self, frame, fire_result):
        if not self.enabled:
            return self._disabled_result()
        if frame is None or frame.size == 0:
            return self._stop_result("video_unavailable")

        resized = cv2.resize(
            frame,
            (self.processing_width, self.processing_height),
            interpolation=cv2.INTER_AREA,
        )
        gray = cv2.cvtColor(resized, cv2.COLOR_BGR2GRAY)
        gray = cv2.GaussianBlur(gray, (5, 5), 0)

        clearances = self._measure_clearance(gray)
        dynamic_obstacle, obstacle_direction, motion_ratio = self._detect_motion(
            gray
        )
        decision = self._decide(
            fire_result,
            clearances,
            dynamic_obstacle,
            obstacle_direction,
        )
        decision.update(
            {
                "autonomy_valid": True,
                "dynamic_obstacle": dynamic_obstacle,
                "obstacle_direction": obstacle_direction,
                "motion_ratio": round(motion_ratio, 4),
                "clearance_left": round(clearances["left"], 4),
                "clearance_front": round(clearances["front"], 4),
                "clearance_right": round(clearances["right"], 4),
            }
        )
        return decision

    def _measure_clearance(self, gray):
        edges = cv2.Canny(gray, 60, 160)
        height, width = edges.shape
        top = max(0, min(height - 1, int(height * self.roi_top_ratio)))
        regions = {
            "left": edges[top:height, 0 : int(width * 0.38)],
            "front": edges[top:height, int(width * 0.31) : int(width * 0.69)],
            "right": edges[top:height, int(width * 0.62) : width],
        }
        clearances = {}
        for name, region in regions.items():
            density = float(np.count_nonzero(region)) / max(1, region.size)
            clearances[name] = max(
                0.0, min(1.0, 1.0 - density / self.blocked_edge_density)
            )
        return clearances

    def _detect_motion(self, gray):
        if self._previous_gray is None:
            self._previous_gray = gray
            return False, "none", 0.0

        previous = self._previous_gray
        if self.motion_compensation:
            previous = self._compensate_camera_motion(previous, gray)
        difference = cv2.absdiff(previous, gray)
        self._previous_gray = gray
        _, mask = cv2.threshold(
            difference, self.motion_pixel_threshold, 255, cv2.THRESH_BINARY
        )
        mask = cv2.morphologyEx(
            mask, cv2.MORPH_OPEN, np.ones((3, 3), dtype=np.uint8)
        )
        contours = cv2.findContours(
            mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE
        )[-2]
        frame_area = float(mask.shape[0] * mask.shape[1])
        candidates = []
        for contour in contours:
            area_ratio = float(cv2.contourArea(contour)) / max(1.0, frame_area)
            if self.minimum_motion_area_ratio <= area_ratio <= self.maximum_motion_area_ratio:
                x, _y, width, _height = cv2.boundingRect(contour)
                center_ratio = (x + width / 2.0) / mask.shape[1]
                candidates.append((area_ratio, center_ratio))

        if not candidates:
            return False, "none", 0.0
        area_ratio, center_ratio = max(candidates, key=lambda item: item[0])
        if center_ratio < 0.38:
            direction = "left"
        elif center_ratio > 0.62:
            direction = "right"
        else:
            direction = "front"
        return True, direction, area_ratio

    @staticmethod
    def _compensate_camera_motion(previous, current):
        points_previous = cv2.goodFeaturesToTrack(
            previous,
            maxCorners=120,
            qualityLevel=0.01,
            minDistance=7,
            blockSize=7,
        )
        if points_previous is None or len(points_previous) < 8:
            return previous
        points_current, status, _error = cv2.calcOpticalFlowPyrLK(
            previous, current, points_previous, None
        )
        if points_current is None or status is None:
            return previous
        valid = status.reshape(-1) == 1
        if np.count_nonzero(valid) < 8:
            return previous
        transform, _inliers = cv2.estimateAffinePartial2D(
            points_previous[valid],
            points_current[valid],
            method=cv2.RANSAC,
        )
        if transform is None:
            return previous
        return cv2.warpAffine(
            previous,
            transform,
            (current.shape[1], current.shape[0]),
            flags=cv2.INTER_LINEAR,
            borderMode=cv2.BORDER_REPLICATE,
        )

    def _decide(
        self, fire_result, clearances, dynamic_obstacle, obstacle_direction
    ):
        if self._extinguish_cooldown > 0:
            self._extinguish_cooldown -= 1

        if dynamic_obstacle:
            return self._avoid_dynamic(clearances, obstacle_direction)

        if bool(fire_result.get("detected", False)):
            fire_decision = self._follow_fire(fire_result, clearances)
            if fire_decision is not None:
                return fire_decision

        left_free = clearances["left"] >= self.clearance_threshold
        front_free = clearances["front"] >= self.clearance_threshold
        right_free = clearances["right"] >= self.clearance_threshold

        # Left-hand wall following works without a preloaded maze map.
        if left_free:
            return self._motion(0.0, -self.turn_speed, False, "explore_turn_left")
        if front_free:
            return self._motion(self.forward_speed, 0.0, False, "explore_forward")
        if right_free:
            return self._motion(0.0, self.turn_speed, False, "explore_turn_right")
        return self._motion(self.backward_speed, 0.0, False, "dead_end_back")

    def _follow_fire(self, fire_result, clearances):
        try:
            center_x = float(fire_result["center_x"])
            radius = float(fire_result["radius"])
            frame_width = float(fire_result["frame_width"])
            frame_height = float(fire_result["frame_height"])
        except (KeyError, TypeError, ValueError):
            return self._motion(0.0, 0.0, False, "fire_geometry_invalid")
        if frame_width <= 0.0 or frame_height <= 0.0:
            return self._motion(0.0, 0.0, False, "fire_geometry_invalid")

        horizontal = center_x / frame_width
        if horizontal < self.fire_left_boundary:
            return self._motion(0.0, -self.turn_speed, False, "align_fire_left")
        if horizontal > self.fire_right_boundary:
            return self._motion(0.0, self.turn_speed, False, "align_fire_right")

        if radius / min(frame_width, frame_height) >= self.extinguish_radius_ratio:
            should_extinguish = self._extinguish_cooldown == 0
            if should_extinguish:
                self._extinguish_cooldown = self.extinguish_retry_frames
            return self._motion(
                0.0, 0.0, should_extinguish, "extinguish_fire"
            )
        if clearances["front"] >= self.clearance_threshold:
            return self._motion(self.forward_speed, 0.0, False, "approach_fire")
        return self._motion(0.0, 0.0, False, "fire_path_blocked")

    def _avoid_dynamic(self, clearances, obstacle_direction):
        if obstacle_direction == "left" and clearances["right"] >= self.clearance_threshold:
            return self._motion(0.0, self.turn_speed, False, "avoid_dynamic_right")
        if obstacle_direction == "right" and clearances["left"] >= self.clearance_threshold:
            return self._motion(0.0, -self.turn_speed, False, "avoid_dynamic_left")
        if clearances["left"] >= clearances["right"] and clearances["left"] >= self.clearance_threshold:
            return self._motion(0.0, -self.turn_speed, False, "avoid_dynamic_left")
        if clearances["right"] >= self.clearance_threshold:
            return self._motion(0.0, self.turn_speed, False, "avoid_dynamic_right")
        return self._motion(0.0, 0.0, False, "wait_for_dynamic_obstacle")

    @staticmethod
    def _motion(v, steer, extinguish, state):
        return {
            "auto_v": float(v),
            "auto_steer": float(steer),
            "auto_extinguish": bool(extinguish),
            "auto_state": str(state),
        }

    @classmethod
    def _disabled_result(cls):
        result = cls._motion(0.0, 0.0, False, "disabled")
        result.update(
            {
                "autonomy_valid": False,
                "dynamic_obstacle": False,
                "obstacle_direction": "none",
                "motion_ratio": 0.0,
                "clearance_left": 0.0,
                "clearance_front": 0.0,
                "clearance_right": 0.0,
            }
        )
        return result

    @classmethod
    def _stop_result(cls, state):
        result = cls._disabled_result()
        result["auto_state"] = state
        return result


def build_autonomy(config):
    return CameraAutonomyController(config)
