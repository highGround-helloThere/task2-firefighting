using NUnit.Framework;

public sealed class OperatorIntentGateTests
{
    [Test]
    public void MovementCanRepeatWhileHeld()
    {
        var gate = new OperatorIntentGate(holdSeconds: 0.25, cooldownSeconds: 0.5);

        OperatorIntent? first = gate.Evaluate(
            OperatorIntentKind.ForwardSmall,
            isActive: true,
            emergencyStopActive: false,
            timestampSeconds: 1.0);
        OperatorIntent? second = gate.Evaluate(
            OperatorIntentKind.ForwardSmall,
            isActive: true,
            emergencyStopActive: false,
            timestampSeconds: 1.1);

        Assert.That(first?.Kind, Is.EqualTo(OperatorIntentKind.ForwardSmall));
        Assert.That(first?.IsOneShot, Is.False);
        Assert.That(second?.Kind, Is.EqualTo(OperatorIntentKind.ForwardSmall));
    }

    [Test]
    public void ExtinguishRequiresHoldAndEmitsOnlyOnce()
    {
        var gate = new OperatorIntentGate(holdSeconds: 0.25, cooldownSeconds: 0.5);

        Assert.That(gate.Evaluate(
            OperatorIntentKind.Extinguish, true, false, 2.0), Is.Null);

        OperatorIntent? triggered = gate.Evaluate(
            OperatorIntentKind.Extinguish, true, false, 2.26);

        Assert.That(triggered?.Kind, Is.EqualTo(OperatorIntentKind.Extinguish));
        Assert.That(triggered?.IsOneShot, Is.True);
        Assert.That(gate.Evaluate(
            OperatorIntentKind.Extinguish, true, false, 3.0), Is.Null);
    }

    [Test]
    public void OneShotMustReturnToNeutralBeforeRetriggering()
    {
        var gate = new OperatorIntentGate(holdSeconds: 0.25, cooldownSeconds: 0.5);

        gate.Evaluate(OperatorIntentKind.Extinguish, true, false, 1.0);
        Assert.That(gate.Evaluate(
            OperatorIntentKind.Extinguish, true, false, 1.26), Is.Not.Null);

        gate.Evaluate(OperatorIntentKind.Stop, false, false, 1.3);
        gate.Evaluate(OperatorIntentKind.Extinguish, true, false, 1.5);
        Assert.That(gate.Evaluate(
            OperatorIntentKind.Extinguish, true, false, 1.76), Is.Null);

        gate.Evaluate(OperatorIntentKind.Stop, false, false, 1.9);
        gate.Evaluate(OperatorIntentKind.Extinguish, true, false, 2.0);
        Assert.That(gate.Evaluate(
            OperatorIntentKind.Extinguish, true, false, 2.26), Is.Not.Null);
    }

    [Test]
    public void EmergencyStopImmediatelyOverridesMovement()
    {
        var gate = new OperatorIntentGate(holdSeconds: 0.25, cooldownSeconds: 0.5);

        OperatorIntent? result = gate.Evaluate(
            OperatorIntentKind.ForwardSmall,
            isActive: true,
            emergencyStopActive: true,
            timestampSeconds: 4.0);

        Assert.That(result?.Kind, Is.EqualTo(OperatorIntentKind.EmergencyStop));
        Assert.That(result?.IsOneShot, Is.True);
    }
}
