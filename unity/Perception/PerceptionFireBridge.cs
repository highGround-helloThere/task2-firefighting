using UnityEngine;

/// <summary>
/// Routes confirmed red, green, and blue perception targets into the existing
/// Unity color event and the matching TonyPi mission command.
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
            Debug.LogWarning("[Perception Color] Neither LightControl nor RobotSyncManager is available.");
        }
        robotSyncManager?.SendOperatorCommand("mission_" + color.ToLowerInvariant());
        Debug.Log($"[Perception Color] Confirmed {message.target} -> {color}");
    }

    private static string ColorForTarget(string target)
    {
        string normalized = (target ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.StartsWith("red")) return "RED";
        if (normalized.StartsWith("green")) return "GREEN";
        if (normalized.StartsWith("blue")) return "BLUE";
        return null;
    }
}
