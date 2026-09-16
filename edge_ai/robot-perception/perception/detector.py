"""Red, green, and blue target detection using OpenCV color filtering."""

import time

import cv2
import numpy as np

class ColorBallDetector:
    def __init__(self, config):
        self.target = str(config.get("target", "red_ball"))
        self.color_space = str(config.get("color_space", "LAB")).upper()
        self.color_ranges = self._load_color_ranges(config)
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
        self._hit_streak = 0
        self._miss_streak = 0
        self._confirmed = False

    @staticmethod
    def _odd_kernel(value):
        value = max(1, int(value))
        return value if value % 2 else value + 1

    def _load_color_ranges(self, config):
        configured_ranges = config.get("ranges")
        if configured_ranges:
            ranges = configured_ranges
        else:
            prefix = self.color_space.lower()
            ranges = [
                {
                    "min": config[f"{prefix}_min"],
                    "max": config[f"{prefix}_max"],
                }
            ]

        parsed = []
        for color_range in ranges:
            minimum = np.array(color_range["min"], dtype=np.uint8)
            maximum = np.array(color_range["max"], dtype=np.uint8)
            if minimum.shape != (3,) or maximum.shape != (3,):
                raise ValueError(f"{self.target} color ranges must contain three channels")
            parsed.append((minimum, maximum))
        return parsed

    def _convert_color(self, frame):
        conversions = {
            "LAB": cv2.COLOR_BGR2LAB,
            "HSV": cv2.COLOR_BGR2HSV,
            "RGB": cv2.COLOR_BGR2RGB,
        }
        conversion = conversions.get(self.color_space)
        if conversion is None:
            raise ValueError(f"unsupported color space: {self.color_space}")
        return cv2.cvtColor(frame, conversion)

    def _build_mask(self, converted):
        mask = np.zeros(converted.shape[:2], dtype=np.uint8)
        for minimum, maximum in self.color_ranges:
            mask = cv2.bitwise_or(mask, cv2.inRange(converted, minimum, maximum))
        return mask

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

    def process(self, frame):
        if frame is None or frame.size == 0:
            raise ValueError("frame must be a non-empty image")

        original_height, original_width = frame.shape[:2]
        resized = cv2.resize(frame, self.processing_size, interpolation=cv2.INTER_AREA)
        blurred = cv2.GaussianBlur(
            resized, (self.blur_kernel, self.blur_kernel), self.blur_kernel
        )
        converted = self._convert_color(blurred)
        mask = self._build_mask(converted)
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
            area = 0.0
            circularity = 0.0
            aspect_ratio = 0.0
            contour = None
            candidate_detected = False
        self._update_confirmation(candidate_detected)

        center_x = None
        center_y = None
        radius = None
        if candidate_detected:
            (x, y), detected_radius = cv2.minEnclosingCircle(contour)
            scale_x = original_width / float(self.processing_size[0])
            scale_y = original_height / float(self.processing_size[1])
            center_x = round(x * scale_x, 2)
            center_y = round(y * scale_y, 2)
            radius = round(detected_radius * (scale_x + scale_y) / 2.0, 2)

        score = min(1.0, area / max(self.minimum_area * 8.0, 1.0))
        return {
            "event": "detection",
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
            "confirmation_progress": min(
                self._hit_streak, self.confirmation_frames
            ),
            "confirmation_required": self.confirmation_frames,
            "frame_width": original_width,
            "frame_height": original_height,
            "timestamp": time.time(),
        }


class RedBallDetector(ColorBallDetector):
    """Backward-compatible name for the original single-target detector."""


class MultiColorBallDetector:
    def __init__(self, detector_configs):
        self.detectors = [ColorBallDetector(config) for config in detector_configs]
        if not self.detectors:
            raise ValueError("at least one color target must be configured")

    def process(self, frame):
        results = [detector.process(frame) for detector in self.detectors]
        confirmed = [result for result in results if result["detected"]]
        candidates = [result for result in results if result["candidate_detected"]]
        selected_pool = confirmed or candidates or results
        selected = max(
            selected_pool,
            key=lambda result: (
                float(result.get("contour_area", 0.0)),
                float(result.get("largest_color_area", 0.0)),
            ),
        )
        selected = dict(selected)
        selected["detected_targets"] = [
            result["target"] for result in confirmed
        ]
        return selected


def build_detector(config):
    targets = config.get("targets")
    if not targets:
        return RedBallDetector(config)
    if not isinstance(targets, dict):
        raise ValueError("detection.targets must be a mapping")

    shared = {key: value for key, value in config.items() if key != "targets"}
    detector_configs = []
    for target, target_config in targets.items():
        if not isinstance(target_config, dict):
            raise ValueError(f"configuration for {target} must be a mapping")
        merged = dict(shared)
        merged.update(target_config)
        merged["target"] = str(target)
        detector_configs.append(merged)
    return MultiColorBallDetector(detector_configs)
