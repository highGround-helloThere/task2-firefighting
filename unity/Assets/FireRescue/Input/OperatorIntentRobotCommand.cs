using System;
using UnityEngine;

[Serializable]
public sealed class OperatorIntentRobotCommandSettings
{
    [Range(0f, 1f)] public float linearSpeed = 0.45f;
    [Range(0f, 1f)] public float turnSpeed = 0.6f;
    public string extinguishCommand = "right_grip";
    public string recoverFrontCommand = "right_trigger";
    public string recoverBackCommand = "left_trigger";
    public string emergencyStopCommand = "emergency_stop";
}

public readonly struct OperatorIntentRobotCommand
{
    public OperatorIntentRobotCommand(
        float linearVelocity,
        float steer,
        string immediateCommand,
        bool triggerLocalAction,
        RobotAvatarState avatarState)
    {
        this.linearVelocity = linearVelocity;
        this.steer = steer;
        this.immediateCommand = immediateCommand;
        this.triggerLocalAction = triggerLocalAction;
        this.avatarState = avatarState;
    }

    public readonly float linearVelocity;
    public readonly float steer;
    public readonly string immediateCommand;
    public readonly bool triggerLocalAction;
    public readonly RobotAvatarState avatarState;

    public bool HasImmediateCommand => !string.IsNullOrEmpty(immediateCommand);
}

public static class OperatorIntentRobotCommandMapping
{
    public static OperatorIntentRobotCommand Map(
        OperatorIntentKind kind,
        OperatorIntentRobotCommandSettings settings)
    {
        settings ??= new OperatorIntentRobotCommandSettings();

        switch (kind)
        {
            case OperatorIntentKind.ForwardSmall:
                return Motion(settings.linearSpeed, 0f, RobotAvatarState.WalkForward);
            case OperatorIntentKind.BackwardSmall:
                return Motion(-settings.linearSpeed, 0f, RobotAvatarState.WalkBackward);
            case OperatorIntentKind.TurnLeftSmall:
                return Motion(0f, -settings.turnSpeed, RobotAvatarState.TurnLeft);
            case OperatorIntentKind.TurnRightSmall:
                return Motion(0f, settings.turnSpeed, RobotAvatarState.TurnRight);
            case OperatorIntentKind.Extinguish:
                return Action(settings.extinguishCommand, true, RobotAvatarState.Extinguish);
            case OperatorIntentKind.RecoverFront:
                return Action(settings.recoverFrontCommand, false, RobotAvatarState.Recovering);
            case OperatorIntentKind.RecoverBack:
                return Action(settings.recoverBackCommand, false, RobotAvatarState.Recovering);
            case OperatorIntentKind.EmergencyStop:
                return Action(settings.emergencyStopCommand, false, RobotAvatarState.Stand);
            case OperatorIntentKind.Stand:
            case OperatorIntentKind.Stop:
            default:
                return Motion(0f, 0f, RobotAvatarState.Stand);
        }
    }

    private static OperatorIntentRobotCommand Motion(
        float linearVelocity,
        float steer,
        RobotAvatarState avatarState)
    {
        return new OperatorIntentRobotCommand(
            Mathf.Clamp(linearVelocity, -1f, 1f),
            Mathf.Clamp(steer, -1f, 1f),
            null,
            false,
            avatarState);
    }

    private static OperatorIntentRobotCommand Action(
        string immediateCommand,
        bool triggerLocalAction,
        RobotAvatarState avatarState)
    {
        return new OperatorIntentRobotCommand(
            0f,
            0f,
            immediateCommand,
            triggerLocalAction,
            avatarState);
    }
}
