using Keyma.Core.Engine;
using Keyma.Core.Input;
using Keyma.Core.Screen;
using Keyma.Network.Protocol;
using Keyma.Network.Transport;
using Keyma.Platform.Windows.Input;
using Keyma.Platform.Windows.Screen;

// ── Usage ──────────────────────────────────────────────────────────────────
// Server (machine that shares its input):
//   keyma server
//
// Client (machine that receives input):
//   keyma client <server-ip>
// ──────────────────────────────────────────────────────────────────────────

if (args.Length == 0)
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  keyma server           - Start as server (share your input)");
    Console.WriteLine("  keyma client <ip>      - Start as client (receive input)");
    return;
}

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

switch (args[0].ToLowerInvariant())
{
    case "server":
        await RunServerAsync(cts.Token);
        break;
    case "client" when args.Length >= 2:
        await RunClientAsync(args[1], cts.Token);
        break;
    default:
        Console.WriteLine("Unknown command.");
        break;
}

// ── Server ──────────────────────────────────────────────────────────────────

static async Task RunServerAsync(CancellationToken ct)
{
    Console.WriteLine($"[Server] Starting on port {KeymaServer.DefaultPort}...");

    var capture      = new WindowsInputCapture();
    var injector     = new WindowsInputInjector();
    var edgeDetector = new WindowsScreenEdgeDetector();

    await using var server = new KeymaServer();

    KeymaConnection? activeClient = null;

    async Task SendToRemote(InputEvent evt)
    {
        if (activeClient is null || !activeClient.IsConnected) return;
        var batch = new InputBatchMessage
        {
            Events = [Keyma.Network.Protocol.MessageMapper.ToMessage(evt)]
        };
        await activeClient.SendInputBatchAsync(batch);
    }

    var engine = new KeymaEngine(capture, injector, edgeDetector, SendToRemote);

    server.ClientConnected += conn =>
    {
        activeClient = conn;
        Console.WriteLine($"[Server] Client connected: {conn.RemoteMachineName}");

        conn.SwitchFromReceived += () =>
        {
            Console.WriteLine("[Server] Control returned from client.");
            engine.OnRemoteSwitchBack();
        };

        conn.Disconnected += () =>
        {
            Console.WriteLine("[Server] Client disconnected.");
            activeClient = null;
            engine.OnRemoteSwitchBack();
        };

        conn.StartReceiving();

        var info = edgeDetector.GetScreenInfo();
        _ = conn.SendHelloAsync(new HelloMessage
        {
            MachineName = Environment.MachineName,
            InstanceId  = Guid.NewGuid().ToString(),
            ScreenWidth = info.Width,
            ScreenHeight = info.Height,
        });
    };

    engine.StateChanged += state =>
    {
        Console.WriteLine($"[Server] State → {state}");
        if (state == EngineState.RemoteActive && activeClient is not null)
        {
            _ = activeClient.SendSwitchToAsync(new SwitchToMessage { EntryX = 0, EntryY = 0.5 });
        }
    };

    server.Start();
    engine.Start();

    Console.WriteLine("[Server] Running. Move mouse to right edge to switch to client. Ctrl+C to exit.");
    await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);

    engine.Dispose();
    await server.DisposeAsync();
}

// ── Client ──────────────────────────────────────────────────────────────────

static async Task RunClientAsync(string serverIp, CancellationToken ct)
{
    Console.WriteLine($"[Client] Connecting to {serverIp}:{KeymaServer.DefaultPort}...");

    var capture      = new WindowsInputCapture();
    var injector     = new WindowsInputInjector();
    var edgeDetector = new WindowsScreenEdgeDetector();

    await using var client = new KeymaClient();
    var conn = await client.ConnectAsync(serverIp, ct: ct);

    Console.WriteLine("[Client] Connected.");

    using var disconnectedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    var engine = new KeymaEngine(capture, injector, edgeDetector, _ => Task.CompletedTask);

    conn.SwitchToReceived += () =>
    {
        Console.WriteLine("[Client] Now active (receiving input).");
        // Position cursor at left edge center on arrival
        var info = edgeDetector.GetScreenInfo();
        injector.MoveCursor(0, info.Height / 2);
    };

    conn.InputReceived += evt =>
    {
        engine.OnRemoteInputReceived(evt);
    };

    // When client's cursor hits left edge, signal server to take back control.
    edgeDetector.EdgeHit += async (edge, pos) =>
    {
        if (edge == ScreenEdge.Left)
        {
            Console.WriteLine("[Client] Cursor returned to left edge — switching back to server.");
            await conn.SendSwitchFromAsync();
        }
    };

    conn.Disconnected += () =>
    {
        Console.WriteLine("[Client] Server disconnected.");
        disconnectedCts.Cancel();
    };

    edgeDetector.SetActiveEdges(new HashSet<ScreenEdge> { ScreenEdge.Left });
    edgeDetector.Start();
    capture.Start();

    await conn.SendHelloAsync(new HelloMessage
    {
        MachineName  = Environment.MachineName,
        InstanceId   = Guid.NewGuid().ToString(),
        ScreenWidth  = edgeDetector.GetScreenInfo().Width,
        ScreenHeight = edgeDetector.GetScreenInfo().Height,
    });

    conn.StartReceiving();

    Console.WriteLine("[Client] Waiting for input. Ctrl+C to exit.");

    try { await Task.Delay(Timeout.Infinite, disconnectedCts.Token); }
    catch (OperationCanceledException) { }

    engine.Dispose();
    edgeDetector.Dispose();
    capture.Dispose();
}
