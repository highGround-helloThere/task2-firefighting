using System;

public enum OperatorIntentKind
{
    Stop,
    Stand,
    ForwardSmall,
    BackwardSmall,
    TurnLeftSmall,
    TurnRightSmall,
    Extinguish,
    RecoverFront,
    RecoverBack,
    EmergencyStop
}

public readonly struct OperatorIntent
{
    public OperatorIntent(
        OperatorIntentKind kind,
        double timestampSeconds,
        bool isOneShot)
    {
        Kind = kind;
        TimestampSeconds = timestampSeconds;
        IsOneShot = isOneShot;
    }

    public OperatorIntentKind Kind { get; }

    public double TimestampSeconds { get; }

    public bool IsOneShot { get; }
}
