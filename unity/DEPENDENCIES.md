# Unity 场景包依赖说明

| 包                         | 版本       |
| -------------------------- | ---------- |
| Input System               | `1.12.0` |
| Shader Graph               | `14.0.12` |
| XR Interaction Toolkit     | `3.1.1`  |
| XR Plug-in Management      | `4.5.1`  |
| OpenXR Plugin              | `1.10.0` |
| PICO Unity Integration SDK | `3.3.3`  |

## 固定设备地址

| 用途           | 地址或端口          |
| -------------- | ------------------- |
| Orange Pi      | `192.168.137.106` |
| 机器人控制 TCP | `5075`            |
| 视频服务 TCP   | `8080`            |
| 感知数据 UDP   | `6101`            |

## XR Project Validation 基线

- 在 Player Settings 中启用 `Run In Background`。
- 将 XR Interaction Toolkit 的第 31 个 Interaction Layer 命名为 `Teleport`。
- 安装与 Unity 2022.3.62f3c1 匹配的 Shader Graph `14.0.12`。
