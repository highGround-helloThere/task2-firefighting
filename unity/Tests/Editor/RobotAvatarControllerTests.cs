using NUnit.Framework;
using UnityEngine;

public sealed class RobotAvatarControllerTests
{
    [Test]
    public void StateChangesAreReportedOnlyWhenStateChanges()
    {
        var root = new GameObject("AvatarControllerTest");
        var controller = root.AddComponent<RobotAvatarController>();
        int notifications = 0;
        controller.onStateChanged.AddListener(_ => notifications++);

        controller.SetState(RobotAvatarState.WalkForward);
        controller.SetState(RobotAvatarState.WalkForward);
        controller.SetState(RobotAvatarState.Extinguish);

        Assert.That(controller.CurrentState, Is.EqualTo(RobotAvatarState.Extinguish));
        Assert.That(notifications, Is.EqualTo(2));
        Object.DestroyImmediate(root);
    }
}
