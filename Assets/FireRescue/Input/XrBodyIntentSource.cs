using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[AddComponentMenu("Fire Rescue/Input/XR Body Intent Source")]
public sealed class XrBodyIntentSource : MonoBehaviour, IOperatorIntentSource
{
    [Header("追踪点")]
    public Transform head;
    public Transform locomotionHand;
    public Transform extinguisherHand;

    [Header("自然动作阈值")]
    [Min(0.01f)] public float forwardReach = 0.18f;
    [Min(0.01f)] public float sideReach = 0.16f;
    [Range(0f, 1f)] public float triggerThreshold = 0.25f;
    [Range(0f, 1f)] public float deadzone = 0.15f;

    [Header("输入动作")]
    public InputActionReference extinguishAction;
    public InputActionReference emergencyStopAction;

    [SerializeField] private bool manageActionLifetime = true;

    private void OnEnable()
    {
        if (!manageActionLifetime)
            return;

        extinguishAction?.action?.Enable();
        emergencyStopAction?.action?.Enable();
    }

    private void OnDisable()
    {
        if (!manageActionLifetime)
            return;

        extinguishAction?.action?.Disable();
        emergencyStopAction?.action?.Disable();
    }

    public OperatorIntent ReadIntent()
    {
        bool emergencyStop = ReadPressed(emergencyStopAction);
        bool extinguish = ReadPressed(extinguishAction);
        Vector2 movement = ReadBodyMovement();
        OperatorIntentKind kind = OperatorIntentMapping.FromInput(
            movement,
            extinguish,
            emergencyStop,
            deadzone);
        return OperatorIntentMapping.Create(kind, Time.unscaledTimeAsDouble);
    }

    private Vector2 ReadBodyMovement()
    {
        if (head == null || locomotionHand == null)
            return Vector2.zero;

        Vector3 localHand = head.InverseTransformPoint(locomotionHand.position);
        float forward = Mathf.Clamp(localHand.z / Mathf.Max(0.01f, forwardReach), -1f, 1f);
        float side = Mathf.Clamp(localHand.x / Mathf.Max(0.01f, sideReach), -1f, 1f);
        return new Vector2(side, forward);
    }

    private bool ReadPressed(InputActionReference actionReference)
    {
        if (actionReference == null || actionReference.action == null)
            return false;

        try
        {
            return actionReference.action.ReadValue<float>() >= triggerThreshold;
        }
        catch
        {
            return actionReference.action.activeControl is ButtonControl button
                && button.isPressed;
        }
    }

}
