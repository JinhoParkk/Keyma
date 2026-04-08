using Keyma.Core.Input;
using Keyma.Core.Screen;

namespace Keyma.Core.Engine;

/// <summary>
/// Central orchestrator. Manages the LocalActive ↔ RemoteActive state machine.
///
/// Server role: captures local input, forwards to remote when in RemoteActive state.
/// Client role: receives remote input events and injects them locally.
///
/// Safety: will NOT suppress input unless a remote client is connected.
/// Emergency: Scroll Lock always forces return to LocalActive.
/// </summary>
public sealed class KeymaEngine : IDisposable
{
    /// <summary>
    /// Scroll Lock is the emergency escape key. When pressed, the engine
    /// unconditionally returns to LocalActive, restoring local input.
    /// </summary>
    private const KeyCode EmergencyKey = KeyCode.ScrollLock;

    private readonly IInputCapture _capture;
    private readonly IInputInjector _injector;
    private readonly IScreenEdgeDetector _edgeDetector;

    // Called by the engine to send an input event to the remote machine.
    private readonly Func<InputEvent, Task> _sendToRemote;

    private EngineState _state = EngineState.LocalActive;
    private volatile bool _disposed;
    private volatile bool _remoteConnected;

    public EngineState State => _state;

    /// <summary>
    /// Set to true when a remote client is connected and ready.
    /// The engine will refuse to transition to RemoteActive if this is false.
    /// </summary>
    public bool RemoteConnected
    {
        get => _remoteConnected;
        set => _remoteConnected = value;
    }

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
        // Always restore local input first
        TransitionTo(EngineState.LocalActive);
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
        // SAFETY: Never transition if no client is connected
        if (!_remoteConnected)
            return;

        if (_state == EngineState.LocalActive)
            TransitionTo(EngineState.RemoteActive);
    }

    private void OnInputReceived(InputEvent evt)
    {
        // Emergency escape: Scroll Lock always forces LocalActive
        if (evt.Key == EmergencyKey && evt.Type == InputEventType.KeyDown)
        {
            if (_state == EngineState.RemoteActive)
            {
                TransitionTo(EngineState.LocalActive);
                return; // Don't forward the escape key
            }
        }

        if (_state != EngineState.RemoteActive)
            return;

        // Safety check: if remote disconnected while we were in RemoteActive, bail out
        if (!_remoteConnected)
        {
            TransitionTo(EngineState.LocalActive);
            return;
        }

        // Fire-and-forget; network layer handles queuing
        _ = _sendToRemote(evt);
    }

    private void TransitionTo(EngineState newState)
    {
        if (_state == newState) return;
        _state = newState;
        _capture.SuppressInput = newState == EngineState.RemoteActive;
        StateChanged?.Invoke(newState);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // Restore input before unhooking
        _capture.SuppressInput = false;
        _capture.InputReceived -= OnInputReceived;
        _edgeDetector.EdgeHit -= OnEdgeHit;
        Stop();
    }
}
