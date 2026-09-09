# Orange Pi AIpro 20T deployment

**UNVERIFIED WITHOUT HARDWARE.** These files prepare the YOLOv8 ONNX-to-OM
workflow for an Ascend 310B-series Orange Pi. They have not been executed on the
target board.

## 1. Check the board

Install a CANN Toolkit and operator package compatible with the board image and
driver. Source the actual installation path, for example:

```bash
source /usr/local/Ascend/ascend-toolkit/set_env.sh
bash deployment/orangepi/check_environment.sh
```

The checker is read-only and reports architecture, Python, OpenCV, `npu-smi`,
`atc`, and common CANN environment paths. Huawei's CANN documentation describes
the same `set_env.sh` setup:
<https://www.hiascend.com/document/detail/zh/canncommercial/800/apiref/envvar/envref_07_0003.html>.

## 2. Export and convert

Export a static NCHW, batch-1 ONNX model on the PC:

```bash
python training/export.py --weights runs/perception/demo/weights/best.pt --imgsz 640
```

Copy the ONNX file and this directory to the board, install Python `onnx` for
input inspection, then run:

```bash
bash deployment/orangepi/convert_onnx_to_om.sh best.onnx best
```

The script reads the real ONNX input name and static shape before invoking ATC.
It defaults to `SOC_VERSION=Ascend310B1`; confirm the exact SoC reported for the
board/CANN combination and override when necessary:

```bash
SOC_VERSION=Ascend310B1 bash deployment/orangepi/convert_onnx_to_om.sh best.onnx best
```

## 3. Runtime status

The config backend name `orangepi_npu` is reserved, but currently raises
`Ascend runtime unavailable` instead of silently using CPU. An ACL/Ascend Python
runtime must be selected from the board's installed CANN examples and tested with
the generated OM model. YOLO output decoding/NMS and measured latency remain
hardware validation tasks.

Required board work: verify CANN/driver compatibility, convert ONNX to OM, run
real camera inference, check output parity, and measure latency/FPS/temperature.
