using System.Runtime.InteropServices;
using System.Threading.Channels;
using Keyma.Core.Input;

namespace Keyma.Platform.Windows.Input;

/// <summary>
/// Global keyboard and mouse capture via Win32 low-level hooks (WH_KEYBOARD_LL,
/// WH_MOUSE_LL). Runs the required message pump on a dedicated STA thread.
/// </summary>
public sealed class WindowsInputCapture : IInputCapture
{
    private nint _keyboardHook;
    private nint _mouseHook;
    private Thread? _hookThread;
    private volatile bool _running;
    private volatile bool _suppress;

    // The delegates must be kept alive as fields to prevent GC collection.
    private NativeMethods.LowLevelHookProc? _keyboardProc;
    private NativeMethods.LowLevelHookProc? _mouseProc;

    // Channel for decoupling hook callback (must be fast) from event processing.
    private readonly Channel<InputEvent> _eventChannel =
        Channel.CreateUnbounded<InputEvent>(new UnboundedChannelOptions { SingleReader = true });

    public event Action<InputEvent>? InputReceived;
    public bool IsRunning => _running;

    public bool SuppressInput
    {
        get => _suppress;
        set => _suppress = value;
    }

    public void Start()
    {
        if (_running) return;
        _running = true;

        _hookThread = new Thread(HookThreadProc)
        {
            IsBackground = true,
            Name = "KeymaHookThread",
        };
        _hookThread.SetApartmentState(ApartmentState.STA);
        _hookThread.Start();

        // Dispatch events from the channel on a background task.
        _ = DispatchEventsAsync();
    }

    public void Stop()
    {
        _running = false;
        _eventChannel.Writer.TryComplete();
        // The message pump will exit when _running becomes false on next tick,
        // or when UnhookWindowsHookEx is called (which triggers WM_NULL).
    }

    public (int X, int Y) GetCursorPosition()
    {
        NativeMethods.GetCursorPos(out var pt);
        return (pt.X, pt.Y);
    }

    // ── Hook thread ─────────────────────────────────────────────────────────

    private void HookThreadProc()
    {
        var hMod = NativeMethods.GetModuleHandleW(null);

        _keyboardProc = KeyboardProc;
        _mouseProc    = MouseProc;

        _keyboardHook = NativeMethods.SetWindowsHookExW(NativeMethods.WH_KEYBOARD_LL, _keyboardProc, hMod, 0);
        _mouseHook    = NativeMethods.SetWindowsHookExW(NativeMethods.WH_MOUSE_LL,    _mouseProc,    hMod, 0);

        // Message pump (required for LL hooks).
        while (_running && NativeMethods.GetMessageW(out var msg, 0, 0, 0))
        {
            NativeMethods.TranslateMessage(in msg);
            NativeMethods.DispatchMessageW(in msg);
        }

        if (_keyboardHook != 0) NativeMethods.UnhookWindowsHookEx(_keyboardHook);
        if (_mouseHook    != 0) NativeMethods.UnhookWindowsHookEx(_mouseHook);
        _keyboardHook = 0;
        _mouseHook    = 0;
    }

    // ── Hook callbacks (must return quickly) ────────────────────────────────

    private nint KeyboardProc(int nCode, nint wParam, nint lParam)
    {
        if (nCode == NativeMethods.HC_ACTION)
        {
            var info = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);

            // Skip events injected by us to prevent feedback loops.
            if ((info.flags & NativeMethods.LLKHF_INJECTED) == 0)
            {
                var type = wParam switch
                {
                    NativeMethods.WM_KEYDOWN    => InputEventType.KeyDown,
                    NativeMethods.WM_SYSKEYDOWN => InputEventType.KeyDown,
                    NativeMethods.WM_KEYUP      => InputEventType.KeyUp,
                    NativeMethods.WM_SYSKEYUP   => InputEventType.KeyUp,
                    _ => (InputEventType?)null
                };

                if (type.HasValue)
                {
                    var evt = new InputEvent
                    {
                        Type = type.Value,
                        Key = VkToKeyCode(info.vkCode),
                        TimestampMs = info.time,
                    };
                    _eventChannel.Writer.TryWrite(evt);

                    if (_suppress) return 1; // swallow
                }
            }
        }
        return NativeMethods.CallNextHookEx(0, nCode, wParam, lParam);
    }

    private nint MouseProc(int nCode, nint wParam, nint lParam)
    {
        if (nCode == NativeMethods.HC_ACTION)
        {
            var info = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);

            if ((info.flags & NativeMethods.LLMHF_INJECTED) == 0)
            {
                var screenW = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
                var screenH = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
                double nx = screenW > 0 ? (double)info.pt.X / screenW : 0;
                double ny = screenH > 0 ? (double)info.pt.Y / screenH : 0;

                InputEvent? evt = (int)wParam switch
                {
                    NativeMethods.WM_MOUSEMOVE => new InputEvent
                    {
                        Type = InputEventType.MouseMove,
                        MouseX = nx,
                        MouseY = ny,
                        TimestampMs = info.time,
                    },
                    NativeMethods.WM_LBUTTONDOWN => new InputEvent { Type = InputEventType.MouseButtonDown, Button = MouseButton.Left, MouseX = nx, MouseY = ny, TimestampMs = info.time },
                    NativeMethods.WM_LBUTTONUP   => new InputEvent { Type = InputEventType.MouseButtonUp,   Button = MouseButton.Left, MouseX = nx, MouseY = ny, TimestampMs = info.time },
                    NativeMethods.WM_RBUTTONDOWN => new InputEvent { Type = InputEventType.MouseButtonDown, Button = MouseButton.Right, MouseX = nx, MouseY = ny, TimestampMs = info.time },
                    NativeMethods.WM_RBUTTONUP   => new InputEvent { Type = InputEventType.MouseButtonUp,   Button = MouseButton.Right, MouseX = nx, MouseY = ny, TimestampMs = info.time },
                    NativeMethods.WM_MBUTTONDOWN => new InputEvent { Type = InputEventType.MouseButtonDown, Button = MouseButton.Middle, MouseX = nx, MouseY = ny, TimestampMs = info.time },
                    NativeMethods.WM_MBUTTONUP   => new InputEvent { Type = InputEventType.MouseButtonUp,   Button = MouseButton.Middle, MouseX = nx, MouseY = ny, TimestampMs = info.time },
                    NativeMethods.WM_MOUSEWHEEL  => new InputEvent { Type = InputEventType.MouseScroll, ScrollDeltaY = (short)(info.mouseData >> 16) / 120.0, TimestampMs = info.time },
                    NativeMethods.WM_MOUSEHWHEEL => new InputEvent { Type = InputEventType.MouseScroll, ScrollDeltaX = (short)(info.mouseData >> 16) / 120.0, TimestampMs = info.time },
                    _ => null
                };

                if (evt is not null)
                {
                    _eventChannel.Writer.TryWrite(evt);
                    if (_suppress) return 1; // swallow
                }
            }
        }
        return NativeMethods.CallNextHookEx(0, nCode, wParam, lParam);
    }

    // ── Event dispatch ──────────────────────────────────────────────────────

    private async Task DispatchEventsAsync()
    {
        await foreach (var evt in _eventChannel.Reader.ReadAllAsync())
        {
            try { InputReceived?.Invoke(evt); }
            catch { /* never let handler exceptions crash the hook thread */ }
        }
    }

    // ── Key code mapping ────────────────────────────────────────────────────

    private static KeyCode VkToKeyCode(uint vk) => vk switch
    {
        0x08 => KeyCode.Backspace,
        0x09 => KeyCode.Tab,
        0x0D => KeyCode.Return,
        0x1B => KeyCode.Escape,
        0x20 => KeyCode.Space,
        0x25 => KeyCode.ArrowLeft,
        0x26 => KeyCode.ArrowUp,
        0x27 => KeyCode.ArrowRight,
        0x28 => KeyCode.ArrowDown,
        0x2D => KeyCode.Insert,
        0x2E => KeyCode.Delete,
        0x24 => KeyCode.Home,
        0x23 => KeyCode.End,
        0x21 => KeyCode.PageUp,
        0x22 => KeyCode.PageDown,
        >= 0x30 and <= 0x39 => (KeyCode)((int)KeyCode.Num0 + (int)(vk - 0x30)),
        >= 0x41 and <= 0x5A => (KeyCode)((int)KeyCode.A   + (int)(vk - 0x41)),
        >= 0x70 and <= 0x7B => (KeyCode)((int)KeyCode.F1  + (int)(vk - 0x70)),
        0xA0 => KeyCode.LeftShift,
        0xA1 => KeyCode.RightShift,
        0xA2 => KeyCode.LeftControl,
        0xA3 => KeyCode.RightControl,
        0xA4 => KeyCode.LeftAlt,
        0xA5 => KeyCode.RightAlt,
        0x5B => KeyCode.LeftSuper,
        0x5C => KeyCode.RightSuper,
        0x14 => KeyCode.CapsLock,
        0x2C => KeyCode.PrintScreen,
        0x91 => KeyCode.ScrollLock,
        0x13 => KeyCode.Pause,
        0xBD => KeyCode.Minus,
        0xBB => KeyCode.Equals,
        0xDB => KeyCode.LeftBracket,
        0xDD => KeyCode.RightBracket,
        0xDC => KeyCode.Backslash,
        0xBA => KeyCode.Semicolon,
        0xDE => KeyCode.Apostrophe,
        0xC0 => KeyCode.Grave,
        0xBC => KeyCode.Comma,
        0xBE => KeyCode.Period,
        0xBF => KeyCode.Slash,
        _ => KeyCode.Unknown,
    };

    public void Dispose()
    {
        Stop();
    }
}
