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

import yaml

from perception.detector import build_detector
from perception.snapshot_server import SnapshotServer
from perception.telemetry import JsonLineTelemetry
from perception.udp_telemetry import UdpPerceptionTelemetry
from perception.video_source import MjpegVideoSource


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
    return parser.parse_args()


def run(config, max_frames=0):
    source = MjpegVideoSource(config["video"])
    detector = build_detector(config["detection"])
    telemetry = JsonLineTelemetry(config["telemetry"])
    udp_telemetry = UdpPerceptionTelemetry(config["telemetry"])
    snapshot_server = SnapshotServer(config["video_proxy"])
    processed = 0

    telemetry.emit_event(
        "startup",
        {
            "mode": config["project"]["mode"],
            "video_url": config["video"]["url"],
        },
    )
    udp_telemetry.emit_status("starting", video_ok=False, force=True)
    snapshot_server.start()

    try:
        while not STOP_REQUESTED:
            frame_record = source.read()
            if frame_record is None:
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

            processed += 1
            if max_frames > 0 and processed >= max_frames:
                break
    finally:
        source.close()
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
        run(config, max_frames=args.max_frames)
    except Exception as exc:
        print("fatal: {}: {}".format(type(exc).__name__, exc), file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
