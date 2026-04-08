namespace Keyma.Core.Input;

[Flags]
public enum ModifierKeys : byte
{
    None    = 0,
    Shift   = 1 << 0,
    Control = 1 << 1,
    Alt     = 1 << 2,
    Super   = 1 << 3,
}
