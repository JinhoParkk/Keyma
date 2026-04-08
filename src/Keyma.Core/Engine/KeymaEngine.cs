using Keyma.Core.Input;
using Keyma.Core.Screen;

namespace Keyma.Core.Engine;

/// <summary>
/// Central orchestrator. Manages the LocalActive ↔ RemoteActive state machine.
///
/// Server role: captures local input, forwards to remote when in RemoteActive state.
/// Client role: receives remote input events and injects them locally.
/// </summary>
public sealed class KeymaEngine : IDisposable
{
    private readonly IInputCapture _capture;
    private readonly IInputInjector _injector;
    private readonly IScreenEdgeDetector _edgeDetector;

    // Called by the engine to send an input event to the remote machine.
    private readonly Func<InputEvent, Task> _sendToRemote;

    private EngineState _state = EngineState.LocalActive;
    private volatile bool _disposed;

    public EngineState State => _state;

    public event Action<EngineState>? StateChanged;

    public KeymaEngine(
        IInputCapture capture,
        IInputInjector injector,
        IScreenEdgeDetector edgeDetector,
        Func<InputEvent, Task> sendToRemote)
    {
        _capture = capture;
        _injector = injector;
        _edgeDetector = edgeDetector;
        _sendToRemote = sendToRemote;

        _capture.InputReceived += OnInputReceived;
        _edgeDetector.EdgeHit += OnEdgeHit;
    }

    public void Start()
    {
        _edgeDetector.SetActiveEdges(new HashSet<ScreenEdge> { ScreenEdge.Right });
        _edgeDetector.Start();
        _capture.Start();
    }

    public void Stop()
    {
        _capture.Stop();
        _edgeDetector.Stop();
    }

    /// <summary>
    /// Called by the network layer when the remote machine signals that
    /// control is returning to this machine (cursor crossed back).
    /// </summary>
    public void OnRemoteSwitchBack()
    {
        if (_state == EngineState.RemoteActive)
            TransitionTo(EngineState.LocalActive);
    }

    /// <summary>
    /// Called by the network layer when an input event arrives from the
    /// remote machine (this machine is the client/receiver).
    /// </summary>
    public void OnRemoteInputReceived(InputEvent evt)
    {
        if (evt.Type == InputEventType.MouseMove)
        {
            var info = _edgeDetector.GetScreenInfo();
            int x = (int)(evt.MouseX * info.Width);
            int y = (int)(evt.MouseY * info.Height);
            _injector.MoveCursor(x, y);
        }
        else
        {
            _injector.Inject(evt);
        }
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void OnEdgeHit(ScreenEdge edge, int position)
    {
        if (_state == EngineState.LocalActive)
            TransitionTo(EngineState.RemoteActive);
    }

    private void OnInputReceived(InputEvent evt)
    {
        if (_state != EngineState.RemoteActive)
            return;

        // Fire-and-forget; network layer handles queuing
        _ = _sendToRemote(evt);
    }

    private void TransitionTo(EngineState newState)
    {
        _state = newState;
        _capture.SuppressInput = newState == EngineState.RemoteActive;
        StateChanged?.Invoke(newState);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _capture.InputReceived -= OnInputReceived;
        _edgeDetector.EdgeHit -= OnEdgeHit;
        Stop();
    }
}
