using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public sealed class RobotMotionCommandEvent : UnityEvent<float, float>
{
}

[Serializable]
public sealed class RobotImmediateCommandEvent : UnityEvent<string>
{
}

[AddComponentMenu("Fire Rescue/Robot/Operator Intent Robot Bridge")]
public sealed class OperatorIntentRobotBridge : MonoBehaviour
{
    [Header("目标")]
    public RobotSyncManager robotSyncManager;
    public RobotAvatarController avatarController;
    public OperatorIntentRouter intentRouter;

    [Header("映射")]
    public OperatorIntentRobotCommandSettings commandSettings = new OperatorIntentRobotCommandSettings();

    [Header("输出")]
    public RobotMotionCommandEvent onMotionCommand = new RobotMotionCommandEvent();
    public RobotImmediateCommandEvent onImmediateCommand = new RobotImmediateCommandEvent();
    public UnityEvent onLocalActionRequested = new UnityEvent();

    public OperatorIntentRobotCommand LastCommand { get; private set; }

    private void Awake()
    {
        if (intentRouter == null)
            intentRouter = GetComponent<OperatorIntentRouter>();

        if (intentRouter == null)
            intentRouter = FindFirstObjectByType<OperatorIntentRouter>();

        if (robotSyncManager == null)
            robotSyncManager = FindFirstObjectByType<RobotSyncManager>();

        if (avatarController == null)
            avatarController = FindFirstObjectByType<RobotAvatarController>();
    }

    private void OnEnable()
    {
        if (intentRouter != null)
        {
            intentRouter.onIntent.RemoveListener(OnIntent);
            intentRouter.onIntent.AddListener(OnIntent);
        }
    }

    private void OnDisable()
    {
        if (intentRouter != null)
            intentRouter.onIntent.RemoveListener(OnIntent);
    }

    public void OnIntent(OperatorIntentKind intentKind)
    {
        LastCommand = OperatorIntentRobotCommandMapping.Map(intentKind, commandSettings);

        robotSyncManager?.ApplyOperatorMotion(LastCommand.linearVelocity, LastCommand.steer);
        onMotionCommand?.Invoke(LastCommand.linearVelocity, LastCommand.steer);

        if (LastCommand.triggerLocalAction)
        {
            robotSyncManager?.NotifyLocalActionTriggered();
            onLocalActionRequested?.Invoke();
        }

        if (LastCommand.HasImmediateCommand)
        {
            robotSyncManager?.SendOperatorCommand(LastCommand.immediateCommand);
            onImmediateCommand?.Invoke(LastCommand.immediateCommand);
        }

        avatarController?.SetState(LastCommand.avatarState);
    }
}
