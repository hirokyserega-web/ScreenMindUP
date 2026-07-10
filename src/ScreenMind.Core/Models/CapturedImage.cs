namespace ScreenMind.Core.Models;

/// <summary>
/// An in-memory screenshot. Never persisted to disk; the buffer is owned by the
/// session and must be disposed when the session ends or the capture is discarded.
/// </summary>
public sealed class CapturedImage : IDisposable
{
    private byte[]? _data;

    public CapturedImage(byte[] data, ImageFormat format, int pixelWidth, int pixelHeight, CaptureSource source)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0)
        {
            throw new ArgumentException("Image data must not be empty.", nameof(data));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelHeight);

        _data = data;
        Format = format;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
        Source = source;
        CapturedAtUtc = DateTimeOffset.UtcNow;
    }

    public ImageFormat Format { get; }

    public int PixelWidth { get; }

    public int PixelHeight { get; }

    public CaptureSource Source { get; }

    public DateTimeOffset CapturedAtUtc { get; }

    public bool IsDisposed => _data is null;

    /// <summary>Size of the encoded payload in bytes.</summary>
    public int SizeInBytes => _data?.Length ?? 0;

    /// <summary>Returns the encoded image bytes. Throws if the image was disposed.</summary>
    public ReadOnlyMemory<byte> Data =>
        _data ?? throw new ObjectDisposedException(nameof(CapturedImage));

    public void Dispose()
    {
        var data = _data;
        if (data is null)
        {
            return;
        }

        // Best-effort wipe: screen content must not linger in memory longer than needed.
        Array.Clear(data);
        _data = null;
    }
}
