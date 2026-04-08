namespace Keyma.Network.Protocol;

public enum MessageType : byte
{
    // Handshake
    Hello       = 0x01,
    Ping        = 0x05,
    Pong        = 0x06,
    Disconnect  = 0x07,

    // Input
    InputBatch  = 0x10,
    SwitchTo    = 0x11,  // "You are now active" → sent to client
    SwitchFrom  = 0x12,  // "Return control to server" → sent back
}
