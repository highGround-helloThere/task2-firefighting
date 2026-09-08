#!/usr/bin/env python3
"""Train a small YOLOv8n model on a YOLO-format perception dataset."""

import argparse
import os
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[1]


def parse_args():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--data",
        type=Path,
        default=PROJECT_ROOT / "data" / "perception" / "dataset.yaml",
    )
    parser.add_argument("--model", default="yolov8n.pt")
    parser.add_argument("--epochs", type=int, default=20)
    parser.add_argument("--imgsz", type=int, default=416)
    parser.add_argument("--batch", type=int, default=-1)
    parser.add_argument("--device", default="0")
    parser.add_argument("--name", default="demo")
    parser.add_argument("--workers", type=int, default=0)
    return parser.parse_args()


def main():
    args = parse_args()
    if not args.data.exists():
        raise FileNotFoundError(
            "dataset config not found; run training/prepare_demo_dataset.py first: {}".format(
                args.data
            )
        )
    config_dir = PROJECT_ROOT / "runs" / "ultralytics-config"
    config_dir.mkdir(parents=True, exist_ok=True)
    os.environ.setdefault("YOLO_CONFIG_DIR", str(config_dir))
    if os.name == "nt":
        os.environ.setdefault("MKL_THREADING_LAYER", "SEQUENTIAL")
        os.environ.setdefault("POLARS_SKIP_CPU_CHECK", "1")
    try:
        import torch
        from ultralytics import YOLO
    except ImportError as exc:
        raise RuntimeError("install requirements-ai.txt before training") from exc

    torch.set_num_threads(1)
    torch.set_num_interop_threads(1)

    model = YOLO(args.model)
    result = model.train(
        data=str(args.data.resolve()),
        epochs=args.epochs,
        imgsz=args.imgsz,
        batch=args.batch,
        device=args.device,
        workers=args.workers,
        project=str((PROJECT_ROOT / "runs" / "perception").resolve()),
        name=args.name,
        exist_ok=True,
        pretrained=True,
        plots=False,
        verbose=True,
    )
    best = Path(result.save_dir) / "weights" / "best.pt"
    print("Training complete. Best weights: {}".format(best))


if __name__ == "__main__":
    main()
