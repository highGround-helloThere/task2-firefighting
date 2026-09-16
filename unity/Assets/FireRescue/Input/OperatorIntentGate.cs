public sealed class OperatorIntentGate
{
    private readonly double _holdSeconds;
    private readonly double _cooldownSeconds;

    private OperatorIntentKind _heldKind;
    private double _holdStartedAt = double.NaN;
    private double _lastOneShotAt = double.NegativeInfinity;
    private bool _oneShotLatched;
    private bool _emergencyLatched;

    public OperatorIntentGate(double holdSeconds, double cooldownSeconds)
    {
        _holdSeconds = holdSeconds;
        _cooldownSeconds = cooldownSeconds;
    }

    public OperatorIntent? Evaluate(
        OperatorIntentKind requested,
        bool isActive,
        bool emergencyStopActive,
        double timestampSeconds)
    {
        if (emergencyStopActive)
        {
            if (_emergencyLatched)
                return null;

            _emergencyLatched = true;
            return NewIntent(OperatorIntentKind.EmergencyStop, timestampSeconds, true);
        }

        _emergencyLatched = false;

        if (!isActive)
        {
            ResetToNeutral();
            return null;
        }

        if (!IsOneShot(requested))
            return NewIntent(requested, timestampSeconds, false);

        if (_oneShotLatched)
            return null;

        if (double.IsNaN(_holdStartedAt) || _heldKind != requested)
        {
            _heldKind = requested;
            _holdStartedAt = timestampSeconds;
            return null;
        }

        bool holdComplete = timestampSeconds - _holdStartedAt >= _holdSeconds;
        bool cooldownComplete = timestampSeconds - _lastOneShotAt > _cooldownSeconds;
        if (!holdComplete || !cooldownComplete)
            return null;

        _oneShotLatched = true;
        _lastOneShotAt = timestampSeconds;
        return NewIntent(requested, timestampSeconds, true);
    }

    private static bool IsOneShot(OperatorIntentKind kind)
    {
        return kind == OperatorIntentKind.Extinguish
            || kind == OperatorIntentKind.RecoverFront
            || kind == OperatorIntentKind.RecoverBack
            || kind == OperatorIntentKind.EmergencyStop;
    }

    private static OperatorIntent NewIntent(
        OperatorIntentKind kind,
        double timestampSeconds,
        bool isOneShot)
    {
        return new OperatorIntent(kind, timestampSeconds, isOneShot);
    }

    private void ResetToNeutral()
    {
        _holdStartedAt = double.NaN;
        _oneShotLatched = false;
    }
}
