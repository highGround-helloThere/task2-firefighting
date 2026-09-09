#!/usr/bin/env python3
"""Export YOLO weights to a static batch-1 ONNX model and validate the graph."""

import argparse
import os
import shutil
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[1]


def parse_args():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--weights",
        type=Path,
        default=PROJECT_ROOT / "runs" / "perception" / "demo" / "weights" / "best.pt",
    )
    parser.add_argument("--output", type=Path)
    parser.add_argument("--imgsz", type=int, default=640)
    parser.add_argument("--opset", type=int, default=12)
    return parser.parse_args()


def main():
    args = parse_args()
    if not args.weights.exists():
        raise FileNotFoundError("weights not found: {}".format(args.weights))
    config_dir = PROJECT_ROOT / "runs" / "ultralytics-config"
    config_dir.mkdir(parents=True, exist_ok=True)
    os.environ.setdefault("YOLO_CONFIG_DIR", str(config_dir))
    try:
        import onnx
        from ultralytics import YOLO
    except ImportError as exc:
        raise RuntimeError("install requirements-ai.txt before export") from exc

    model = YOLO(str(args.weights.resolve()))
    exported = Path(
        model.export(
            format="onnx",
            imgsz=args.imgsz,
            opset=args.opset,
            dynamic=False,
            batch=1,
            simplify=False,
        )
    ).resolve()
    if args.output:
        destination = args.output.resolve()
        destination.parent.mkdir(parents=True, exist_ok=True)
        if destination != exported:
            shutil.copy2(exported, destination)
        exported = destination

    graph = onnx.load(str(exported))
    onnx.checker.check_model(graph)
    input_tensor = graph.graph.input[0]
    shape = [dimension.dim_value for dimension in input_tensor.type.tensor_type.shape.dim]
    print("ONNX validation: PASS")
    print("path={}".format(exported))
    print("input_name={} input_shape={}".format(input_tensor.name, shape))


if __name__ == "__main__":
    main()
