using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public sealed class OperatorIntentKindEvent : UnityEvent<OperatorIntentKind>
{
}

[AddComponentMenu("Fire Rescue/Input/Operator Intent Router")]
public sealed class OperatorIntentRouter : MonoBehaviour
{
    [Header("输入源")]
    [SerializeField] private MonoBehaviour sourceBehaviour;
    [Range(0f, 2f)] [SerializeField] private float holdSeconds = 0.25f;
    [Min(0f)] [SerializeField] private float cooldownSeconds = 0.5f;

    [Header("输出")]
    public OperatorIntentKindEvent onIntent = new OperatorIntentKindEvent();
    public UnityEvent<bool> onEmergencyStopChanged = new UnityEvent<bool>();

    private IOperatorIntentSource source;
    private OperatorIntentProcessor processor;
    private bool emergencyStopActive;

    public bool IsConfigured => sourceBehaviour is IOperatorIntentSource;

    private void Awake()
    {
        source = sourceBehaviour as IOperatorIntentSource;
        processor = new OperatorIntentProcessor(holdSeconds, cooldownSeconds);
        if (source == null)
            Debug.LogWarning("[OperatorIntentRouter] Input source must implement IOperatorIntentSource.", this);
    }

    private void Update()
    {
        if (source == null)
            return;

        OperatorIntent? accepted = processor.Process(source.ReadIntent());
        if (!accepted.HasValue)
            return;

        OperatorIntent intent = accepted.Value;
        bool nextEmergencyStop = intent.Kind == OperatorIntentKind.EmergencyStop;
        if (nextEmergencyStop != emergencyStopActive)
        {
            emergencyStopActive = nextEmergencyStop;
            onEmergencyStopChanged?.Invoke(emergencyStopActive);
        }

        onIntent?.Invoke(intent.Kind);
    }
}
