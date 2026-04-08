using Keyma.Core.Screen;
using Keyma.Platform.Windows.Input;

namespace Keyma.Platform.Windows.Screen;

/// <summary>
/// Polls the cursor position every ~8ms and fires <see cref="EdgeHit"/>
/// when the cursor stays at a screen edge for at least <see cref="DebounceTicks"/>
/// consecutive polls (~200ms). This prevents accidental transitions.
/// </summary>
public sealed class WindowsScreenEdgeDetector : IScreenEdgeDetector
{
    /// <summary>Number of consecutive edge polls before firing (25 × 8ms ≈ 200ms).</summary>
    private const int DebounceTicks = 25;

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

        ScreenEdge? currentEdge = null;
        int edgeTicks = 0;
        bool fired = false; // Prevent repeated firing while cursor stays at edge

        while (!ct.IsCancellationRequested)
        {
            NativeMethods.GetCursorPos(out var pt);
            ScreenEdge? detectedEdge = null;
            int position = 0;

            if (_activeEdges.Contains(ScreenEdge.Right) && pt.X >= maxX)
            {
                detectedEdge = ScreenEdge.Right; position = pt.Y;
            }
            else if (_activeEdges.Contains(ScreenEdge.Left) && pt.X <= 0)
            {
                detectedEdge = ScreenEdge.Left; position = pt.Y;
            }
            else if (_activeEdges.Contains(ScreenEdge.Bottom) && pt.Y >= maxY)
            {
                detectedEdge = ScreenEdge.Bottom; position = pt.X;
            }
            else if (_activeEdges.Contains(ScreenEdge.Top) && pt.Y <= 0)
            {
                detectedEdge = ScreenEdge.Top; position = pt.X;
            }

            if (detectedEdge == currentEdge && detectedEdge.HasValue)
            {
                edgeTicks++;
                if (edgeTicks >= DebounceTicks && !fired)
                {
                    fired = true;
                    EdgeHit?.Invoke(detectedEdge.Value, position);
                }
            }
            else
            {
                // Edge changed or cursor left the edge — reset
                currentEdge = detectedEdge;
                edgeTicks = detectedEdge.HasValue ? 1 : 0;
                fired = false;
            }

            await Task.Delay(8, ct).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
