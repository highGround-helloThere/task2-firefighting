using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[AddComponentMenu("Fire Rescue/Input/Keyboard Intent Source")]
public sealed class KeyboardIntentSource : MonoBehaviour, IOperatorIntentSource
{
    [Header("移动")]
    public Key forwardKey = Key.W;
    public Key backwardKey = Key.S;
    public Key leftKey = Key.A;
    public Key rightKey = Key.D;

    [Header("动作")]
    public Key extinguishKey = Key.J;
    public Key emergencyStopKey = Key.Escape;
    [Range(0f, 1f)] public float deadzone = 0.15f;

    public OperatorIntent ReadIntent()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return OperatorIntentMapping.Create(
                OperatorIntentKind.Stop,
                Time.unscaledTimeAsDouble);

        bool emergencyStop = keyboard[emergencyStopKey].isPressed;
        bool extinguish = keyboard[extinguishKey].isPressed;
        Vector2 movement = new Vector2(
            Axis(keyboard[leftKey], keyboard[rightKey]),
            Axis(keyboard[backwardKey], keyboard[forwardKey]));
        OperatorIntentKind kind = OperatorIntentMapping.FromInput(
            movement,
            extinguish,
            emergencyStop,
            deadzone);
        return OperatorIntentMapping.Create(kind, Time.unscaledTimeAsDouble);
    }

    private static float Axis(KeyControl negative, KeyControl positive)
    {
        return (positive.isPressed ? 1f : 0f) - (negative.isPressed ? 1f : 0f);
    }
}
