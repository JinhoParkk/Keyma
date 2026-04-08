using Keyma.Core.Screen;
using Keyma.Platform.Windows.Input;

namespace Keyma.Platform.Windows.Screen;

/// <summary>
/// Polls the cursor position every ~8ms and fires <see cref="EdgeHit"/>
/// when it reaches a configured screen edge.
///
/// Edge detection uses a 1-pixel threshold: the cursor is considered to have
/// hit an edge when its coordinate equals the screen boundary.
/// </summary>
public sealed class WindowsScreenEdgeDetector : IScreenEdgeDetector
{
    private IReadOnlySet<ScreenEdge> _activeEdges = new HashSet<ScreenEdge>();
    private CancellationTokenSource? _cts;
    private Task? _pollTask;

    public event Action<ScreenEdge, int>? EdgeHit;

    public void SetActiveEdges(IReadOnlySet<ScreenEdge> edges) => _activeEdges = edges;

    public ScreenInfo GetScreenInfo()
    {
        int w = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
        int h = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
        return new ScreenInfo(w, h);
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _pollTask = PollAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
    }

    private async Task PollAsync(CancellationToken ct)
    {
        var info = GetScreenInfo();
        int maxX = info.Width - 1;
        int maxY = info.Height - 1;

        // Track last state to fire EdgeHit only on transition (not every tick).
        bool wasAtEdge = false;

        while (!ct.IsCancellationRequested)
        {
            NativeMethods.GetCursorPos(out var pt);
            bool atEdge = false;

            if (_activeEdges.Contains(ScreenEdge.Right) && pt.X >= maxX)
            {
                atEdge = true;
                if (!wasAtEdge) EdgeHit?.Invoke(ScreenEdge.Right, pt.Y);
            }
            else if (_activeEdges.Contains(ScreenEdge.Left) && pt.X <= 0)
            {
                atEdge = true;
                if (!wasAtEdge) EdgeHit?.Invoke(ScreenEdge.Left, pt.Y);
            }
            else if (_activeEdges.Contains(ScreenEdge.Bottom) && pt.Y >= maxY)
            {
                atEdge = true;
                if (!wasAtEdge) EdgeHit?.Invoke(ScreenEdge.Bottom, pt.X);
            }
            else if (_activeEdges.Contains(ScreenEdge.Top) && pt.Y <= 0)
            {
                atEdge = true;
                if (!wasAtEdge) EdgeHit?.Invoke(ScreenEdge.Top, pt.X);
            }

            wasAtEdge = atEdge;

            await Task.Delay(8, ct).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
