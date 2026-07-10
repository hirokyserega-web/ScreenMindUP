using ScreenMind.Core.Models;

namespace ScreenMind.Core.Abstractions;

/// <summary>Rectangle in virtual-desktop physical pixels (may have negative origin on multi-monitor setups).</summary>
public readonly record struct PixelRect(int X, int Y, int Width, int Height);

/// <summary>
/// Platform capture abstraction. Capture happens only as a result of an explicit
/// user action and produces in-memory images only.
/// </summary>
public interface IScreenCaptureService
{
    /// <summary>Captures the current foreground window, excluding ScreenMind's own windows.</summary>
    Task<CapturedImage> CaptureActiveWindowAsync(CancellationToken cancellationToken);

    /// <summary>Captures the monitor containing the mouse cursor.</summary>
    Task<CapturedImage> CaptureMonitorWithCursorAsync(CancellationToken cancellationToken);

    /// <summary>Captures an arbitrary region of the virtual desktop.</summary>
    Task<CapturedImage> CaptureRegionAsync(PixelRect region, CancellationToken cancellationToken);
}
