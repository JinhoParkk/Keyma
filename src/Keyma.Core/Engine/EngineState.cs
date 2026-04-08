namespace Keyma.Core.Engine;

public enum EngineState
{
    /// <summary>This machine has local control. Input goes to local apps.</summary>
    LocalActive,

    /// <summary>Input is being forwarded to a remote machine.</summary>
    RemoteActive,
}
