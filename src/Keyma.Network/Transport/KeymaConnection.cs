using System.Buffers;
using System.Buffers.Binary;
using System.Net.Sockets;
using Keyma.Core.Input;
using Keyma.Network.Protocol;
using MessagePack;

namespace Keyma.Network.Transport;

/// <summary>
/// Wraps a <see cref="TcpClient"/> and provides typed send/receive over the
/// Keyma wire protocol: [4B LE length][1B MessageType][N bytes MessagePack body].
/// </summary>
public sealed class KeymaConnection : IAsyncDisposable
{
    private readonly TcpClient _tcp;
    private readonly NetworkStream _stream;
    private readonly CancellationTokenSource _cts = new();

    public string RemoteMachineName { get; private set; } = string.Empty;
    public string RemoteInstanceId { get; private set; } = string.Empty;
    public bool IsConnected => _tcp.Connected;

    public event Action<InputEvent>? InputReceived;
    public event Action? SwitchToReceived;
    public event Action? SwitchFromReceived;
    public event Action? Disconnected;

    public KeymaConnection(TcpClient tcp)
    {
        _tcp = tcp;
        _tcp.NoDelay = true;
        _stream = tcp.GetStream();
    }

    public async Task SendHelloAsync(HelloMessage msg)
        => await SendAsync(MessageType.Hello, msg);

    public async Task SendInputBatchAsync(InputBatchMessage msg)
        => await SendAsync(MessageType.InputBatch, msg);

    public async Task SendSwitchToAsync(SwitchToMessage msg)
        => await SendAsync(MessageType.SwitchTo, msg);

    public async Task SendSwitchFromAsync()
        => await SendAsync(MessageType.SwitchFrom, new SwitchFromMessage());

    public async Task SendPingAsync()
        => await SendAsync(MessageType.Ping, new PingMessage { SentAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });

    /// <summary>Start reading messages in a background loop until disconnected.</summary>
    public void StartReceiving()
    {
        _ = ReceiveLoopAsync(_cts.Token);
    }

    // ── Send ────────────────────────────────────────────────────────────────

    private async Task SendAsync<T>(MessageType type, T payload)
    {
        byte[] body = MessagePackSerializer.Serialize(payload);
        // Frame: [4B length (body+1)][1B type][body]
        int frameSize = 4 + 1 + body.Length;
        byte[] frame = ArrayPool<byte>.Shared.Rent(frameSize);
        try
        {
            BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(0, 4), (uint)(1 + body.Length));
            frame[4] = (byte)type;
            body.CopyTo(frame, 5);
            await _stream.WriteAsync(frame.AsMemory(0, frameSize));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(frame);
        }
    }

    // ── Receive loop ────────────────────────────────────────────────────────

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        byte[] header = new byte[5]; // 4B length + 1B type
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await ReadExactAsync(header, 0, 5, ct);
                uint payloadLength = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0, 4));
                var type = (MessageType)header[4];
                int bodyLength = (int)payloadLength - 1;

                byte[] body = bodyLength > 0 ? ArrayPool<byte>.Shared.Rent(bodyLength) : [];
                try
                {
                    if (bodyLength > 0)
                        await ReadExactAsync(body, 0, bodyLength, ct);

                    HandleMessage(type, body.AsSpan(0, bodyLength));
                }
                finally
                {
                    if (bodyLength > 0) ArrayPool<byte>.Shared.Return(body);
                }
            }
        }
        catch (Exception) when (ct.IsCancellationRequested || !_tcp.Connected)
        {
            // Normal disconnection
        }
        catch (Exception)
        {
            // Connection dropped unexpectedly
        }
        finally
        {
            Disconnected?.Invoke();
        }
    }

    private void HandleMessage(MessageType type, ReadOnlySpan<byte> body)
    {
        switch (type)
        {
            case MessageType.Hello:
            {
                var msg = MessagePackSerializer.Deserialize<HelloMessage>(body.ToArray());
                RemoteMachineName = msg.MachineName;
                RemoteInstanceId = msg.InstanceId;
                break;
            }
            case MessageType.InputBatch:
            {
                var msg = MessagePackSerializer.Deserialize<InputBatchMessage>(body.ToArray());
                foreach (var e in msg.Events)
                    InputReceived?.Invoke(MessageMapper.ToEvent(e));
                break;
            }
            case MessageType.SwitchTo:
                SwitchToReceived?.Invoke();
                break;

            case MessageType.SwitchFrom:
                SwitchFromReceived?.Invoke();
                break;

            case MessageType.Ping:
                _ = SendAsync(MessageType.Pong, new PongMessage
                {
                    OriginalSentAtMs = MessagePackSerializer.Deserialize<PingMessage>(body.ToArray()).SentAtMs
                });
                break;
        }
    }

    private async Task ReadExactAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        int read = 0;
        while (read < count)
        {
            int n = await _stream.ReadAsync(buffer.AsMemory(offset + read, count - read), ct);
            if (n == 0) throw new EndOfStreamException("Connection closed.");
            read += n;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _stream.Dispose();
        _tcp.Dispose();
        _cts.Dispose();
    }
}
