using MessagePack;

namespace Keyma.Network.Protocol;

[MessagePackObject]
public sealed class HelloMessage
{
    [Key(0)] public required string MachineName { get; init; }
    [Key(1)] public required string InstanceId { get; init; }
    [Key(2)] public int ScreenWidth { get; init; }
    [Key(3)] public int ScreenHeight { get; init; }
}

[MessagePackObject]
public sealed class InputEventMessage
{
    [Key(0)] public byte EventType { get; init; }
    [Key(1)] public ushort KeyCode { get; init; }
    [Key(2)] public byte Modifiers { get; init; }
    [Key(3)] public double MouseX { get; init; }
    [Key(4)] public double MouseY { get; init; }
    [Key(5)] public double DeltaX { get; init; }
    [Key(6)] public double DeltaY { get; init; }
    [Key(7)] public byte Button { get; init; }
    [Key(8)] public double ScrollDeltaX { get; init; }
    [Key(9)] public double ScrollDeltaY { get; init; }
    [Key(10)] public long TimestampMs { get; init; }
}

[MessagePackObject]
public sealed class InputBatchMessage
{
    [Key(0)] public required InputEventMessage[] Events { get; init; }
}

[MessagePackObject]
public sealed class SwitchToMessage
{
    /// <summary>Cursor entry position on the remote screen (normalized).</summary>
    [Key(0)] public double EntryX { get; init; }
    [Key(1)] public double EntryY { get; init; }
}

[MessagePackObject]
public sealed class SwitchFromMessage
{
    // No payload needed for Phase 1
}

[MessagePackObject]
public sealed class PingMessage
{
    [Key(0)] public long SentAtMs { get; init; }
}

[MessagePackObject]
public sealed class PongMessage
{
    [Key(0)] public long OriginalSentAtMs { get; init; }
}
