# D - autonomous control inside the course framework

## Existing communication path

No new service or port is introduced.

```text
TonyPi camera -> Orange Pi robot-perception
              -> UDP 6101 -> Unity RobotSyncManager
              -> TCP 5075 -> course transparent gateway -> TonyPi
```

The Unity maze/firefighting scene is prepared by member A. D does not create,
reconstruct, or edit the Unity maze.

## Orange Pi processing

`perception/autonomy.py` receives the same camera frame already read by the
course `main.py`. It performs:

- left/front/right image-region clearance estimation;
- localized motion detection with camera-motion compensation;
- dynamic-obstacle stop or turn-away decisions;
- left-hand wall following without a preloaded maze map;
- fire alignment and approach using the existing red-target result;
- rate-limited extinguishing commands.

The result is added to the existing perception telemetry packet by
`perception/udp_telemetry.py`. The destination remains Unity UDP 6101.

## Unity integration

`PerceptionTelemetryMessage.cs` receives the additional autonomous fields.
Attach `AutonomyControlBridge.cs` to a Unity GameObject and assign the existing
`PerceptionUdpReceiver` and `RobotSyncManager` references.

The bridge gives the autonomous velocity to `RobotSyncManager`, which sends
the existing `{v, steer, grab, t}` format through TCP 5075. It sends the
existing `CMD:right_grip` command for extinguishing.

- Call `EnableAutonomousMode()` from a Unity button to start AUTO.
- Call `DisableAutonomousMode()` to return to manual control.
- Press `U` while keyboard debug is enabled to toggle AUTO.
- Moving a VR stick beyond the takeover threshold exits AUTO.
- Missing UDP telemetry for 0.6 seconds produces zero velocity.

## What remains for equipment testing

The camera ROI, edge-density, motion-area, fire-distance, walking-speed, and
turn-speed values in `config.yaml` must be calibrated against the physical
cardboard maze and TonyPi camera. The virtual maze is not used as the
autonomous route map.
