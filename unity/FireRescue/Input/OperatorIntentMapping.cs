using UnityEngine;

public static class OperatorIntentMapping
{
    public static OperatorIntentKind FromMovement(Vector2 movement, float deadzone)
    {
        float clampedDeadzone = Mathf.Clamp01(deadzone);
        if (movement.magnitude <= clampedDeadzone)
            return OperatorIntentKind.Stop;

        if (Mathf.Abs(movement.y) >= Mathf.Abs(movement.x))
            return movement.y > 0f
                ? OperatorIntentKind.ForwardSmall
                : OperatorIntentKind.BackwardSmall;

        return movement.x > 0f
            ? OperatorIntentKind.TurnRightSmall
            : OperatorIntentKind.TurnLeftSmall;
    }

    public static OperatorIntentKind FromInput(
        Vector2 movement,
        bool extinguish,
        bool emergencyStop,
        float deadzone = 0.15f)
    {
        if (emergencyStop)
            return OperatorIntentKind.EmergencyStop;
        if (extinguish)
            return OperatorIntentKind.Extinguish;
        return FromMovement(movement, deadzone);
    }

    public static bool IsOneShot(OperatorIntentKind kind)
    {
        return kind == OperatorIntentKind.Extinguish
            || kind == OperatorIntentKind.RecoverFront
            || kind == OperatorIntentKind.RecoverBack
            || kind == OperatorIntentKind.EmergencyStop;
    }

    public static OperatorIntent Create(
        OperatorIntentKind kind,
        double timestampSeconds)
    {
        return new OperatorIntent(kind, timestampSeconds, IsOneShot(kind));
    }
}
