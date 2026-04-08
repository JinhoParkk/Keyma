namespace Keyma.Core.Input;

/// <summary>
/// Platform-neutral input event. Mouse positions are normalized to [0.0, 1.0]
/// relative to the screen, so they remain valid across different resolutions.
/// </summary>
public sealed record InputEvent
{
    public required InputEventType Type { get; init; }

    // Keyboard
    public KeyCode Key { get; init; }
    public ModifierKeys Modifiers { get; init; }

    // Mouse — absolute position normalized to [0.0, 1.0] relative to screen
    public double MouseX { get; init; }
    public double MouseY { get; init; }

    // Mouse — relative delta for smooth movement
    public double DeltaX { get; init; }
    public double DeltaY { get; init; }

    public MouseButton Button { get; init; }
    public double ScrollDeltaX { get; init; }
    public double ScrollDeltaY { get; init; }

    public long TimestampMs { get; init; }
}
