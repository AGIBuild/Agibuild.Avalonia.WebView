namespace Agibuild.Fulora;

/// <summary>
/// Virtual key codes for global shortcut registration.
/// Subset of commonly used keys across all platforms.
/// </summary>
public enum ShortcutKey
{
    None = 0,

    // Letters
    A = 65, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

    // Digits
    D0 = 48, D1, D2, D3, D4, D5, D6, D7, D8, D9,

    // Function keys
    F1 = 112, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,

    // Navigation
    Escape = 27,
    Space = 32,
    Enter = 13,
    Tab = 9,
    Backspace = 8,
    Delete = 46,
    Insert = 45,
    Home = 36,
    End = 35,
    PageUp = 33,
    PageDown = 34,
    Left = 37,
    Up = 38,
    Right = 39,
    Down = 40,

    // Punctuation
    Minus = 189,
    Plus = 187,
    Comma = 188,
    Period = 190,
    Slash = 191,
    Backslash = 220,
    Semicolon = 186,
    Quote = 222,
    BracketLeft = 219,
    BracketRight = 221,
    Backtick = 192,

    // Numpad
    NumPad0 = 96, NumPad1, NumPad2, NumPad3, NumPad4,
    NumPad5, NumPad6, NumPad7, NumPad8, NumPad9,

    // Media (reserved for future)
    PrintScreen = 44,
    Pause = 19,
    ScrollLock = 145
}

/// <summary>
/// Modifier key flags for global shortcut registration.
/// </summary>
[Flags]
public enum ShortcutModifiers
{
    /// <summary>No modifier keys.</summary>
    None = 0,
    /// <summary>Ctrl key (Control on macOS).</summary>
    Ctrl = 1,
    /// <summary>Alt key (Option on macOS).</summary>
    Alt = 2,
    /// <summary>Shift key.</summary>
    Shift = 4,
    /// <summary>Meta key (Cmd on macOS, Win on Windows, Super on Linux).</summary>
    Meta = 8
}
