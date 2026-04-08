using System.Runtime.InteropServices;

namespace Keyma.Platform.Windows.Input;

internal static partial class NativeMethods
{
    // ── Hook constants ──────────────────────────────────────────────────────
    internal const int WH_KEYBOARD_LL = 13;
    internal const int WH_MOUSE_LL    = 14;

    internal const int HC_ACTION = 0;

    // WM values
    internal const int WM_KEYDOWN    = 0x0100;
    internal const int WM_KEYUP      = 0x0101;
    internal const int WM_SYSKEYDOWN = 0x0104;
    internal const int WM_SYSKEYUP   = 0x0105;

    internal const int WM_MOUSEMOVE        = 0x0200;
    internal const int WM_LBUTTONDOWN      = 0x0201;
    internal const int WM_LBUTTONUP        = 0x0202;
    internal const int WM_RBUTTONDOWN      = 0x0204;
    internal const int WM_RBUTTONUP        = 0x0205;
    internal const int WM_MBUTTONDOWN      = 0x0207;
    internal const int WM_MBUTTONUP        = 0x0208;
    internal const int WM_MOUSEWHEEL       = 0x020A;
    internal const int WM_MOUSEHWHEEL      = 0x020E;
    internal const int WM_XBUTTONDOWN      = 0x020B;
    internal const int WM_XBUTTONUP        = 0x020C;

    // Hook struct flags
    internal const uint LLKHF_INJECTED = 0x10;
    internal const uint LLMHF_INJECTED = 0x01;

    // SendInput types
    internal const uint INPUT_MOUSE    = 0;
    internal const uint INPUT_KEYBOARD = 1;

    // Mouse flags
    internal const uint MOUSEEVENTF_MOVE        = 0x0001;
    internal const uint MOUSEEVENTF_LEFTDOWN    = 0x0002;
    internal const uint MOUSEEVENTF_LEFTUP      = 0x0004;
    internal const uint MOUSEEVENTF_RIGHTDOWN   = 0x0008;
    internal const uint MOUSEEVENTF_RIGHTUP     = 0x0010;
    internal const uint MOUSEEVENTF_MIDDLEDOWN  = 0x0020;
    internal const uint MOUSEEVENTF_MIDDLEUP    = 0x0040;
    internal const uint MOUSEEVENTF_WHEEL       = 0x0800;
    internal const uint MOUSEEVENTF_HWHEEL      = 0x1000;
    internal const uint MOUSEEVENTF_ABSOLUTE    = 0x8000;

    // Keyboard flags
    internal const uint KEYEVENTF_KEYUP      = 0x0002;
    internal const uint KEYEVENTF_SCANCODE   = 0x0008;
    internal const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

    [LibraryImport("user32.dll")]
    internal static partial short GetAsyncKeyState(int vKey);

    internal const int VK_LSHIFT   = 0xA0;
    internal const int VK_RSHIFT   = 0xA1;
    internal const int VK_LCONTROL = 0xA2;
    internal const int VK_RCONTROL = 0xA3;
    internal const int VK_LMENU    = 0xA4; // Left Alt
    internal const int VK_RMENU    = 0xA5; // Right Alt

    // ── Delegates ──────────────────────────────────────────────────────────
    internal delegate nint LowLevelHookProc(int nCode, nint wParam, nint lParam);

    // ── P/Invoke ────────────────────────────────────────────────────────────
    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial nint SetWindowsHookExW(int idHook, LowLevelHookProc lpfn, nint hMod, uint dwThreadId);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool UnhookWindowsHookEx(nint hhk);

    [LibraryImport("user32.dll")]
    internal static partial nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint GetModuleHandleW(string? lpModuleName);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetMessageW(out MSG lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool TranslateMessage(in MSG lpMsg);

    [LibraryImport("user32.dll")]
    internal static partial nint DispatchMessageW(in MSG lpMsg);

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetCursorPos(int X, int Y);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetCursorPos(out POINT lpPoint);

    [LibraryImport("user32.dll")]
    internal static partial int GetSystemMetrics(int nIndex);

    internal const int SM_CXSCREEN = 0;
    internal const int SM_CYSCREEN = 1;

    // ── Structs ─────────────────────────────────────────────────────────────
    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MSG
    {
        public nint hwnd;
        public uint message;
        public nint wParam;
        public nint lParam;
        public uint time;
        public POINT pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public nuint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public nuint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct INPUT
    {
        public uint type;
        public INPUTUNION u;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct INPUTUNION
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public nuint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public nuint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }
}
