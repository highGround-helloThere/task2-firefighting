public sealed class OperatorIntentProcessor
{
    private readonly OperatorIntentGate gate;

    public OperatorIntentProcessor(double holdSeconds, double cooldownSeconds)
    {
        gate = new OperatorIntentGate(holdSeconds, cooldownSeconds);
    }

    public OperatorIntent? Process(OperatorIntent rawIntent)
    {
        bool isActive = rawIntent.Kind != OperatorIntentKind.Stop;
        bool emergencyStop = rawIntent.Kind == OperatorIntentKind.EmergencyStop;
        return gate.Evaluate(
            rawIntent.Kind,
            isActive,
            emergencyStop,
            rawIntent.TimestampSeconds);
    }
}
