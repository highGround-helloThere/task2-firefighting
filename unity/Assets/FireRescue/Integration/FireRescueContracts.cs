using System;
using UnityEngine.Events;

public enum FireRescueControlMode { Manual, Auto, EmergencyStop, Disconnected }
public enum FireRescueNavigationState { IDLE, EXPLORING, TURNING, AVOIDING_OBSTACLE, FIRE_DETECTED, APPROACHING_FIRE, EXTINGUISHING, MISSION_COMPLETE, EMERGENCY_STOP }
[Serializable] public sealed class RobotConfirmedStateEvent : UnityEvent<RobotAvatarState> { }
public interface IRobotCommandSink { void ApplyMotion(float linearVelocity, float steer); void SendAction(string command); bool IsConnected { get; } }
public interface IRobotStateProvider { event Action<RobotAvatarState> ConfirmedStateChanged; RobotAvatarState ConfirmedState { get; } bool IsConnected { get; } }
public interface IAutonomousNavigationProvider { FireRescueControlMode ControlMode { get; } FireRescueNavigationState NavigationState { get; } string PlannedDirection { get; } bool CanTakeOver { get; } }
