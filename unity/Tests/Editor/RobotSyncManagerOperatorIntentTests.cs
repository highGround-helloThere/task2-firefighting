using NUnit.Framework;
using UnityEngine;

public sealed class RobotSyncManagerOperatorIntentTests
{
    [Test]
    public void ApplyOperatorMotionEnablesExternalInputAndClampsValues()
    {
        var root = new GameObject("RobotSyncManagerOperatorIntentTest");
        var manager = root.AddComponent<RobotSyncManager>();

        manager.ApplyOperatorMotion(2f, -2f);

        Assert.That(manager.useOperatorIntentInput, Is.True);
        Assert.That(manager.CurrentLinearVelocity, Is.EqualTo(1f));
        Assert.That(manager.CurrentSteer, Is.EqualTo(-1f));
        Object.DestroyImmediate(root);
    }
}
