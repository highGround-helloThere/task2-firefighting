#!/usr/bin/env python3
"""Run the baseline detector and UDP telemetry without robot hardware."""

import copy
import json
import socket
import sys
import time
from pathlib import Path

import cv2
import numpy as np
import yaml


PROJECT_ROOT = Path(__file__).resolve().parents[1]
if str(PROJECT_ROOT) not in sys.path:
    sys.path.insert(0, str(PROJECT_ROOT))

from perception.detector import build_detector
from perception.udp_telemetry import UdpPerceptionTelemetry


LOOPBACK_HOST = "127.0.0.1"
LOOPBACK_PORT = 6101
FRAME_WIDTH = 640
FRAME_HEIGHT = 480
BALL_CENTER = (FRAME_WIDTH // 2, FRAME_HEIGHT // 2)
BALL_RADIUS = 80


def load_config():
    with (PROJECT_ROOT / "config.yaml").open("r", encoding="utf-8") as stream:
        config = yaml.safe_load(stream)
    if not isinstance(config, dict):
        raise ValueError("configuration root must be a mapping")
    return config


def bgr_to_lab(bgr):
    pixel = np.array([[bgr]], dtype=np.uint8)
    return cv2.cvtColor(pixel, cv2.COLOR_BGR2LAB)[0, 0].tolist()


def lab_to_bgr(lab):
    pixel = np.array([[lab]], dtype=np.uint8)
    return cv2.cvtColor(pixel, cv2.COLOR_LAB2BGR)[0, 0].tolist()


def is_in_lab_range(value, lab_min, lab_max):
    return all(
        low <= channel <= high
        for channel, low, high in zip(value, lab_min, lab_max)
    )


def make_config_compatible_frame(detection_config):
    lab_min = [int(value) for value in detection_config["lab_min"]]
    lab_max = [int(value) for value in detection_config["lab_max"]]
    target_lab = [(low + high) // 2 for low, high in zip(lab_min, lab_max)]
    target_bgr = lab_to_bgr(target_lab)
    round_trip_lab = bgr_to_lab(target_bgr)

    if not is_in_lab_range(round_trip_lab, lab_min, lab_max):
        raise AssertionError(
            "LAB-to-BGR round trip escaped the configured range: {}".format(
                round_trip_lab
            )
        )

    frame = np.zeros((FRAME_HEIGHT, FRAME_WIDTH, 3), dtype=np.uint8)
    cv2.circle(frame, BALL_CENTER, BALL_RADIUS, tuple(target_bgr), thickness=-1)
    return frame, target_lab, target_bgr, round_trip_lab


def run_detector_test(config):
    detection_config = config["detection"]
    lab_min = [int(value) for value in detection_config["lab_min"]]
    lab_max = [int(value) for value in detection_config["lab_max"]]
    pure_bgr_red = [0, 0, 255]
    pure_red_lab = bgr_to_lab(pure_bgr_red)

    print("Detector smoke test")
    print("  configured LAB range: {} to {}".format(lab_min, lab_max))
    print("  pure BGR red {} -> LAB {}".format(pure_bgr_red, pure_red_lab))
    if not is_in_lab_range(pure_red_lab, lab_min, lab_max):
        print(
            "  pure BGR red is outside the configured LAB range; "
            "the smoke test uses a configuration-compatible target color."
        )

    frame, target_lab, target_bgr, round_trip_lab = make_config_compatible_frame(
        detection_config
    )
    print(
        "  synthetic target LAB {} -> BGR {} -> LAB {}".format(
            target_lab, target_bgr, round_trip_lab
        )
    )

    detector = build_detector(detection_config)
    result = None
    for call_index in range(int(detection_config["confirmation_frames"])):
        started_at = time.perf_counter()
        result = detector.process(frame)
        result["processing_delay_ms"] = round(
            (time.perf_counter() - started_at) * 1000.0, 2
        )
        print(
            "  call {}: candidate_detected={} detected={} confirmation={}/{}".format(
                call_index + 1,
                result["candidate_detected"],
                result["detected"],
                result["confirmation_progress"],
                result["confirmation_required"],
            )
        )

    assert result is not None
    assert result["candidate_detected"] is True
    assert result["detected"] is True
    assert result["center_x"] is not None and result["center_y"] is not None
    assert abs(result["center_x"] - BALL_CENTER[0]) <= 5.0
    assert abs(result["center_y"] - BALL_CENTER[1]) <= 5.0

    print("  final result:")
    for field in (
        "candidate_detected",
        "detected",
        "center_x",
        "center_y",
        "radius",
        "score",
        "circularity",
        "aspect_ratio",
    ):
        print("    {}={}".format(field, result[field]))
    return result


def validate_packet(packet, result):
    assert packet["type"] == "perception"
    assert packet["schema_version"] == 1
    assert packet["source"]
    assert packet["session_id"]
    assert packet["seq"] > 0
    assert packet["video_ok"] is True
    assert packet["detected"] is True
    assert "processing_ms" in packet
    assert abs(packet["center_x"] - result["center_x"]) <= 0.01
    assert abs(packet["center_y"] - result["center_y"]) <= 0.01


def run_udp_loopback_test(config, result):
    telemetry_config = copy.deepcopy(config["telemetry"])
    telemetry_config["udp"].update(
        {
            "enabled": True,
            "destination_host": LOOPBACK_HOST,
            "destination_port": LOOPBACK_PORT,
            "schema_version": 1,
        }
    )

    receiver = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    receiver.bind((LOOPBACK_HOST, LOOPBACK_PORT))
    receiver.settimeout(2.0)
    telemetry = UdpPerceptionTelemetry(
        telemetry_config, session_id="baseline-smoke-test"
    )
    packets = []

    try:
        for _ in range(3):
            assert telemetry.emit_detection(result, force=True) is True
            data, address = receiver.recvfrom(4096)
            packet = json.loads(data.decode("utf-8"))
            validate_packet(packet, result)
            packets.append(packet)
            print(
                "  received UDP from {}:{}: {}".format(
                    address[0], address[1], json.dumps(packet, sort_keys=True)
                )
            )
    finally:
        telemetry.close()
        receiver.close()

    sequences = [packet["seq"] for packet in packets]
    assert sequences == sorted(sequences)
    assert all(
        current == previous + 1
        for previous, current in zip(sequences, sequences[1:])
    )
    assert len({packet["session_id"] for packet in packets}) == 1
    print("UDP loopback test")
    print("  endpoint={}:{}".format(LOOPBACK_HOST, LOOPBACK_PORT))
    print(
        "  schema_version={} session_id={}".format(
            packets[0]["schema_version"], packets[0]["session_id"]
        )
    )
    print("  seq values={} (strictly increasing)".format(sequences))


def main():
    config = load_config()
    result = run_detector_test(config)
    run_udp_loopback_test(config, result)
    print("Stage 1A baseline smoke test: PASS")


if __name__ == "__main__":
    main()
