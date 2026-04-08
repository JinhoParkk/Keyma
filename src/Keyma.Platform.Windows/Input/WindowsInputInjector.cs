using Keyma.Core.Input;

namespace Keyma.Platform.Windows.Input;

/// <summary>
/// Injects keyboard and mouse events via Win32 <c>SendInput</c>.
/// </summary>
public sealed class WindowsInputInjector : IInputInjector
{
    public void Inject(InputEvent evt)
    {
        switch (evt.Type)
        {
            case InputEventType.KeyDown:
            case InputEventType.KeyUp:
                InjectKey(evt);
                break;

            case InputEventType.MouseMove:
                InjectMouseMove(evt);
                break;

            case InputEventType.MouseButtonDown:
            case InputEventType.MouseButtonUp:
                InjectMouseButton(evt);
                break;

            case InputEventType.MouseScroll:
                InjectScroll(evt);
                break;
        }
    }

    public void MoveCursor(int x, int y)
    {
        NativeMethods.SetCursorPos(x, y);
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private static void InjectKey(InputEvent evt)
    {
        uint vk = KeyCodeToVk(evt.Key);
        if (vk == 0) return;

        var input = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            u = new NativeMethods.INPUTUNION
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = (ushort)vk,
                    dwFlags = evt.Type == InputEventType.KeyUp ? NativeMethods.KEYEVENTF_KEYUP : 0,
                }
            }
        };
        NativeMethods.SendInput(1, [input], System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static void InjectMouseMove(InputEvent evt)
    {
        int screenW = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
        int screenH = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
        int x = (int)(evt.MouseX * screenW);
        int y = (int)(evt.MouseY * screenH);

        // Normalize to 0-65535 for MOUSEEVENTF_ABSOLUTE
        int nx = screenW > 0 ? (int)((double)x / screenW * 65535) : 0;
        int ny = screenH > 0 ? (int)((double)y / screenH * 65535) : 0;

        var input = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_MOUSE,
            u = new NativeMethods.INPUTUNION
            {
                mi = new NativeMethods.MOUSEINPUT
                {
                    dx = nx,
                    dy = ny,
                    dwFlags = NativeMethods.MOUSEEVENTF_MOVE | NativeMethods.MOUSEEVENTF_ABSOLUTE,
                }
            }
        };
        NativeMethods.SendInput(1, [input], System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static void InjectMouseButton(InputEvent evt)
    {
        uint flags = (evt.Button, evt.Type) switch
        {
            (MouseButton.Left,   InputEventType.MouseButtonDown) => NativeMethods.MOUSEEVENTF_LEFTDOWN,
            (MouseButton.Left,   InputEventType.MouseButtonUp)   => NativeMethods.MOUSEEVENTF_LEFTUP,
            (MouseButton.Right,  InputEventType.MouseButtonDown) => NativeMethods.MOUSEEVENTF_RIGHTDOWN,
            (MouseButton.Right,  InputEventType.MouseButtonUp)   => NativeMethods.MOUSEEVENTF_RIGHTUP,
            (MouseButton.Middle, InputEventType.MouseButtonDown) => NativeMethods.MOUSEEVENTF_MIDDLEDOWN,
            (MouseButton.Middle, InputEventType.MouseButtonUp)   => NativeMethods.MOUSEEVENTF_MIDDLEUP,
            _ => 0
        };
        if (flags == 0) return;

        var input = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_MOUSE,
            u = new NativeMethods.INPUTUNION
            {
                mi = new NativeMethods.MOUSEINPUT { dwFlags = flags }
            }
        };
        NativeMethods.SendInput(1, [input], System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static void InjectScroll(InputEvent evt)
    {
        if (evt.ScrollDeltaY != 0)
        {
            var input = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_MOUSE,
                u = new NativeMethods.INPUTUNION
                {
                    mi = new NativeMethods.MOUSEINPUT
                    {
                        mouseData = (uint)(int)(evt.ScrollDeltaY * 120),
                        dwFlags = NativeMethods.MOUSEEVENTF_WHEEL,
                    }
                }
            };
            NativeMethods.SendInput(1, [input], System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>());
        }

        if (evt.ScrollDeltaX != 0)
        {
            var input = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_MOUSE,
                u = new NativeMethods.INPUTUNION
                {
                    mi = new NativeMethods.MOUSEINPUT
                    {
                        mouseData = (uint)(int)(evt.ScrollDeltaX * 120),
                        dwFlags = NativeMethods.MOUSEEVENTF_HWHEEL,
                    }
                }
            };
            NativeMethods.SendInput(1, [input], System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>());
        }
    }

    private static uint KeyCodeToVk(KeyCode key) => key switch
    {
        KeyCode.Backspace    => 0x08,
        KeyCode.Tab          => 0x09,
        KeyCode.Return       => 0x0D,
        KeyCode.Escape       => 0x1B,
        KeyCode.Space        => 0x20,
        KeyCode.ArrowLeft    => 0x25,
        KeyCode.ArrowUp      => 0x26,
        KeyCode.ArrowRight   => 0x27,
        KeyCode.ArrowDown    => 0x28,
        KeyCode.Insert       => 0x2D,
        KeyCode.Delete       => 0x2E,
        KeyCode.Home         => 0x24,
        KeyCode.End          => 0x23,
        KeyCode.PageUp       => 0x21,
        KeyCode.PageDown     => 0x22,
        >= KeyCode.Num0 and <= KeyCode.Num9 => (uint)(0x30 + ((int)key - (int)KeyCode.Num0)),
        >= KeyCode.A    and <= KeyCode.Z    => (uint)(0x41 + ((int)key - (int)KeyCode.A)),
        >= KeyCode.F1   and <= KeyCode.F12  => (uint)(0x70 + ((int)key - (int)KeyCode.F1)),
        KeyCode.LeftShift    => 0xA0,
        KeyCode.RightShift   => 0xA1,
        KeyCode.LeftControl  => 0xA2,
        KeyCode.RightControl => 0xA3,
        KeyCode.LeftAlt      => 0xA4,
        KeyCode.RightAlt     => 0xA5,
        KeyCode.LeftSuper    => 0x5B,
        KeyCode.RightSuper   => 0x5C,
        KeyCode.CapsLock     => 0x14,
        KeyCode.Minus        => 0xBD,
        KeyCode.Equals       => 0xBB,
        KeyCode.LeftBracket  => 0xDB,
        KeyCode.RightBracket => 0xDD,
        KeyCode.Backslash    => 0xDC,
        KeyCode.Semicolon    => 0xBA,
        KeyCode.Apostrophe   => 0xDE,
        KeyCode.Grave        => 0xC0,
        KeyCode.Comma        => 0xBC,
        KeyCode.Period       => 0xBE,
        KeyCode.Slash        => 0xBF,
        _ => 0,
    };

    public void Dispose() { }
}
