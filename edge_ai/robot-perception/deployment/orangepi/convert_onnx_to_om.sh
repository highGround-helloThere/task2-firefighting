#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -lt 1 ] || [ "$#" -gt 2 ]; then
    echo "Usage: $0 MODEL.onnx [OUTPUT_PREFIX]" >&2
    exit 2
fi

model="$1"
output_prefix="${2:-${model%.onnx}}"
soc_version="${SOC_VERSION:-Ascend310B1}"

if [ ! -f "$model" ]; then
    echo "ONNX model not found: $model" >&2
    exit 1
fi
command -v python3 >/dev/null || { echo "python3 is required" >&2; exit 1; }
command -v atc >/dev/null || { echo "atc is unavailable; source the CANN environment" >&2; exit 1; }

model_info="$(python3 - "$model" <<'PY'
import sys
import onnx

graph = onnx.load(sys.argv[1]).graph
if len(graph.input) != 1:
    raise SystemExit("expected exactly one ONNX input, found {}".format(len(graph.input)))
tensor = graph.input[0]
dims = [item.dim_value for item in tensor.type.tensor_type.shape.dim]
if len(dims) != 4 or any(value <= 0 for value in dims):
    raise SystemExit("expected static NCHW input, found {}".format(dims))
print("{} {}".format(tensor.name, ",".join(str(value) for value in dims)))
PY
)"
read -r input_name input_shape <<<"$model_info"

echo "UNVERIFIED WITHOUT HARDWARE"
echo "model=$model"
echo "input=${input_name}:${input_shape}"
echo "soc_version=$soc_version"
echo "output=${output_prefix}.om"

atc \
    --model="$model" \
    --framework=5 \
    --output="$output_prefix" \
    --input_format=NCHW \
    --input_shape="${input_name}:${input_shape}" \
    --soc_version="$soc_version"
