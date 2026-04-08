using Keyma.Core.Input;
using Keyma.Network.Protocol;

namespace Keyma.Network.Protocol;

/// <summary>Maps between domain InputEvent and wire InputEventMessage.</summary>
public static class MessageMapper
{
    public static InputEventMessage ToMessage(InputEvent evt) => new()
    {
        EventType = (byte)evt.Type,
        KeyCode = (ushort)evt.Key,
        Modifiers = (byte)evt.Modifiers,
        MouseX = evt.MouseX,
        MouseY = evt.MouseY,
        DeltaX = evt.DeltaX,
        DeltaY = evt.DeltaY,
        Button = (byte)evt.Button,
        ScrollDeltaX = evt.ScrollDeltaX,
        ScrollDeltaY = evt.ScrollDeltaY,
        TimestampMs = evt.TimestampMs,
    };

    public static InputEvent ToEvent(InputEventMessage msg) => new()
    {
        Type = (InputEventType)msg.EventType,
        Key = (KeyCode)msg.KeyCode,
        Modifiers = (ModifierKeys)msg.Modifiers,
        MouseX = msg.MouseX,
        MouseY = msg.MouseY,
        DeltaX = msg.DeltaX,
        DeltaY = msg.DeltaY,
        Button = (MouseButton)msg.Button,
        ScrollDeltaX = msg.ScrollDeltaX,
        ScrollDeltaY = msg.ScrollDeltaY,
        TimestampMs = msg.TimestampMs,
    };
}
