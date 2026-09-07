#!/usr/bin/env python3
"""Send schema-v1 perception telemetry for Unity loopback testing."""

import argparse
import sys
import time
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[1]
if str(PROJECT_ROOT) not in sys.path:
    sys.path.insert(0, str(PROJECT_ROOT))

from perception.udp_telemetry import UdpPerceptionTelemetry


def parse_args():
    parser = argparse.ArgumentParser(description="Send simulated perception UDP packets")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=6101)
    parser.add_argument("--duration", type=float, default=30.0)
    parser.add_argument("--hz", type=float, default=10.0)
    args = parser.parse_args()
    if not 1 <= args.port <= 65535:
        parser.error("--port must be between 1 and 65535")
    if args.duration < 0:
        parser.error("--duration must not be negative")
    if args.hz <= 0:
        parser.error("--hz must be greater than zero")
    return args


def simulated_results():
    return (
        {
            "detected": False,
            "candidate_detected": False,
            "target": "red_ball",
            "center_x": None,
            "center_y": None,
            "radius": None,
            "score": 0.0,
            "circularity": 0.0,
            "aspect_ratio": 0.0,
            "frame_width": 640,
            "frame_height": 480,
            "processing_delay_ms": 4.0,
        },
        {
            "detected": False,
            "candidate_detected": True,
            "target": "red_ball",
            "center_x": 302.0,
            "center_y": 238.0,
            "radius": 32.0,
            "score": 0.62,
            "circularity": 0.84,
            "aspect_ratio": 1.02,
            "frame_width": 640,
            "frame_height": 480,
            "processing_delay_ms": 5.0,
        },
        {
            "detected": True,
            "candidate_detected": True,
            "target": "red_ball",
            "center_x": 320.0,
            "center_y": 240.0,
            "radius": 38.0,
            "score": 0.91,
            "circularity": 0.93,
            "aspect_ratio": 1.0,
            "frame_width": 640,
            "frame_height": 480,
            "processing_delay_ms": 5.5,
        },
    )


def main():
    args = parse_args()
    telemetry = UdpPerceptionTelemetry(
        {
            "udp": {
                "enabled": True,
                "destination_host": args.host,
                "destination_port": args.port,
                "send_hz": args.hz,
                "schema_version": 1,
                "source": "telemetry_simulator",
            }
        }
    )
    results = simulated_results()
    interval_seconds = 1.0 / args.hz
    deadline = time.monotonic() + args.duration
    index = 0

    print(
        "Sending schema-v1 perception telemetry to {}:{} for {:.1f} seconds "
        "(session_id={})".format(
            args.host, args.port, args.duration, telemetry.session_id
        )
    )
    try:
        while time.monotonic() < deadline:
            result = results[index % len(results)]
            if telemetry.emit_detection(result, force=True):
                state = (
                    "detected"
                    if result["detected"]
                    else "candidate"
                    if result["candidate_detected"]
                    else "clear"
                )
                print(
                    "seq={} state={} detected={}".format(
                        telemetry._sequence, state, result["detected"]
                    )
                )
            index += 1
            time.sleep(interval_seconds)
    finally:
        telemetry.close()


if __name__ == "__main__":
    main()
