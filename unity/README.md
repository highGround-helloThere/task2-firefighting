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
