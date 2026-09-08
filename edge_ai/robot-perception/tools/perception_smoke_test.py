#!/usr/bin/env python3
"""Smoke-test unified perception, video sources, and UDP schemas without hardware."""

import copy
import json
import socket
import sys
import tempfile
from pathlib import Path

import cv2
import numpy as np


PROJECT_ROOT = Path(__file__).resolve().parents[1]
if str(PROJECT_ROOT) not in sys.path:
    sys.path.insert(0, str(PROJECT_ROOT))

from main import load_config
from perception.detector import build_detector
from perception.udp_telemetry import UdpPerceptionTelemetry
from perception.video_source import LocalVideoSource, WebcamVideoSource, build_video_source


FRAME = np.zeros((480, 640, 3), dtype=np.uint8)


def detection(class_name, bbox, confidence=0.9):
    return {"class_name": class_name, "bbox": bbox, "confidence": confidence}


def model_detector(config, frames):
    detector_config = copy.deepcopy(config["detection"])
    detector_config["backend"] = "model"
    detector_config["confirmation_frames"] = 1
    detector_config["obstacle_confirmation_frames"] = 1
    detector_config["model"]["baseline_fire_fallback"] = False
    stream = iter(frames)
    return build_detector(detector_config, predictor=lambda _frame: next(stream))


def test_config_and_directions(config):
    for expected, box in (
        ("left", (20.0, 80.0, 120.0, 180.0)),
        ("centered", (270.0, 80.0, 370.0, 180.0)),
        ("right", (520.0, 80.0, 620.0, 180.0)),
    ):
        result = model_detector(config, [[detection("fire_marker", box)]]).process(FRAME)
        assert result["fire_detected"] is True
        assert result["fire_direction"] == expected
        assert result["fire_center_x"] is not None
    print("fire direction left/centered/right: PASS")


def test_obstacles_and_free_space(config):
    cases = (
        ("left", (10.0, 260.0, 190.0, 470.0), "free_left"),
        ("front", (235.0, 260.0, 405.0, 470.0), "free_front"),
        ("right", (450.0, 260.0, 630.0, 470.0), "free_right"),
    )
    for expected_direction, box, blocked_field in cases:
        result = model_detector(
            config, [[detection("dynamic_obstacle", box)]]
        ).process(FRAME)
        assert result["dynamic_obstacle"] is True
        assert result["obstacle_direction"] == expected_direction
        assert result[blocked_field] is False
    static_result = model_detector(
        config, [[detection("static_obstacle", cases[1][1])]]
    ).process(FRAME)
    assert static_result["dynamic_obstacle"] is False
    assert static_result["free_front"] is False
    print("obstacle direction and free-space ROIs: PASS")


def test_temporal_state(config):
    fire = detection("fire_marker", (270.0, 80.0, 370.0, 180.0))
    detector = model_detector(config, [[fire], [], [], []])
    results = [detector.process(FRAME) for _ in range(4)]
    assert results[0]["fire_detected"] is True
    assert results[1]["fire_detected"] is True
    assert results[1]["fire_center_x"] is not None
    assert results[2]["fire_detected"] is True
    assert results[2]["fire_center_x"] is not None
    assert results[3]["fire_detected"] is False
    assert results[3]["fire_center_x"] is None
    print("hit/miss stable geometry and release: PASS")


def receive_packet(schema_version, result):
    receiver = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    receiver.bind(("127.0.0.1", 0))
    receiver.settimeout(2.0)
    port = receiver.getsockname()[1]
    sender = UdpPerceptionTelemetry(
        {
            "udp": {
                "enabled": True,
                "destination_host": "127.0.0.1",
                "destination_port": port,
                "send_hz": 10,
                "schema_version": schema_version,
                "source": "perception-smoke-test",
            }
        },
        session_id="perception-smoke-test",
    )
    try:
        assert sender.emit_detection(result, force=True)
        packet = json.loads(receiver.recvfrom(8192)[0].decode("utf-8"))
    finally:
        sender.close()
        receiver.close()
    return packet


def test_udp(config):
    fire = detection("fire_marker", (270.0, 80.0, 370.0, 180.0))
    obstacle = detection("dynamic_obstacle", (235.0, 260.0, 405.0, 470.0))
    result = model_detector(config, [[fire, obstacle]]).process(FRAME)
    v1 = receive_packet(1, result)
    assert v1["schema_version"] == 1 and v1["detected"] is True
    assert v1["center_x"] == result["fire_center_x"]
    v2 = receive_packet(2, result)
    assert v2["schema_version"] == 2 and v2["fire_detected"] is True
    assert v2["dynamic_obstacle"] is True and v2["free_front"] is False
    assert v2["fire_direction"] == "centered" and "processing_ms" in v2
    print("UDP schema v1: PASS")
    print("UDP schema v2: PASS")
    print("v2 example={}".format(json.dumps(v2, sort_keys=True)))


def test_local_video_source():
    with tempfile.TemporaryDirectory(prefix="perception-smoke-") as temp_dir:
        path = Path(temp_dir) / "source.avi"
        writer = cv2.VideoWriter(
            str(path), cv2.VideoWriter_fourcc(*"MJPG"), 10.0, (64, 48)
        )
        assert writer.isOpened()
        writer.write(np.full((48, 64, 3), 127, dtype=np.uint8))
        writer.release()
        source = build_video_source(
            {"source": "local", "local_path": str(path), "loop_local_video": False}
        )
        assert isinstance(source, LocalVideoSource)
        record = source.read()
        assert record is not None and record[1].shape[:2] == (48, 64)
        assert source.read() is None and source.exhausted is True
        source.close()
    webcam = build_video_source({"source": "webcam", "camera_index": 0})
    assert isinstance(webcam, WebcamVideoSource)
    webcam.close()
    print("local video source and webcam construction: PASS")


def main():
    config = load_config(PROJECT_ROOT / "config.yaml")
    assert config["detection"]["backend"] == "baseline"
    print("config load and default baseline: PASS")
    test_config_and_directions(config)
    test_obstacles_and_free_space(config)
    test_temporal_state(config)
    test_udp(config)
    test_local_video_source()
    print("Unified perception smoke test: PASS")


if __name__ == "__main__":
    main()
