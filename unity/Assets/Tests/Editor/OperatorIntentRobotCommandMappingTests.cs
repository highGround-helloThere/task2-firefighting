using NUnit.Framework;

public sealed class OperatorIntentRobotCommandMappingTests
{
    [Test]
    public void MovementIntentMapsToSmallRobotMotion()
    {
        var settings = new OperatorIntentRobotCommandSettings
        {
            linearSpeed = 0.45f,
            turnSpeed = 0.6f
        };

        OperatorIntentRobotCommand forward =
            OperatorIntentRobotCommandMapping.Map(OperatorIntentKind.ForwardSmall, settings);
        OperatorIntentRobotCommand turnLeft =
            OperatorIntentRobotCommandMapping.Map(OperatorIntentKind.TurnLeftSmall, settings);

        Assert.That(forward.linearVelocity, Is.EqualTo(0.45f));
        Assert.That(forward.steer, Is.EqualTo(0f));
        Assert.That(forward.avatarState, Is.EqualTo(RobotAvatarState.WalkForward));
        Assert.That(turnLeft.linearVelocity, Is.EqualTo(0f));
        Assert.That(turnLeft.steer, Is.EqualTo(-0.6f));
        Assert.That(turnLeft.avatarState, Is.EqualTo(RobotAvatarState.TurnLeft));
    }

    [Test]
    public void ActionIntentMapsToConfiguredImmediateCommand()
    {
        var settings = new OperatorIntentRobotCommandSettings
        {
            extinguishCommand = "right_grip",
            recoverFrontCommand = "right_trigger",
            recoverBackCommand = "left_trigger",
            emergencyStopCommand = "emergency_stop"
        };

        OperatorIntentRobotCommand extinguish =
            OperatorIntentRobotCommandMapping.Map(OperatorIntentKind.Extinguish, settings);
        OperatorIntentRobotCommand emergency =
            OperatorIntentRobotCommandMapping.Map(OperatorIntentKind.EmergencyStop, settings);

        Assert.That(extinguish.immediateCommand, Is.EqualTo("right_grip"));
        Assert.That(extinguish.triggerLocalAction, Is.True);
        Assert.That(extinguish.avatarState, Is.EqualTo(RobotAvatarState.Extinguish));
        Assert.That(emergency.immediateCommand, Is.EqualTo("emergency_stop"));
        Assert.That(emergency.linearVelocity, Is.EqualTo(0f));
        Assert.That(emergency.steer, Is.EqualTo(0f));
    }
}
