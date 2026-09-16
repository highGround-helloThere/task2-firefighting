using UnityEngine;

/// <summary>
/// Routes confirmed fire perception into the Unity visual event only.
/// </summary>
public sealed class PerceptionFireBridge : MonoBehaviour
{
    [SerializeField] private RobotSyncManager robotSyncManager;
    [SerializeField] private PerceptionUdpReceiver perceptionReceiver;
    [SerializeField] private LightControl fireControl;
    private string _activeColor;

    private void Awake()
    {
        if (robotSyncManager == null)
            robotSyncManager = FindObjectOfType<RobotSyncManager>();
        if (perceptionReceiver == null)
            perceptionReceiver = GetComponent<PerceptionUdpReceiver>();
        if (perceptionReceiver == null)
            perceptionReceiver = FindObjectOfType<PerceptionUdpReceiver>();
        if (fireControl == null)
            fireControl = FindObjectOfType<LightControl>();
    }

    private void OnEnable()
    {
        if (perceptionReceiver != null)
            perceptionReceiver.onTelemetry.AddListener(HandleTelemetry);
    }

    private void OnDisable()
    {
        if (perceptionReceiver != null)
            perceptionReceiver.onTelemetry.RemoveListener(HandleTelemetry);
    }

    public void HandleDetectionChanged(bool detected)
    {
        if (!detected)
            _activeColor = null;
    }

    public void HandleTelemetry(PerceptionTelemetryMessage message)
    {
        if (message == null || !message.video_ok || !message.detected)
        {
            _activeColor = null;
            return;
        }

        string color = ColorForTarget(message.target);
        if (string.IsNullOrEmpty(color) || color == _activeColor)
            return;
        _activeColor = color;
        if (color == "RED" && fireControl != null)
        {
            fireControl.HandleConfirmedFireDetection(message.target);
        }
        else if (robotSyncManager != null)
        {
            robotSyncManager.onColorSignalReceived?.Invoke(color);
        }
        else
        {
            Debug.LogWarning("[Perception Fire] Neither LightControl nor RobotSyncManager is available.");
        }
        Debug.Log($"[Perception Fire] Confirmed {message.target} -> {color} (visual only)");
    }

    private static string ColorForTarget(string target)
    {
        string normalized = (target ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.StartsWith("red")) return "RED";
        return null;
    }
}
