namespace ScreenMind.Core.Abstractions;

/// <summary>Result of attempting to exclude a window from screen capture.</summary>
public enum CaptureExclusionStatus
{
    Applied,
    NotSupported,
    Failed,
}

/// <summary>
/// Best-effort exclusion of ScreenMind's own windows from standard capture APIs
/// (SetWindowDisplayAffinity on Windows). Absolute invisibility is never promised.
/// </summary>
public interface IWindowCaptureExclusionService
{
    /// <summary>True when the current OS version supports capture exclusion.</summary>
    bool IsSupported { get; }

    /// <summary>Applies exclusion to a native window handle and reports the real outcome.</summary>
    CaptureExclusionStatus TryExclude(nint windowHandle);
}
