namespace Keyma.Core.Input;

/// <summary>
/// Injects synthetic input events on the local machine, as if a physical
/// device produced them. Used on the receiving (client) side.
/// </summary>
public interface IInputInjector : IDisposable
{
    /// <summary>Inject a single input event.</summary>
    void Inject(InputEvent evt);

    /// <summary>Move the mouse cursor to absolute pixel coordinates.</summary>
    void MoveCursor(int x, int y);
}
