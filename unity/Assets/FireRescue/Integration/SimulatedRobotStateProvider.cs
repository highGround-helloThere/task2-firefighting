using System;
using UnityEngine;

[AddComponentMenu("Fire Rescue/Integration/Simulated Robot State")]
public sealed class SimulatedRobotStateProvider : MonoBehaviour, IRobotCommandSink, IRobotStateProvider
{
    public bool enabledForAcceptance = true;
    public float simulatedCommandDuration = 0.18f;
    public RobotAvatarState ConfirmedState { get; private set; } = RobotAvatarState.Stand;
    public bool IsConnected => enabledForAcceptance;
    public event Action<RobotAvatarState> ConfirmedStateChanged;
    private float stateUntil;

    public void ApplyMotion(float linearVelocity, float steer)
    {
        if (!enabledForAcceptance) return;
        SetState(Mathf.Abs(linearVelocity) > 0.01f ? (linearVelocity > 0 ? RobotAvatarState.WalkForward : RobotAvatarState.WalkBackward) : (steer < -0.01f ? RobotAvatarState.TurnLeft : steer > 0.01f ? RobotAvatarState.TurnRight : RobotAvatarState.Stand));
    }
    public void SendAction(string command)
    {
        if (!enabledForAcceptance) return;
        SetState(command == "emergency_stop" ? RobotAvatarState.Stand : RobotAvatarState.Extinguish);
    }
    private void Update()
    {
        if (enabledForAcceptance && stateUntil > 0f && Time.unscaledTime >= stateUntil) SetState(RobotAvatarState.Stand);
    }
    private void SetState(RobotAvatarState state)
    {
        if (ConfirmedState == state) return;
        ConfirmedState = state;
        stateUntil = state == RobotAvatarState.Stand ? 0f : Time.unscaledTime + simulatedCommandDuration;
        ConfirmedStateChanged?.Invoke(state);
    }
}
