# Member C: edge perception

The member-C pipeline is intentionally small:

```text
TonyPi camera / local video / webcam
  -> Orange Pi or PC video source
  -> baseline LAB detector or YOLOv8n model
  -> direction, temporal stability, obstacle occupancy
  -> JSON Lines + UDP
  -> Unity / autonomy
```

The default remains the original LAB/contour red-ball baseline and UDP schema v1.
No robot control code is included here.

## Install and baseline

```bash
python -m pip install -r requirements.txt
python tools/baseline_smoke_test.py
```

AI training, PT/ONNX inference, and export use separate dependencies:

```bash
python -m pip install -r requirements-ai.txt
```

Select `detection.backend: baseline` or `model` in `config.yaml`. The model
backend uses the same `detector.process(frame)` interface and accepts a YOLO `.pt`
or `.onnx` path. The baseline remains available as an explicit fire-marker
fallback. `orangepi_npu` fails clearly until an Ascend runtime is installed.

## PC local demo

Run from `edge_ai/robot-perception` so relative video paths remain simple on
Windows:

```bash
python tools/generate_demo_video.py
python main.py --source local --input data/demo.avi --max-frames 90 \
  --udp-host 127.0.0.1 --schema-version 2 --no-snapshot \
  --annotated-output runs/demo-annotated.avi
```

Use `--source webcam --camera-index 0` for a PC camera. The default `mjpeg`
source retains TonyPi URL failover and now applies OpenCV open/read timeouts.
When a source is unavailable, UDP status packets continue with `video_ok=false`
after the bounded read/open attempt; stale frames are not reused.

## Unified perception

The model result contains fire state/center/confidence/direction/alignment,
dynamic-obstacle state/direction/confidence, and `free_left/front/right`.
Fire direction uses configurable 0.4/0.6 width ratios. Bottom-half obstacle boxes
block three explainable left/front/right ROIs. Fire uses hit confirmation,
miss release, and stale timeout; obstacles block quickly and clear slowly. A true
stable state always retains valid geometry.

## Data, training, and ONNX

```bash
python training/prepare_demo_dataset.py
python training/train.py --epochs 20 --imgsz 416 --batch -1 --device 0
python training/export.py --imgsz 640 --opset 12
```

See `training/README.md` for the short smoke commands. The generated YOLO dataset
uses `fire_marker`, `dynamic_obstacle`, and `static_obstacle`. Replace `--data`
with a real dataset YAML after collecting TonyPi images.

**Synthetic dataset != final physical validation.** It only validates data,
training, inference, and export plumbing. Datasets, runs, videos, PT, ONNX, and OM
artifacts are ignored by Git.

Ultralytics documents the Python training and static/dynamic ONNX export options:
<https://docs.ultralytics.com/modes/train> and
<https://docs.ultralytics.com/modes/export>.

## UDP compatibility

`telemetry.udp.schema_version` defaults to `1` for the current Unity consumer.
Schema v1 retains `detected`, `candidate_detected`, center, radius, score,
shape metrics, frame size, and processing time.

Schema v2 adds `fire_detected`, fire geometry/confidence/direction/alignment,
dynamic-obstacle fields, three free-space booleans, and processing time. Both
schemas include `type`, `schema_version`, `source`, fixed `session_id`, increasing
`seq`, `sent_at_ms`, and `video_ok`.

```bash
python tools/telemetry_simulator.py --host 127.0.0.1 --port 6101 --duration 30
python tools/telemetry_simulator.py --host 127.0.0.1 --port 6101 --duration 30 --schema-version 2
```

## Tests and Orange Pi

```bash
python tools/baseline_smoke_test.py
python tools/perception_smoke_test.py
```

Orange Pi preparation is in `deployment/orangepi/`. **Orange Pi OM inference
requires hardware validation.** Remaining physical work is camera/data capture,
quick fine-tuning, ATC conversion, ACL runtime integration, NPU parity/performance
testing, and Unity/autonomy integration.
