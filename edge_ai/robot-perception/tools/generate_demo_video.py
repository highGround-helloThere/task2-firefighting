#!/usr/bin/env python3
"""Generate a small local AVI with a moving fire marker and obstacles."""

import argparse
from pathlib import Path

import cv2
import numpy as np


def parse_args():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=Path("data/demo.avi"))
    parser.add_argument("--frames", type=int, default=90)
    parser.add_argument("--fps", type=float, default=15.0)
    return parser.parse_args()


def main():
    args = parse_args()
    if args.frames < 1 or args.fps <= 0:
        raise ValueError("--frames and --fps must be positive")
    args.output.parent.mkdir(parents=True, exist_ok=True)
    size = (640, 480)
    writer = cv2.VideoWriter(
        str(args.output), cv2.VideoWriter_fourcc(*"MJPG"), args.fps, size
    )
    if not writer.isOpened():
        raise RuntimeError(
            "cannot create video; run from robot-perception with a simple relative path"
        )
    try:
        for index in range(args.frames):
            frame = np.zeros((size[1], size[0], 3), dtype=np.uint8)
            frame[:] = (35, 35, 35)
            center_x = 70 + int((size[0] - 140) * index / max(1, args.frames - 1))
            # BGR color round-trips to LAB [122, 170, 134], inside config.yaml.
            cv2.circle(frame, (center_x, 170), 42, (105, 82, 179), -1, cv2.LINE_AA)
            cv2.rectangle(frame, (250, 320), (390, 475), (100, 100, 100), -1)
            cv2.putText(
                frame,
                "synthetic pipeline demo",
                (15, 35),
                cv2.FONT_HERSHEY_SIMPLEX,
                0.8,
                (255, 255, 255),
                2,
                cv2.LINE_AA,
            )
            writer.write(frame)
    finally:
        writer.release()
    print("Demo video created: {} ({} frames)".format(args.output, args.frames))


if __name__ == "__main__":
    main()
