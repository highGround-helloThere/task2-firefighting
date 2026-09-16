using NUnit.Framework;
using UnityEngine;

public sealed class IntegrationContractTests
{
    [Test]
    public void SimulatedRobotPublishesConfirmedStateInsteadOfInputState()
    {
        var root = new GameObject("SimulatedRobotContractTest");
        var provider = root.AddComponent<SimulatedRobotStateProvider>();
        RobotAvatarState received = RobotAvatarState.Stand;
        provider.ConfirmedStateChanged += state => received = state;

        provider.ApplyMotion(1f, 0f);

        Assert.That(provider.ConfirmedState, Is.EqualTo(RobotAvatarState.WalkForward));
        Assert.That(received, Is.EqualTo(RobotAvatarState.WalkForward));
        Object.DestroyImmediate(root);
    }

    [Test]
    public void RobotManagerExposesConfirmedStateContract()
    {
        var root = new GameObject("RobotStateContractTest");
        var manager = root.AddComponent<RobotSyncManager>();
        RobotAvatarState received = RobotAvatarState.Stand;
        manager.ConfirmedStateChanged += state => received = state;

        manager.PublishConfirmedState(RobotAvatarState.Fallen);

        Assert.That(manager.ConfirmedState, Is.EqualTo(RobotAvatarState.Fallen));
        Assert.That(received, Is.EqualTo(RobotAvatarState.Fallen));
        Object.DestroyImmediate(root);
    }
}
