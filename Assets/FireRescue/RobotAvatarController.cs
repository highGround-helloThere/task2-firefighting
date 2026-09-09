using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public sealed class RobotAvatarStateEvent : UnityEvent<RobotAvatarState>
{
}

[AddComponentMenu("Fire Rescue/Robot/Robot Avatar Controller")]
public sealed class RobotAvatarController : MonoBehaviour
{
    public Animator animator;
    public string animatorStateParameter = "RobotState";
    public RobotAvatarState initialState = RobotAvatarState.Stand;
    public RobotAvatarStateEvent onStateChanged = new RobotAvatarStateEvent();

    public RobotAvatarState CurrentState { get; private set; }

    private void Awake()
    {
        CurrentState = initialState;
        ApplyAnimatorState();
    }

    public void SetState(RobotAvatarState nextState)
    {
        if (CurrentState == nextState)
            return;

        CurrentState = nextState;
        ApplyAnimatorState();
        onStateChanged?.Invoke(nextState);
    }

    private void ApplyAnimatorState()
    {
        if (animator == null || animator.runtimeAnimatorController == null ||
            string.IsNullOrEmpty(animatorStateParameter))
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash != Animator.StringToHash(animatorStateParameter) ||
                parameter.type != AnimatorControllerParameterType.Int)
                continue;

            animator.SetInteger(parameter.nameHash, (int)CurrentState);
            return;
        }
    }
}
