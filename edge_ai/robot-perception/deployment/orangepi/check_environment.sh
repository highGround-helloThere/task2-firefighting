#!/usr/bin/env bash
set -u

failures=0

run_check() {
    label="$1"
    shift
    echo "=== ${label} ==="
    if "$@"; then
        echo "PASS: ${label}"
    else
        echo "MISSING/FAILED: ${label}"
        failures=$((failures + 1))
    fi
    echo
}

echo "Orange Pi AIpro / Ascend environment check (read-only)"
echo "UNVERIFIED WITHOUT HARDWARE"
echo
run_check "machine architecture" uname -m
run_check "kernel" uname -a
run_check "Python" python3 --version
run_check "OpenCV Python" python3 -c "import cv2; print(cv2.__version__)"
run_check "Ascend device/driver" npu-smi info
run_check "ATC converter" atc --version

echo "=== CANN environment ==="
echo "ASCEND_TOOLKIT_HOME=${ASCEND_TOOLKIT_HOME:-<unset>}"
echo "ASCEND_HOME_PATH=${ASCEND_HOME_PATH:-<unset>}"
echo "LD_LIBRARY_PATH=${LD_LIBRARY_PATH:-<unset>}"
if [ -f /usr/local/Ascend/ascend-toolkit/set_env.sh ]; then
    echo "Found /usr/local/Ascend/ascend-toolkit/set_env.sh"
elif [ -f "${HOME}/Ascend/ascend-toolkit/set_env.sh" ]; then
    echo "Found ${HOME}/Ascend/ascend-toolkit/set_env.sh"
elif [ -f "${HOME}/Ascend/cann/set_env.sh" ]; then
    echo "Found ${HOME}/Ascend/cann/set_env.sh"
else
    echo "MISSING: known CANN set_env.sh path"
    failures=$((failures + 1))
fi
echo

if [ "$failures" -eq 0 ]; then
    echo "Environment checks: PASS (runtime/model still require device validation)"
    exit 0
fi
echo "Environment checks: ${failures} missing or failed item(s)"
exit 1
