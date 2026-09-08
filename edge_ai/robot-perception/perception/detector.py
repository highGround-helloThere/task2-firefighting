"""Baseline and lightweight model-backed perception detectors."""

import time
from pathlib import Path

import cv2
import numpy as np


def _direction(center_x, width, left_ratio, right_ratio, centered="centered"):
    if center_x is None or width <= 0:
        return "none"
    if center_x < width * left_ratio:
        return "left"
    if center_x > width * right_ratio:
        return "right"
    return centered


class _StableTarget:
    """Small hit/miss filter that retains valid geometry while state is stable."""

    def __init__(self, confirmation_frames, release_frames, stale_seconds):
        self.confirmation_frames = max(1, int(confirmation_frames))
        self.release_frames = max(1, int(release_frames))
        self.stale_seconds = max(0.0, float(stale_seconds))
        self.hits = 0
        self.misses = 0
        self.stable = False
        self.last_seen = 0.0
        self.geometry = None

    def update(self, observation, now=None):
        now = time.monotonic() if now is None else now
        if observation is not None:
            self.hits += 1
            self.misses = 0
            self.last_seen = now
            self.geometry = observation
            if self.hits >= self.confirmation_frames:
                self.stable = True
        else:
            self.hits = 0
            self.misses += 1
            stale = self.stale_seconds > 0 and now - self.last_seen > self.stale_seconds
            if self.misses >= self.release_frames or stale:
                self.stable = False
                self.geometry = None
        return self.stable, self.geometry


class _FreeSpaceFilter:
    """Block immediately and require several clear frames before reopening a zone."""

    def __init__(self, clear_frames=3):
        self.clear_frames = max(1, int(clear_frames))
        self.free = [True, True, True]
        self.clear_streak = [self.clear_frames] * 3

    def update(self, raw_free):
        for index, is_free in enumerate(raw_free):
            if not is_free:
                self.free[index] = False
                self.clear_streak[index] = 0
            elif not self.free[index]:
                self.clear_streak[index] += 1
                if self.clear_streak[index] >= self.clear_frames:
                    self.free[index] = True
        return tuple(self.free)


class RedBallDetector:
    """Original LAB/contour detector, retained as the baseline and fallback."""

    def __init__(self, config):
        self.target = str(config.get("target", "red_ball"))
        self.lab_min = np.array(config["lab_min"], dtype=np.uint8)
        self.lab_max = np.array(config["lab_max"], dtype=np.uint8)
        self.processing_size = (
            int(config.get("processing_width", 320)),
            int(config.get("processing_height", 240)),
        )
        self.blur_kernel = self._odd_kernel(config.get("gaussian_blur_kernel", 3))
        self.morphology_kernel = max(1, int(config.get("morphology_kernel", 3)))
        self.minimum_area = float(config.get("minimum_contour_area", 100.0))
        self.minimum_circularity = float(config.get("minimum_circularity", 0.70))
        self.minimum_aspect_ratio = float(config.get("minimum_aspect_ratio", 0.75))
        self.maximum_aspect_ratio = float(config.get("maximum_aspect_ratio", 1.33))
        self.confirmation_frames = max(1, int(config.get("confirmation_frames", 3)))
        self.release_frames = max(1, int(config.get("release_frames", 3)))
        self.left_ratio = float(config.get("fire_left_ratio", 0.4))
        self.right_ratio = float(config.get("fire_right_ratio", 0.6))
        self._hit_streak = 0
        self._miss_streak = 0
        self._confirmed = False
        self._last_geometry = None

    @staticmethod
    def _odd_kernel(value):
        value = max(1, int(value))
        return value if value % 2 else value + 1

    def _update_confirmation(self, candidate_detected):
        if candidate_detected:
            self._hit_streak += 1
            self._miss_streak = 0
            if self._hit_streak >= self.confirmation_frames:
                self._confirmed = True
        else:
            self._miss_streak += 1
            self._hit_streak = 0
            if self._miss_streak >= self.release_frames:
                self._confirmed = False
                self._last_geometry = None

    def process(self, frame):
        started = time.perf_counter()
        if frame is None or frame.size == 0:
            raise ValueError("frame must be a non-empty image")

        original_height, original_width = frame.shape[:2]
        resized = cv2.resize(frame, self.processing_size, interpolation=cv2.INTER_AREA)
        blurred = cv2.GaussianBlur(
            resized, (self.blur_kernel, self.blur_kernel), self.blur_kernel
        )
        lab = cv2.cvtColor(blurred, cv2.COLOR_BGR2LAB)
        mask = cv2.inRange(lab, self.lab_min, self.lab_max)
        kernel = cv2.getStructuringElement(
            cv2.MORPH_ELLIPSE,
            (self.morphology_kernel, self.morphology_kernel),
        )
        mask = cv2.morphologyEx(mask, cv2.MORPH_OPEN, kernel)
        mask = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, kernel)

        contours = cv2.findContours(
            mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE
        )[-2]
        contour_candidates = []
        largest_color_area = 0.0
        largest_color_circularity = 0.0
        largest_color_aspect_ratio = 0.0
        for current_contour in contours:
            current_area = float(cv2.contourArea(current_contour))
            perimeter = float(cv2.arcLength(current_contour, True))
            _x, _y, width, height = cv2.boundingRect(current_contour)
            current_aspect_ratio = width / float(height) if height > 0 else 0.0
            current_circularity = (
                4.0 * np.pi * current_area / (perimeter * perimeter)
                if perimeter > 0.0
                else 0.0
            )
            if current_area > largest_color_area:
                largest_color_area = current_area
                largest_color_circularity = current_circularity
                largest_color_aspect_ratio = current_aspect_ratio
            if (
                current_area >= self.minimum_area
                and current_circularity >= self.minimum_circularity
                and self.minimum_aspect_ratio
                <= current_aspect_ratio
                <= self.maximum_aspect_ratio
            ):
                contour_candidates.append(
                    (
                        current_area,
                        current_circularity,
                        current_aspect_ratio,
                        current_contour,
                    )
                )

        if contour_candidates:
            area, circularity, aspect_ratio, contour = max(
                contour_candidates, key=lambda item: item[0]
            )
            candidate_detected = True
        else:
            area = circularity = aspect_ratio = 0.0
            contour = None
            candidate_detected = False
        self._update_confirmation(candidate_detected)

        center_x = center_y = radius = None
        if candidate_detected:
            (x, y), detected_radius = cv2.minEnclosingCircle(contour)
            scale_x = original_width / float(self.processing_size[0])
            scale_y = original_height / float(self.processing_size[1])
            center_x = round(x * scale_x, 2)
            center_y = round(y * scale_y, 2)
            radius = round(detected_radius * (scale_x + scale_y) / 2.0, 2)
            self._last_geometry = (center_x, center_y, radius)
        elif self._confirmed and self._last_geometry is not None:
            center_x, center_y, radius = self._last_geometry

        score = min(1.0, area / max(self.minimum_area * 8.0, 1.0))
        fire_direction = _direction(
            center_x if self._confirmed else None,
            original_width,
            self.left_ratio,
            self.right_ratio,
        )
        processing_ms = round((time.perf_counter() - started) * 1000.0, 2)
        return {
            "event": "detection",
            "backend": "baseline",
            "target": self.target,
            "detected": self._confirmed,
            "candidate_detected": candidate_detected,
            "center_x": center_x,
            "center_y": center_y,
            "radius": radius,
            "contour_area": round(area, 2),
            "circularity": round(circularity, 4),
            "minimum_circularity": self.minimum_circularity,
            "aspect_ratio": round(aspect_ratio, 4),
            "minimum_aspect_ratio": self.minimum_aspect_ratio,
            "maximum_aspect_ratio": self.maximum_aspect_ratio,
            "largest_color_area": round(largest_color_area, 2),
            "largest_color_circularity": round(largest_color_circularity, 4),
            "largest_color_aspect_ratio": round(largest_color_aspect_ratio, 4),
            "score": round(score, 4),
            "confirmation_progress": min(self._hit_streak, self.confirmation_frames),
            "confirmation_required": self.confirmation_frames,
            "fire_detected": self._confirmed,
            "fire_center_x": center_x,
            "fire_center_y": center_y,
            "fire_confidence": round(score, 4) if self._confirmed else 0.0,
            "fire_direction": fire_direction,
            "fire_aligned": self._confirmed and fire_direction == "centered",
            "dynamic_obstacle": False,
            "obstacle_direction": "none",
            "obstacle_confidence": 0.0,
            "free_left": True,
            "free_front": True,
            "free_right": True,
            "frame_width": original_width,
            "frame_height": original_height,
            "processing_ms": processing_ms,
            "timestamp": time.time(),
        }


class _UltralyticsPredictor:
    def __init__(self, model_path, confidence, image_size, device):
        try:
            from ultralytics import YOLO
        except ImportError as exc:
            raise RuntimeError(
                "model backend requires requirements-ai.txt (ultralytics)"
            ) from exc
        self.model_path = str(model_path)
        self.model = YOLO(self.model_path)
        self.confidence = confidence
        self.image_size = image_size
        self.device = (
            "cpu"
            if device == "auto" and self.model_path.lower().endswith(".onnx")
            else device
        )

    def __call__(self, frame):
        options = {
            "source": frame,
            "conf": self.confidence,
            "imgsz": self.image_size,
            "verbose": False,
        }
        if self.device not in (None, "", "auto"):
            options["device"] = self.device
        result = self.model.predict(**options)[0]
        names = result.names
        detections = []
        if result.boxes is None:
            return detections
        for coordinates, class_id, confidence in zip(
            result.boxes.xyxy.cpu().tolist(),
            result.boxes.cls.cpu().tolist(),
            result.boxes.conf.cpu().tolist(),
        ):
            class_index = int(class_id)
            class_name = names[class_index] if isinstance(names, dict) else names[class_index]
            detections.append(
                {
                    "class_name": str(class_name),
                    "confidence": float(confidence),
                    "bbox": tuple(float(value) for value in coordinates),
                }
            )
        return detections


class ModelDetector:
    """YOLO detector with simple temporal state and explainable free-space ROIs."""

    def __init__(self, config, predictor=None):
        model_config = config.get("model", {})
        self.fire_classes = set(model_config.get("fire_classes", ["fire_marker"]))
        self.dynamic_classes = set(
            model_config.get("dynamic_classes", ["dynamic_obstacle", "person"])
        )
        self.static_classes = set(
            model_config.get("static_classes", ["static_obstacle", "obstacle"])
        )
        self.left_ratio = float(config.get("fire_left_ratio", 0.4))
        self.right_ratio = float(config.get("fire_right_ratio", 0.6))
        self.obstacle_left_ratio = float(config.get("obstacle_left_ratio", 1.0 / 3.0))
        self.obstacle_right_ratio = float(config.get("obstacle_right_ratio", 2.0 / 3.0))
        self.roi_top_ratio = float(config.get("occupancy_roi_top_ratio", 0.5))
        self.occupancy_threshold = float(config.get("occupancy_threshold", 0.06))
        stale_seconds = float(config.get("stale_timeout_seconds", 1.0))
        self.fire_state = _StableTarget(
            config.get("confirmation_frames", 3),
            config.get("release_frames", 3),
            stale_seconds,
        )
        self.obstacle_state = _StableTarget(
            config.get("obstacle_confirmation_frames", 1),
            config.get("obstacle_release_frames", 3),
            stale_seconds,
        )
        self.free_filter = _FreeSpaceFilter(config.get("free_clear_frames", 3))
        self.predictor = predictor or _UltralyticsPredictor(
            model_config.get("path", "weights/best.pt"),
            float(model_config.get("confidence", 0.25)),
            int(model_config.get("image_size", 640)),
            model_config.get("device", "auto"),
        )
        self.fallback = (
            RedBallDetector(config)
            if bool(model_config.get("baseline_fire_fallback", True))
            else None
        )

    @staticmethod
    def _observation(detection):
        x1, y1, x2, y2 = detection["bbox"]
        return {
            "center_x": (x1 + x2) / 2.0,
            "center_y": (y1 + y2) / 2.0,
            "radius": max(x2 - x1, y2 - y1) / 2.0,
            "confidence": detection["confidence"],
            "bbox": (x1, y1, x2, y2),
        }

    def _raw_free_space(self, boxes, width, height):
        top = height * self.roi_top_ratio
        boundaries = (0.0, width / 3.0, 2.0 * width / 3.0, float(width))
        blocked = [False, False, False]
        for box in boxes:
            x1, y1, x2, y2 = box
            clipped_y1 = max(y1, top)
            clipped_y2 = min(y2, float(height))
            if clipped_y2 <= clipped_y1:
                continue
            for index in range(3):
                clipped_x1 = max(x1, boundaries[index])
                clipped_x2 = min(x2, boundaries[index + 1])
                intersection = max(0.0, clipped_x2 - clipped_x1) * (
                    clipped_y2 - clipped_y1
                )
                roi_area = (boundaries[index + 1] - boundaries[index]) * (
                    height - top
                )
                if roi_area > 0 and intersection / roi_area >= self.occupancy_threshold:
                    blocked[index] = True
        return tuple(not value for value in blocked)

    def process(self, frame):
        started = time.perf_counter()
        if frame is None or frame.size == 0:
            raise ValueError("frame must be a non-empty image")
        height, width = frame.shape[:2]
        detections = self.predictor(frame)
        fire_detections = [
            item for item in detections if item["class_name"] in self.fire_classes
        ]
        dynamic_detections = [
            item for item in detections if item["class_name"] in self.dynamic_classes
        ]
        obstacle_detections = [
            item
            for item in detections
            if item["class_name"] in self.dynamic_classes | self.static_classes
        ]

        fire_candidate = (
            self._observation(max(fire_detections, key=lambda item: item["confidence"]))
            if fire_detections
            else None
        )
        if fire_candidate is None and self.fallback is not None:
            fallback_result = self.fallback.process(frame)
            if fallback_result["candidate_detected"]:
                fire_candidate = {
                    "center_x": fallback_result["center_x"],
                    "center_y": fallback_result["center_y"],
                    "radius": fallback_result["radius"],
                    "confidence": fallback_result["score"],
                    "bbox": (
                        fallback_result["center_x"] - fallback_result["radius"],
                        fallback_result["center_y"] - fallback_result["radius"],
                        fallback_result["center_x"] + fallback_result["radius"],
                        fallback_result["center_y"] + fallback_result["radius"],
                    ),
                }
        dynamic_candidate = (
            self._observation(
                max(dynamic_detections, key=lambda item: item["confidence"])
            )
            if dynamic_detections
            else None
        )
        fire_detected, fire = self.fire_state.update(fire_candidate)
        dynamic_obstacle, obstacle = self.obstacle_state.update(dynamic_candidate)

        occupancy_boxes = [item["bbox"] for item in obstacle_detections]
        if dynamic_obstacle and obstacle is not None and obstacle["bbox"] not in occupancy_boxes:
            occupancy_boxes.append(obstacle["bbox"])
        free_left, free_front, free_right = self.free_filter.update(
            self._raw_free_space(occupancy_boxes, width, height)
        )
        fire_direction = _direction(
            fire["center_x"] if fire_detected and fire else None,
            width,
            self.left_ratio,
            self.right_ratio,
        )
        obstacle_direction = _direction(
            obstacle["center_x"] if dynamic_obstacle and obstacle else None,
            width,
            self.obstacle_left_ratio,
            self.obstacle_right_ratio,
            centered="front",
        )
        processing_ms = round((time.perf_counter() - started) * 1000.0, 2)
        center_x = fire["center_x"] if fire_detected and fire else None
        center_y = fire["center_y"] if fire_detected and fire else None
        radius = fire["radius"] if fire_detected and fire else None
        score = fire["confidence"] if fire_detected and fire else 0.0
        return {
            "event": "detection",
            "backend": "model",
            "target": "fire_marker",
            "detected": fire_detected,
            "candidate_detected": fire_candidate is not None,
            "center_x": center_x,
            "center_y": center_y,
            "radius": radius,
            "score": round(score, 4),
            "circularity": 0.0,
            "aspect_ratio": 1.0,
            "fire_detected": fire_detected,
            "fire_center_x": center_x,
            "fire_center_y": center_y,
            "fire_confidence": round(score, 4),
            "fire_direction": fire_direction,
            "fire_aligned": fire_detected and fire_direction == "centered",
            "dynamic_obstacle": dynamic_obstacle,
            "obstacle_direction": obstacle_direction,
            "obstacle_confidence": round(
                obstacle["confidence"] if dynamic_obstacle and obstacle else 0.0, 4
            ),
            "free_left": free_left,
            "free_front": free_front,
            "free_right": free_right,
            "frame_width": width,
            "frame_height": height,
            "processing_ms": processing_ms,
            "timestamp": time.time(),
        }


class OrangePiNpuDetector:
    def __init__(self, _config):
        raise RuntimeError(
            "Ascend runtime unavailable: install and validate the Orange Pi ACL runtime"
        )


def build_detector(config, predictor=None):
    backend = str(config.get("backend", "baseline")).lower()
    if backend == "baseline":
        return RedBallDetector(config)
    if backend == "model":
        return ModelDetector(config, predictor=predictor)
    if backend in ("orangepi_npu", "npu"):
        return OrangePiNpuDetector(config)
    raise ValueError("unknown detection backend: {}".format(backend))
