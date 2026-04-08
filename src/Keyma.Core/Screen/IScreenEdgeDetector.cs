namespace Keyma.Core.Screen;

/// <summary>
/// Polls the cursor position and fires <see cref="EdgeHit"/> when the cursor
/// reaches a configured screen edge.
/// </summary>
public interface IScreenEdgeDetector : IDisposable
{
    /// <summary>
    /// Fires when the cursor hits an active edge.
    /// Parameters: edge, position along that edge (pixels).
    /// </summary>
    event Action<ScreenEdge, int>? EdgeHit;

    /// <summary>Configure which edges are monitored.</summary>
    void SetActiveEdges(IReadOnlySet<ScreenEdge> edges);

    ScreenInfo GetScreenInfo();

    void Start();
    void Stop();
}
