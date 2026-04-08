namespace Keyma.Core.Input;

/// <summary>
/// Platform-neutral key codes. Mapping from/to platform-specific codes
/// (Windows VK, macOS CGKeyCode) is done in each platform project.
/// </summary>
public enum KeyCode : ushort
{
    Unknown = 0,

    // Letters
    A = 4, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

    // Numbers (top row) — Num0='0' key, Num1='1' key, ..., Num9='9' key
    Num0 = 30, Num1, Num2, Num3, Num4, Num5, Num6, Num7, Num8, Num9,

    // Function keys
    F1 = 58, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    F13, F14, F15, F16, F17, F18, F19, F20, F21, F22, F23, F24,

    // Control keys
    Return = 40,
    Escape = 41,
    Backspace = 42,
    Tab = 43,
    Space = 44,
    Minus = 45,
    Equals = 46,
    LeftBracket = 47,
    RightBracket = 48,
    Backslash = 49,
    Semicolon = 51,
    Apostrophe = 52,
    Grave = 53,
    Comma = 54,
    Period = 55,
    Slash = 56,
    CapsLock = 57,

    // Navigation
    PrintScreen = 70,
    ScrollLock = 71,
    Pause = 72,
    Insert = 73,
    Home = 74,
    PageUp = 75,
    Delete = 76,
    End = 77,
    PageDown = 78,
    ArrowRight = 79,
    ArrowLeft = 80,
    ArrowDown = 81,
    ArrowUp = 82,

    // Modifiers
    LeftControl = 224,
    LeftShift = 225,
    LeftAlt = 226,
    LeftSuper = 227,
    RightControl = 228,
    RightShift = 229,
    RightAlt = 230,
    RightSuper = 231,
}
