# Orange Pi 代码说明

## robot-edge-gateway

- `main.py`：控制网关入口。
- `config.yaml`：Unity、Orange Pi 和 TonyPi 的地址与端口。
- `edge_gateway/control_proxy.py`：把 Unity 的 TCP 控制数据转发到 TonyPi `192.168.149.1:5075`。

运行：

```bash
cd robot-edge-gateway
python3 main.py
```

## robot-perception

- `main.py`：视觉程序入口。
- `config.yaml`：视频地址、识别阈值和 UDP 参数。
- `perception/video_source.py`：读取 TonyPi 摄像头视频。
- `perception/detector.py`：使用 OpenCV 颜色和轮廓识别目标。
- `perception/udp_telemetry.py`：向 Unity UDP `6101` 发送识别结果。
- `perception/snapshot_server.py`：在 Orange Pi `8080` 提供视频画面。
- `perception/telemetry.py`：输出运行日志。

运行：

```bash
cd robot-perception
python3 main.py
```
