using UnityEngine;

/// <summary>
/// Applies Orange Pi autonomous commands received through the existing
/// perception UDP 6101 connection to the existing RobotSyncManager TCP 5075
/// control path.
/// </summary>
public sealed class AutonomyControlBridge : MonoBehaviour
{
    [SerializeField] private PerceptionUdpReceiver perceptionReceiver;
    [SerializeField] private RobotSyncManager robotSyncManager;
    [SerializeField] private bool startInAutonomousMode;

    private void Awake()
    {
        if (perceptionReceiver == null)
            perceptionReceiver = FindObjectOfType<PerceptionUdpReceiver>();
        if (robotSyncManager == null)
            robotSyncManager = FindObjectOfType<RobotSyncManager>();
    }

    private void OnEnable()
    {
        if (perceptionReceiver != null)
        {
            perceptionReceiver.onTelemetry.AddListener(HandleTelemetry);
            perceptionReceiver.onConnectionChanged.AddListener(HandleConnectionChanged);
        }
    }

    private void Start()
    {
        robotSyncManager?.SetAutonomousMode(startInAutonomousMode);
    }

    private void OnDisable()
    {
        if (perceptionReceiver != null)
        {
            perceptionReceiver.onTelemetry.RemoveListener(HandleTelemetry);
            perceptionReceiver.onConnectionChanged.RemoveListener(HandleConnectionChanged);
        }
        robotSyncManager?.SetAutonomousMode(false);
    }

    public void SetAutonomousMode(bool enabled)
    {
        robotSyncManager?.SetAutonomousMode(enabled);
    }

    public void EnableAutonomousMode()
    {
        SetAutonomousMode(true);
    }

    public void DisableAutonomousMode()
    {
        SetAutonomousMode(false);
    }

    private void HandleTelemetry(PerceptionTelemetryMessage message)
    {
        robotSyncManager?.ApplyAutonomousTelemetry(message);
    }

    private void HandleConnectionChanged(bool connected)
    {
        if (!connected)
            robotSyncManager?.InvalidateAutonomousTelemetry();
    }
}
