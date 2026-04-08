using System.Net;
using System.Net.Sockets;

namespace Keyma.Network.Transport;

/// <summary>
/// Listens for incoming Keyma client connections on a TCP port.
/// </summary>
public sealed class KeymaServer : IAsyncDisposable
{
    public const int DefaultPort = 19875;

    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();

    public event Action<KeymaConnection>? ClientConnected;

    public KeymaServer(int port = DefaultPort)
    {
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public void Start()
    {
        _listener.Start();
        _ = AcceptLoopAsync(_cts.Token);
    }

    public async Task StopAsync()
    {
        await _cts.CancelAsync();
        _listener.Stop();
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var tcp = await _listener.AcceptTcpClientAsync(ct);
                var conn = new KeymaConnection(tcp);
                ClientConnected?.Invoke(conn);
            }
        }
        catch (OperationCanceledException) { }
        catch (SocketException) when (ct.IsCancellationRequested) { }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cts.Dispose();
    }
}
