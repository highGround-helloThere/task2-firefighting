# Unity 代码说明

- `ganzhi.unitypackage`：课程场景包。
- `RobotSyncManager.cs`：读取 PICO 输入并发送机器人控制指令。
- `RobotVideoReceiver.cs`、`RobotVideoSource.cs`：接收并显示机器人视频。
- `PerceptionUdpReceiver.cs`、`PerceptionTelemetryMessage.cs`：接收并解析 Orange Pi 感知数据。
- `PerceptionFireBridge.cs`：把识别结果连接到火焰事件。
- `FireRescue/Input/OperatorIntentRobotBridge.cs`：把统一输入意图转换成机器人动作、一次性命令和 Avatar 状态。
- `LightControl.cs`：生成和管理火焰。
- `ExtinguisherSprayController.cs`：处理灭火器喷射和熄灭判定。

先按上级目录的 `Unity依赖说明.md` 安装依赖，再导入 `ganzhi.unitypackage`。

通信端口：控制 TCP `5075`、视频 TCP `8080`、感知 UDP `6101`。

## 现场验收入口

无 PICO 设备时，把 `KeyboardIntentSource` 挂到场景对象即可复现同一套抽象动作：

- `W/S`：前进/后退
- `A/D`：左转/右转
- `J`：灭火
- `Esc`：急停

PICO 设备接入时，把 `XrBodyIntentSource` 的 `head`、`locomotionHand`、`extinguisherHand` 指向 XR Origin 的追踪点，并把灭火和急停动作拖到对应的 `InputActionReference`。输入源只输出 `OperatorIntent`，机器人控制、火焰桥接和 HUD 不需要改设备代码。

如果要直接驱动现场验收链路，给场景添加 `OperatorIntentRouter`、`OperatorIntentRobotBridge` 和输入源即可；桥接器会自动订阅路由器，并自动查找场景中的 `RobotSyncManager` 和 `RobotAvatarController`。也可以在 Inspector 中显式指定这些引用。

`PicoThreePointAvatarDriver` 使用头部和左右控制器 Transform 驱动虚拟 Avatar，不依赖 PICO SDK 命名空间；安装 PICO SDK 后可直接复用 XR Origin 的追踪点，避免在没有 SDK 的机器上阻断编译。

### EditMode 验收

在 Unity Test Runner 中运行 `EditMode` 全部测试。当前覆盖输入安全门、输入映射、感知 JSON、感知火源重新触发、MJPEG 解析和 PICO 三点位姿。

## 当前增量

- `FireRescue/Input/`：设备无关的操作意图、输入接口和一次性动作安全门控。
- `FireRescue/Input/OperatorIntentRobotBridge.cs`：把统一输入落到机器人控制和 Avatar 状态。
- `Tests/Editor/OperatorIntentGateTests.cs`：覆盖移动心跳、灭火保持/冷却/回中和急停优先级。
- `Tests/Editor/OperatorIntentRobotCommandMappingTests.cs`、`OperatorIntentRobotBridgeTests.cs`：覆盖意图到动作和桥接输出。
- `Samples/XR Interaction Toolkit/3.1.1/Starter Assets/`：补回课程包缺失的 XRI 官方程序集定义，解决 `ComponentLocatorUtility<T>` 的 `CS0122` 错误。
- `Editor/PicoLivePreviewPlayModeCleanup.cs`：PICO SDK 未安装时跳过 Live Preview 专用清理，保持无设备编译能力。

将本目录中的增量文件复制到 Unity 工程的 `Assets/` 下，并保持相同的相对路径。课程场景包和第三方美术资源不进入 Git 仓库。
