using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

[AddComponentMenu("Fire Rescue/Integration/Perception Telemetry Simulator")]
public sealed class PerceptionTelemetrySimulator : MonoBehaviour
{
    [Tooltip("仅用于 Play Mode 无设备验收，默认关闭。")]
    public bool enabledForAcceptance;
    public string targetIp = "127.0.0.1";
    public int targetPort = 6101;
    public Key fireKey = Key.F;
    public Key obstacleKey = Key.O;
    public Key clearKey = Key.C;
    public float sendInterval = 0.15f;
    private UdpClient client;
    private long sequence;
    private float nextSend;
    private bool fireDetected;
    private bool obstacleDetected;

    private void OnEnable()
    {
        if (enabledForAcceptance) client = new UdpClient();
    }

    private void Update()
    {
        if (!enabledForAcceptance || Keyboard.current == null) return;
        if (Keyboard.current[fireKey].wasPressedThisFrame) fireDetected = true;
        if (Keyboard.current[obstacleKey].wasPressedThisFrame) obstacleDetected = !obstacleDetected;
        if (Keyboard.current[clearKey].wasPressedThisFrame) { fireDetected = false; obstacleDetected = false; }
        if (Time.unscaledTime >= nextSend)
        {
            nextSend = Time.unscaledTime + Mathf.Max(0.05f, sendInterval);
            SendPacket();
        }
    }

    private void SendPacket()
    {
        if (client == null) return;
        var packet = new PerceptionPacket
        {
            type = "perception", schema_version = 2, source = "unity-simulator",
            session_id = "unity-acceptance", seq = ++sequence,
            sent_at_ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), state = "SIMULATED",
            video_ok = true, fire_detected = fireDetected, fire_center_x = fireDetected ? 418f : 0f,
            fire_center_y = fireDetected ? 236f : 0f, fire_confidence = fireDetected ? 0.91f : 0f,
            dynamic_obstacle = obstacleDetected, obstacle_direction = obstacleDetected ? "front" : "none",
            free_left = true, free_front = !obstacleDetected, free_right = true,
            frame_width = 640, frame_height = 360, processing_ms = 42.5f
        };
        byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(packet));
        client.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Parse(targetIp), targetPort));
    }

    private void OnDisable()
    {
        client?.Close();
        client = null;
    }

    [Serializable]
    private sealed class PerceptionPacket
    {
        public string type, source, session_id, state, obstacle_direction;
        public int schema_version, frame_width, frame_height;
        public long seq, sent_at_ms;
        public bool video_ok, fire_detected, dynamic_obstacle, free_left, free_front, free_right;
        public float fire_center_x, fire_center_y, fire_confidence, processing_ms;
    }
}
