using NUnit.Framework;
using UnityEngine;

public sealed class OperatorIntentMappingTests
{
    [Test]
    public void MovementUsesDeadzoneAndDominantAxis()
    {
        Assert.That(
            OperatorIntentMapping.FromMovement(new Vector2(0f, 0.8f), 0.15f),
            Is.EqualTo(OperatorIntentKind.ForwardSmall));
        Assert.That(
            OperatorIntentMapping.FromMovement(new Vector2(-0.8f, 0f), 0.15f),
            Is.EqualTo(OperatorIntentKind.TurnLeftSmall));
        Assert.That(
            OperatorIntentMapping.FromMovement(new Vector2(0.1f, 0.1f), 0.15f),
            Is.EqualTo(OperatorIntentKind.Stop));
    }

    [Test]
    public void SafetyInputHasPriorityOverMovement()
    {
        Assert.That(
            OperatorIntentMapping.FromInput(
                new Vector2(0f, 1f), extinguish: true, emergencyStop: true),
            Is.EqualTo(OperatorIntentKind.EmergencyStop));
        Assert.That(
            OperatorIntentMapping.FromInput(
                new Vector2(0f, 1f), extinguish: true, emergencyStop: false),
            Is.EqualTo(OperatorIntentKind.Extinguish));
    }
}
