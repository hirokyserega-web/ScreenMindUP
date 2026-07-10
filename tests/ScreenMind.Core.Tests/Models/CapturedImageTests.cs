using ScreenMind.Core.Models;

namespace ScreenMind.Core.Tests.Models;

public sealed class CapturedImageTests
{
    [Fact]
    public void Dispose_clears_pixel_data()
    {
        var image = new CapturedImage([1, 2, 3, 4], ImageFormat.Png, 2, 2, CaptureSource.ActiveWindow);

        image.Dispose();

        image.IsDisposed.Should().BeTrue();
        var act = () => _ = image.Data;
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Double_dispose_is_safe()
    {
        var image = new CapturedImage([1, 2], ImageFormat.Jpeg, 1, 1, CaptureSource.Monitor);

        image.Dispose();
        var act = () => image.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Rejects_empty_data()
    {
        var act = () => new CapturedImage(
            Array.Empty<byte>(), ImageFormat.Png, 1, 1, CaptureSource.Region);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-5, 10)]
    public void Rejects_non_positive_dimensions(int width, int height)
    {
        var act = () => new CapturedImage([1], ImageFormat.Png, width, height, CaptureSource.Region);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Exposes_size_in_bytes()
    {
        var image = new CapturedImage([1, 2, 3], ImageFormat.Webp, 1, 1, CaptureSource.Region);

        image.SizeInBytes.Should().Be(3);
    }
}
