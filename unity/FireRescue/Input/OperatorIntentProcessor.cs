public sealed class OperatorIntentProcessor
{
    private readonly OperatorIntentGate gate;
    private bool movementWasActive;

    public OperatorIntentProcessor(double holdSeconds, double cooldownSeconds)
    {
        gate = new OperatorIntentGate(holdSeconds, cooldownSeconds);
    }

    public OperatorIntent? Process(OperatorIntent rawIntent)
    {
        if (rawIntent.Kind == OperatorIntentKind.Stop)
        {
            gate.Evaluate(
                rawIntent.Kind,
                false,
                false,
                rawIntent.TimestampSeconds);

            if (!movementWasActive)
                return null;

            movementWasActive = false;
            return rawIntent;
        }

        bool isActive = rawIntent.Kind != OperatorIntentKind.Stop;
        bool emergencyStop = rawIntent.Kind == OperatorIntentKind.EmergencyStop;
        OperatorIntent? accepted = gate.Evaluate(
            rawIntent.Kind,
            isActive,
            emergencyStop,
            rawIntent.TimestampSeconds);

        if (accepted.HasValue && !accepted.Value.IsOneShot)
            movementWasActive = true;

        if (accepted.HasValue && accepted.Value.Kind == OperatorIntentKind.EmergencyStop)
            movementWasActive = false;

        return accepted;
    }
}
