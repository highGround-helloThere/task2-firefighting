#!/usr/bin/env python3
"""Generate a tiny synthetic YOLO dataset for pipeline validation only."""

import argparse
import random
from pathlib import Path

import cv2
import numpy as np
import yaml


PROJECT_ROOT = Path(__file__).resolve().parents[1]
CLASS_NAMES = ("fire_marker", "dynamic_obstacle", "static_obstacle")


def parse_args():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--output", type=Path, default=PROJECT_ROOT / "data" / "perception"
    )
    parser.add_argument("--train-count", type=int, default=48)
    parser.add_argument("--val-count", type=int, default=12)
    parser.add_argument("--size", type=int, default=416)
    parser.add_argument("--seed", type=int, default=7)
    args = parser.parse_args()
    if args.train_count < 1 or args.val_count < 1 or args.size < 128:
        parser.error("counts must be positive and --size must be at least 128")
    return args


def yolo_label(class_id, box, size):
    x1, y1, x2, y2 = box
    return "{} {:.6f} {:.6f} {:.6f} {:.6f}".format(
        class_id,
        (x1 + x2) / (2.0 * size),
        (y1 + y2) / (2.0 * size),
        (x2 - x1) / size,
        (y2 - y1) / size,
    )


def make_image(size, rng):
    base = np.full((size, size, 3), rng.randint(15, 80), dtype=np.uint8)
    gradient = np.linspace(0, rng.randint(15, 70), size, dtype=np.uint8)
    base = cv2.add(base, np.tile(gradient[:, None, None], (1, size, 3)))
    noise = np.random.default_rng(rng.randrange(2**32)).normal(0, 8, base.shape)
    image = np.clip(base.astype(np.float32) + noise, 0, 255).astype(np.uint8)
    labels = []

    if rng.random() < 0.9:
        radius = rng.randint(size // 18, size // 9)
        center = (
            rng.randint(radius + 5, size - radius - 5),
            rng.randint(radius + 5, int(size * 0.7)),
        )
        color = rng.choice(((0, 45, 255), (0, 100, 255), (20, 30, 220)))
        cv2.circle(image, center, radius, color, -1, cv2.LINE_AA)
        labels.append(
            yolo_label(
                0,
                (
                    center[0] - radius,
                    center[1] - radius,
                    center[0] + radius,
                    center[1] + radius,
                ),
                size,
            )
        )
        if rng.random() < 0.25:
            cover_x = center[0] + rng.randint(-radius, radius // 2)
            cv2.rectangle(
                image,
                (cover_x, center[1] - radius),
                (min(size - 1, cover_x + radius // 2), center[1] + radius),
                (35, 35, 35),
                -1,
            )

    if rng.random() < 0.7:
        width = rng.randint(size // 14, size // 8)
        height = rng.randint(size // 4, size // 2)
        x1 = rng.randint(5, size - width - 5)
        y2 = rng.randint(int(size * 0.75), size - 5)
        y1 = max(5, y2 - height)
        color = rng.choice(((210, 170, 80), (80, 180, 210), (180, 100, 180)))
        cv2.rectangle(image, (x1, y1 + width // 3), (x1 + width, y2), color, -1)
        cv2.circle(
            image,
            (x1 + width // 2, y1 + width // 3),
            width // 3,
            color,
            -1,
        )
        labels.append(yolo_label(1, (x1, y1, x1 + width, y2), size))

    if rng.random() < 0.75:
        width = rng.randint(size // 6, size // 3)
        height = rng.randint(size // 8, size // 3)
        x1 = rng.randint(2, size - width - 2)
        y2 = rng.randint(int(size * 0.78), size - 2)
        y1 = y2 - height
        color = rng.choice(((80, 80, 80), (130, 90, 50), (70, 120, 80)))
        cv2.rectangle(image, (x1, y1), (x1 + width, y2), color, -1)
        cv2.rectangle(image, (x1, y1), (x1 + width, y2), (220, 220, 220), 2)
        labels.append(yolo_label(2, (x1, y1, x1 + width, y2), size))

    brightness = rng.uniform(0.75, 1.25)
    image = np.clip(image.astype(np.float32) * brightness, 0, 255).astype(np.uint8)
    return image, labels


def generate_split(root, split, count, size, rng):
    image_dir = root / "images" / split
    label_dir = root / "labels" / split
    image_dir.mkdir(parents=True, exist_ok=True)
    label_dir.mkdir(parents=True, exist_ok=True)
    for index in range(count):
        image, labels = make_image(size, rng)
        stem = "synthetic_{:04d}".format(index)
        encoded_ok, encoded = cv2.imencode(".jpg", image)
        if not encoded_ok:
            raise RuntimeError("failed to write synthetic image")
        (image_dir / (stem + ".jpg")).write_bytes(encoded.tobytes())
        (label_dir / (stem + ".txt")).write_text(
            "\n".join(labels) + ("\n" if labels else ""), encoding="utf-8"
        )


def main():
    args = parse_args()
    output = args.output.resolve()
    rng = random.Random(args.seed)
    generate_split(output, "train", args.train_count, args.size, rng)
    generate_split(output, "val", args.val_count, args.size, rng)
    dataset = {
        "path": str(output),
        "train": "images/train",
        "val": "images/val",
        "names": {index: name for index, name in enumerate(CLASS_NAMES)},
    }
    (output / "dataset.yaml").write_text(
        yaml.safe_dump(dataset, sort_keys=False, allow_unicode=True), encoding="utf-8"
    )
    print("Synthetic dataset created: {}".format(output))
    print("train={} val={} classes={}".format(
        args.train_count, args.val_count, ",".join(CLASS_NAMES)
    ))
    print("PIPELINE VALIDATION ONLY: synthetic data is not physical validation.")


if __name__ == "__main__":
    main()
