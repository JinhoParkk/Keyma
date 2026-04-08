using System.Net.Sockets;

namespace Keyma.Network.Transport;

/// <summary>
/// Connects to a remote Keyma server.
/// </summary>
public sealed class KeymaClient : IAsyncDisposable
{
    private TcpClient? _tcp;
    private KeymaConnection? _connection;

    public KeymaConnection? Connection => _connection;

    public async Task<KeymaConnection> ConnectAsync(string host, int port = KeymaServer.DefaultPort, CancellationToken ct = default)
    {
        _tcp = new TcpClient();
        await _tcp.ConnectAsync(host, port, ct);
        _connection = new KeymaConnection(_tcp);
        return _connection;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
        _tcp?.Dispose();
    }
}
