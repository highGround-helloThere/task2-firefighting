# Training and export

This directory provides the minimum YOLOv8n pipeline for the three logical
classes: `fire_marker`, `dynamic_obstacle`, and `static_obstacle`.

The generated synthetic dataset is only for pipeline validation. It does not
represent final TonyPi camera accuracy or physical validation.

```bash
python training/prepare_demo_dataset.py
python training/train.py --epochs 20 --imgsz 416 --batch -1 --device 0
python training/export.py --imgsz 640 --opset 12
```

For a short pipeline smoke test, use fewer images and one epoch:

```bash
python training/prepare_demo_dataset.py --train-count 12 --val-count 6 --size 320
python training/train.py --epochs 1 --imgsz 320 --batch 4 --device 0 --name smoke
python training/export.py --weights runs/perception/smoke/weights/best.pt --imgsz 320
```

On Windows, the training entry point selects MKL's sequential threading layer
to avoid duplicate OpenMP runtimes commonly present in Conda environments. It
uses Polars' compatibility runtime and skips a known false-positive feature check
that can reject `sse3` on supported Windows CPUs.

Replace `--data` with the real YOLO dataset YAML after collecting TonyPi images.
Weights, datasets, training runs, ONNX, and OM artifacts are intentionally ignored
by Git.
