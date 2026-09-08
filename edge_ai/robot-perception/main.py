#!/usr/bin/env python3
"""TonyPi red-ball perception sidecar for Orange Pi.

The process reads MJPEG video and writes JSON Lines to stdout. It deliberately
contains no motion-control client or robot action imports.
"""

import argparse
import signal
import sys
import time
from pathlib import Path

import cv2
import yaml

from perception.detector import build_detector
from perception.snapshot_server import SnapshotServer
from perception.telemetry import JsonLineTelemetry
from perception.udp_telemetry import UdpPerceptionTelemetry
from perception.video_source import build_video_source


STOP_REQUESTED = False


def request_stop(_signum, _frame):
    global STOP_REQUESTED
    STOP_REQUESTED = True


def load_config(path):
    with path.open("r", encoding="utf-8") as stream:
        config = yaml.safe_load(stream)
    if not isinstance(config, dict):
        raise ValueError("configuration root must be a mapping")
    safety = config.get("safety", {})
    expected_listener = "tcp:0.0.0.0:8080"
    if safety.get("network_listener_enabled") is not True:
        raise ValueError("edge video proxy listener must be enabled")
    if safety.get("allowed_network_listeners") != [expected_listener]:
        raise ValueError("only the edge video proxy listener is allowed")
    return config


def parse_args():
    default_config = Path(__file__).resolve().with_name("config.yaml")
    parser = argparse.ArgumentParser(description="Orange Pi shadow perception")
    parser.add_argument("--config", type=Path, default=default_config)
    parser.add_argument(
        "--max-frames",
        type=int,
        default=0,
        help="stop after N successfully read frames; 0 means run continuously",
    )
    parser.add_argument("--source", choices=("mjpeg", "local", "webcam"))
    parser.add_argument("--input", type=Path, help="local video path")
    parser.add_argument("--camera-index", type=int)
    parser.add_argument("--backend", choices=("baseline", "model", "orangepi_npu"))
    parser.add_argument("--model", type=Path, help="PT or ONNX model path")
    parser.add_argument("--udp-host")
    parser.add_argument("--schema-version", type=int, choices=(1, 2))
    parser.add_argument("--annotated-output", type=Path)
    parser.add_argument("--no-snapshot", action="store_true")
    return parser.parse_args()


def annotate_frame(frame, result):
    output = frame.copy()
    if result.get("fire_detected") and result.get("fire_center_x") is not None:
        center = (int(result["fire_center_x"]), int(result["fire_center_y"]))
        radius = max(3, int(result.get("radius") or 10))
        cv2.circle(output, center, radius, (0, 0, 255), 2)
    lines = (
        "fire={} {}".format(
            result.get("fire_detected", False), result.get("fire_direction", "none")
        ),
        "obstacle={} {}".format(
            result.get("dynamic_obstacle", False),
            result.get("obstacle_direction", "none"),
        ),
        "free L/F/R={}/{}/{}".format(
            result.get("free_left", True),
            result.get("free_front", True),
            result.get("free_right", True),
        ),
    )
    for index, line in enumerate(lines):
        cv2.putText(
            output,
            line,
            (10, 25 + index * 25),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.6,
            (255, 255, 255),
            2,
            cv2.LINE_AA,
        )
    return output


def run(config, max_frames=0, annotated_output=None):
    source = build_video_source(config["video"])
    detector = build_detector(config["detection"])
    telemetry = JsonLineTelemetry(config["telemetry"])
    udp_telemetry = UdpPerceptionTelemetry(config["telemetry"])
    snapshot_server = SnapshotServer(config["video_proxy"])
    processed = 0
    video_writer = None

    telemetry.emit_event(
        "startup",
        {
            "mode": config["project"]["mode"],
            "video_source": config["video"].get("source", "mjpeg"),
            "backend": config["detection"].get("backend", "baseline"),
        },
    )
    udp_telemetry.emit_status("starting", video_ok=False, force=True)
    snapshot_server.start()

    try:
        while not STOP_REQUESTED:
            frame_record = source.read()
            if frame_record is None:
                if source.exhausted:
                    break
                telemetry.emit_event(
                    "video_unavailable",
                    {"detected": False, "target": config["detection"]["target"]},
                    rate_limit_key="video_unavailable",
                )
                udp_telemetry.emit_status("video_unavailable", video_ok=False)
                continue

            captured_at, frame = frame_record
            snapshot_server.publish(frame)
            result = detector.process(frame)
            result["captured_at"] = captured_at
            result["processed_at"] = time.time()
            result["processing_delay_ms"] = round(
                (result["processed_at"] - captured_at) * 1000.0, 2
            )
            telemetry.emit_detection(result)
            udp_telemetry.emit_detection(result)
            if annotated_output is not None:
                if video_writer is None:
                    annotated_output.parent.mkdir(parents=True, exist_ok=True)
                    height, width = frame.shape[:2]
                    codec = "MJPG" if annotated_output.suffix.lower() == ".avi" else "mp4v"
                    video_writer = cv2.VideoWriter(
                        str(annotated_output),
                        cv2.VideoWriter_fourcc(*codec),
                        float(config["video"].get("annotated_fps", 20.0)),
                        (width, height),
                    )
                    if not video_writer.isOpened():
                        raise RuntimeError(
                            "cannot create annotated video: {}".format(annotated_output)
                        )
                video_writer.write(annotate_frame(frame, result))

            processed += 1
            if max_frames > 0 and processed >= max_frames:
                break
    finally:
        source.close()
        if video_writer is not None:
            video_writer.release()
        snapshot_server.close()
        udp_telemetry.emit_status("shutdown", video_ok=False, force=True)
        udp_telemetry.close()
        telemetry.emit_event("shutdown", {"processed_frames": processed})

    return processed


def main():
    args = parse_args()
    signal.signal(signal.SIGINT, request_stop)
    signal.signal(signal.SIGTERM, request_stop)
    try:
        config = load_config(args.config)
        if args.source:
            config["video"]["source"] = args.source
        if args.input:
            config["video"]["source"] = "local"
            config["video"]["local_path"] = str(args.input)
        if args.camera_index is not None:
            config["video"]["source"] = "webcam"
            config["video"]["camera_index"] = args.camera_index
        if args.backend:
            config["detection"]["backend"] = args.backend
        if args.model:
            config["detection"]["model"]["path"] = str(args.model)
        if args.udp_host:
            config["telemetry"]["udp"]["destination_host"] = args.udp_host
        if args.schema_version:
            config["telemetry"]["udp"]["schema_version"] = args.schema_version
        if args.no_snapshot:
            config["video_proxy"]["enabled"] = False
        run(
            config,
            max_frames=args.max_frames,
            annotated_output=args.annotated_output,
        )
    except Exception as exc:
        print("fatal: {}: {}".format(type(exc).__name__, exc), file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
