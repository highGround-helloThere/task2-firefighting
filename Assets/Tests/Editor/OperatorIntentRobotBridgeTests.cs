using NUnit.Framework;
using UnityEngine;

public sealed class OperatorIntentRobotBridgeTests
{
    [Test]
    public void BridgePublishesMotionAndImmediateCommands()
    {
        var root = new GameObject("OperatorIntentRobotBridgeTest");
        var bridge = root.AddComponent<OperatorIntentRobotBridge>();
        float linear = 0f;
        float steer = 0f;
        string immediateCommand = null;
        int localActions = 0;
        bridge.onMotionCommand.AddListener((v, s) =>
        {
            linear = v;
            steer = s;
        });
        bridge.onImmediateCommand.AddListener(cmd => immediateCommand = cmd);
        bridge.onLocalActionRequested.AddListener(() => localActions++);

        bridge.OnIntent(OperatorIntentKind.ForwardSmall);
        Assert.That(linear, Is.EqualTo(bridge.commandSettings.linearSpeed));
        Assert.That(steer, Is.EqualTo(0f));

        bridge.OnIntent(OperatorIntentKind.Extinguish);
        Assert.That(immediateCommand, Is.EqualTo(bridge.commandSettings.extinguishCommand));
        Assert.That(localActions, Is.EqualTo(1));
        Object.DestroyImmediate(root);
    }

    [Test]
    public void BridgeUpdatesRobotAvatarState()
    {
        var root = new GameObject("OperatorIntentRobotBridgeAvatarTest");
        var bridge = root.AddComponent<OperatorIntentRobotBridge>();
        var avatar = root.AddComponent<RobotAvatarController>();
        bridge.avatarController = avatar;

        bridge.OnIntent(OperatorIntentKind.TurnRightSmall);
        Assert.That(avatar.CurrentState, Is.EqualTo(RobotAvatarState.TurnRight));

        bridge.OnIntent(OperatorIntentKind.Stop);
        Assert.That(avatar.CurrentState, Is.EqualTo(RobotAvatarState.Stand));
        Object.DestroyImmediate(root);
    }
}
