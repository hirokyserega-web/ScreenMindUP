namespace ScreenMind.Core.Abstractions;

/// <summary>Modifier flags for a global hotkey combination.</summary>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8,
}

/// <summary>A platform-neutral hotkey definition.</summary>
public sealed record HotkeyGesture(HotkeyModifiers Modifiers, int VirtualKey);

/// <summary>Raised when a registered hotkey fires.</summary>
public sealed record HotkeyPressedEventArgs(string ActionId, HotkeyGesture Gesture);

/// <summary>
/// Global hotkey registration. Implementations must expose registration
/// conflicts as results, not crashes, and release native resources on disposal.
/// </summary>
public interface IHotkeyService : IDisposable
{
    /// <summary>Registers a gesture for a named action. Returns false when the gesture is taken.</summary>
    bool TryRegister(string actionId, HotkeyGesture gesture);

    /// <summary>Removes the registration for an action if present.</summary>
    void Unregister(string actionId);

    /// <summary>Temporarily suspends or resumes all registered hotkeys.</summary>
    void SetSuspended(bool suspended);

    event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;
}
