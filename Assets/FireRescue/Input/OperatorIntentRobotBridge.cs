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
    public SimulatedRobotStateProvider simulatedStateProvider;
    public bool enableSimulationWhenDisconnected = true;

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
        if (simulatedStateProvider == null)
            simulatedStateProvider = FindFirstObjectByType<SimulatedRobotStateProvider>();
        if (simulatedStateProvider == null && enableSimulationWhenDisconnected)
            simulatedStateProvider = new GameObject("Simulated Robot State").AddComponent<SimulatedRobotStateProvider>();
    }

    private void OnEnable()
    {
        if (intentRouter != null)
        {
            intentRouter.onIntent.RemoveListener(OnIntent);
            intentRouter.onIntent.AddListener(OnIntent);
        }
        if (robotSyncManager != null)
            robotSyncManager.ConfirmedStateChanged += OnConfirmedState;
        if (simulatedStateProvider != null)
            simulatedStateProvider.ConfirmedStateChanged += OnConfirmedState;
    }

    private void OnDisable()
    {
        if (intentRouter != null)
            intentRouter.onIntent.RemoveListener(OnIntent);
        if (robotSyncManager != null)
            robotSyncManager.ConfirmedStateChanged -= OnConfirmedState;
        if (simulatedStateProvider != null)
            simulatedStateProvider.ConfirmedStateChanged -= OnConfirmedState;
    }

    public void OnIntent(OperatorIntentKind intentKind)
    {
        LastCommand = OperatorIntentRobotCommandMapping.Map(intentKind, commandSettings);

        bool useSimulation = enableSimulationWhenDisconnected && (robotSyncManager == null || !robotSyncManager.IsConnected);
        if (useSimulation)
            simulatedStateProvider?.ApplyMotion(LastCommand.linearVelocity, LastCommand.steer);
        else
            robotSyncManager?.ApplyOperatorMotion(LastCommand.linearVelocity, LastCommand.steer);
        onMotionCommand?.Invoke(LastCommand.linearVelocity, LastCommand.steer);

        if (LastCommand.triggerLocalAction)
        {
            if (!useSimulation) robotSyncManager?.NotifyLocalActionTriggered();
            if (useSimulation) simulatedStateProvider?.SendAction(LastCommand.immediateCommand);
            onLocalActionRequested?.Invoke();
        }

        if (LastCommand.HasImmediateCommand)
        {
            if (!useSimulation) robotSyncManager?.SendOperatorCommand(LastCommand.immediateCommand);
            onImmediateCommand?.Invoke(LastCommand.immediateCommand);
        }

    }

    private void OnConfirmedState(RobotAvatarState state)
    {
        avatarController?.SetState(state);
    }
}
