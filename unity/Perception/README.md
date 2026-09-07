# Orange Pi perception telemetry

This module is independent from `RobotSyncManager` and the TonyPi TCP 5075
control connection. `PerceptionUdpReceiver` only receives UDP telemetry on port
6101 and never emits robot commands.

Local test without an Orange Pi:

1. Create an empty GameObject in the Unity scene.
2. Add the `PerceptionUdpReceiver` component.
3. Enable `Log Packets` for initial testing.
4. Enter Play mode.
5. From the repository root, run:

   ```powershell
   python .\robot-perception\tools\telemetry_simulator.py --host 127.0.0.1 --port 6101 --duration 30
   ```

The receiver changes to disconnected when no valid packet arrives for 0.5
seconds. This status has no effect on robot motion.
