# Unity 场景包依赖说明

| 包                         | 版本       |
| -------------------------- | ---------- |
| glTFast                    | `6.10.2`  |
| Input System               | `1.12.0` |
| Shader Graph               | `14.0.12` |
| XR Interaction Toolkit     | `3.1.1`  |
| XR Plug-in Management      | `4.5.1`  |
| OpenXR Plugin              | `1.10.0` |
| PICO Unity Integration SDK | `3.4.0`  |
| Android Build Support | 与 Unity `2022.3.56f1` 同版本，包含 Android SDK、NDK 和 OpenJDK |

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
- 全组统一使用 Unity `2022.3.56f1`，并安装同版本 Android Build Support、Android SDK & NDK Tools、OpenJDK。
- 安装与 Unity 2022.3.56f1 匹配的 Shader Graph `14.0.12`。
- PICO 真机验收需要安装 Android Build Support；无 Android 模块时可以继续进行 Windows/键盘模式的代码和 EditMode 验收，但不能切换 Android 或打包 APK。
