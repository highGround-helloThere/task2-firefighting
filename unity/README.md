# Unity 代码说明

- `ganzhi.unitypackage`：课程场景包。
- `RobotSyncManager.cs`：读取 PICO 输入并发送机器人控制指令。
- `RobotVideoReceiver.cs`、`RobotVideoSource.cs`：接收并显示机器人视频。
- `PerceptionUdpReceiver.cs`、`PerceptionTelemetryMessage.cs`：接收并解析 Orange Pi 感知数据。
- `PerceptionFireBridge.cs`：把识别结果连接到火焰事件。
- `LightControl.cs`：生成和管理火焰。
- `ExtinguisherSprayController.cs`：处理灭火器喷射和熄灭判定。

先按上级目录的 `Unity依赖说明.md` 安装依赖，再导入 `ganzhi.unitypackage`。

通信端口：控制 TCP `5075`、视频 TCP `8080`、感知 UDP `6101`。

## 当前增量

- `FireRescue/Input/`：设备无关的操作意图、输入接口和一次性动作安全门控。
- `Tests/Editor/OperatorIntentGateTests.cs`：覆盖移动心跳、灭火保持/冷却/回中和急停优先级。
- `Samples/XR Interaction Toolkit/3.1.1/Starter Assets/`：补回课程包缺失的 XRI 官方程序集定义，解决 `ComponentLocatorUtility<T>` 的 `CS0122` 错误。
- `Editor/PicoLivePreviewPlayModeCleanup.cs`：PICO SDK 未安装时跳过 Live Preview 专用清理，保持无设备编译能力。

将本目录中的增量文件复制到 Unity 工程的 `Assets/` 下，并保持相同的相对路径。课程场景包和第三方美术资源不进入 Git 仓库。
