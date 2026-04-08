namespace Keyma.Core.Input;

/// <summary>
/// Captures global keyboard and mouse input on the local machine.
/// When <see cref="SuppressInput"/> is true, physical input is swallowed
/// and not delivered to local applications.
/// </summary>
public interface IInputCapture : IDisposable
{
    /// <summary>Fired for every captured input event.</summary>
    event Action<InputEvent>? InputReceived;

    /// <summary>Start the global hook / event tap.</summary>
    void Start();

    /// <summary>Stop capturing.</summary>
    void Stop();

    bool IsRunning { get; }

    /// <summary>
    /// When true, captured input is suppressed locally.
    /// Used when the cursor is on a remote machine.
    /// </summary>
    bool SuppressInput { get; set; }

    /// <summary>Get current absolute cursor position (pixels).</summary>
    (int X, int Y) GetCursorPosition();
}
