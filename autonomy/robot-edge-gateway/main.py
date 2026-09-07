#!/usr/bin/env python3
"""Orange Pi transparent control gateway for Unity and TonyPi."""

import argparse
import signal
import sys
from pathlib import Path

import yaml

from edge_gateway.control_proxy import ControlProxyServer


def load_config(path):
    with path.open("r", encoding="utf-8") as stream:
        config = yaml.safe_load(stream)
    if not isinstance(config, dict):
        raise ValueError("configuration root must be a mapping")

    safety = config.get("safety", {})
    if safety.get("transparent_forwarding_only") is not True:
        raise ValueError("gateway must remain byte-transparent")
    if safety.get("generated_motion_commands_allowed") is not False:
        raise ValueError("gateway must not generate motion commands")
    if int(safety.get("max_clients", 0)) != 1:
        raise ValueError("exactly one Unity control client is required")
    return config


def parse_args():
    default_config = Path(__file__).resolve().with_name("config.yaml")
    parser = argparse.ArgumentParser(description="Orange Pi control gateway")
    parser.add_argument("--config", type=Path, default=default_config)
    return parser.parse_args()


def main():
    args = parse_args()
    try:
        config = load_config(args.config)
        server = ControlProxyServer(config["control_proxy"])
    except Exception as exc:
        print("fatal: {}: {}".format(type(exc).__name__, exc), file=sys.stderr)
        return 1

    def request_stop(_signum, _frame):
        server.stop()

    signal.signal(signal.SIGINT, request_stop)
    signal.signal(signal.SIGTERM, request_stop)

    try:
        server.serve_forever()
    except KeyboardInterrupt:
        server.stop()
    return 0


if __name__ == "__main__":
    sys.exit(main())
