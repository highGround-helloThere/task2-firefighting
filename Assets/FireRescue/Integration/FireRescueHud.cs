using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[AddComponentMenu("Fire Rescue/Integration/Fire Rescue HUD")]
public sealed class FireRescueHud : MonoBehaviour, IAutonomousNavigationProvider
{
    public FireRescueControlMode ControlMode { get; private set; } = FireRescueControlMode.Manual;
    public FireRescueNavigationState NavigationState { get; private set; } = FireRescueNavigationState.IDLE;
    public string PlannedDirection { get; private set; } = "-";
    public bool CanTakeOver => ControlMode == FireRescueControlMode.Auto;
    private Text statusText;
    private PerceptionUdpReceiver perception;
    private RobotVideoReceiver video;
    private OperatorIntentRouter router;
    private bool emergency;
    private PerceptionTelemetryMessage latest;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<FireRescueHud>() == null)
            new GameObject("Fire Rescue HUD").AddComponent<FireRescueHud>();
    }

    private void Awake()
    {
        perception = FindFirstObjectByType<PerceptionUdpReceiver>();
        video = FindFirstObjectByType<RobotVideoReceiver>();
        router = FindFirstObjectByType<OperatorIntentRouter>();
        BuildView();
        if (perception != null) perception.onTelemetry.AddListener(OnTelemetry);
        if (router != null) router.onEmergencyStopChanged.AddListener(OnEmergencyChanged);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame && !emergency)
            ControlMode = ControlMode == FireRescueControlMode.Auto ? FireRescueControlMode.Manual : FireRescueControlMode.Auto;
        if (emergency) ControlMode = FireRescueControlMode.EmergencyStop;
        RenderStatus();
    }

    public void SetAutonomousState(FireRescueNavigationState state, string direction)
    {
        NavigationState = state;
        PlannedDirection = string.IsNullOrEmpty(direction) ? "-" : direction;
    }

    private void OnTelemetry(PerceptionTelemetryMessage message) { latest = message; }
    private void OnEmergencyChanged(bool value) { emergency = value; }

    private void BuildView()
    {
        GameObject canvasObject = new GameObject("FireRescueStatusCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject panel = new GameObject("Status", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f); rect.anchoredPosition = new Vector2(24f, -24f);
        rect.sizeDelta = new Vector2(520f, 220f);
        panel.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.05f, 0.9f);

        GameObject textObject = new GameObject("StatusText", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(panel.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 14f); textRect.offsetMax = new Vector2(-18f, -14f);
        statusText = textObject.GetComponent<Text>();
        statusText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        statusText.fontSize = 22; statusText.color = Color.white;
        statusText.alignment = TextAnchor.UpperLeft; statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
    }

    private void RenderStatus()
    {
        if (statusText == null) return;
        bool perceptionOnline = perception != null && perception.IsConnected;
        bool videoOnline = video != null && video.IsConnected;
        bool detected = perception != null && perception.IsDetected;
        string obstacle = latest != null && latest.dynamic_obstacle ? "障碍物警告: " + latest.obstacle_direction : "障碍物: 无";
        float confidence = latest == null ? 0f : latest.FireConfidence;
        var builder = new StringBuilder();
        builder.AppendLine("救火任务监控");
        builder.AppendLine("模式: " + ControlMode + "    导航: " + NavigationState);
        builder.AppendLine("视频: " + (videoOnline ? "在线" : "断线") + "    感知: " + (perceptionOnline ? "在线" : "断线"));
        builder.AppendLine("火源: " + (detected ? "已发现" : "未发现") + "    置信度: " + confidence.ToString("P0"));
        builder.AppendLine(obstacle + "    计划方向: " + PlannedDirection);
        builder.AppendLine(emergency ? "急停已生效" : "WASD移动  J灭火  Esc急停  M切换模式");
        statusText.text = builder.ToString();
        statusText.color = emergency || (latest != null && latest.dynamic_obstacle) ? new Color(1f, 0.55f, 0.35f) : Color.white;
    }
}
