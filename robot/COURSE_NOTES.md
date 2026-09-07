# TonyPi 代码说明

## 文件

- `TCP_connect.py`：监听 TCP `5075`，接收 Unity 控制指令并调用 TonyPi 动作组；通信超时后自动站立。

## 运行

将文件放到 TonyPi 后执行：

```bash
python3 TCP_connect.py
```

TonyPi 地址：`192.168.149.1`。程序依赖机器人系统自带的 `hiwonder.ActionGroupControl` 和对应动作组。
