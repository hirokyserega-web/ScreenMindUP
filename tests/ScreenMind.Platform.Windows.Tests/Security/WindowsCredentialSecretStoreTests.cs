using System.Runtime.Versioning;
using ScreenMind.Platform.Windows.Security;

namespace ScreenMind.Platform.Windows.Tests.Security;

public sealed class WindowsCredentialSecretStoreTests
{
    [Theory]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("has/slash")]
    [InlineData("..")]
    public async Task InvalidSecretName_IsRejected(string name)
    {
        var store = new WindowsCredentialSecretStore();

        var act = () => store.HasSecretAsync(name, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    public async Task SetGetHasRemove_RoundTripsUsingWindowsCredentialManager()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var name = $"integration-{Guid.NewGuid():N}";
        var value = $"secret-{Guid.NewGuid():N}";
        var store = new WindowsCredentialSecretStore();

        try
        {
            await store.SetSecretAsync(name, value, CancellationToken.None);
            (await store.HasSecretAsync(name, CancellationToken.None)).Should().BeTrue();
            (await store.GetSecretAsync(name, CancellationToken.None)).Should().Be(value);

            await store.RemoveSecretAsync(name, CancellationToken.None);

            (await store.HasSecretAsync(name, CancellationToken.None)).Should().BeFalse();
            (await store.GetSecretAsync(name, CancellationToken.None)).Should().BeNull();
        }
        finally
        {
            await store.RemoveSecretAsync(name, CancellationToken.None);
        }
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    public async Task CancelledOperation_DoesNotWriteSecret()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var name = $"cancelled-{Guid.NewGuid():N}";
        var store = new WindowsCredentialSecretStore();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var act = () => store.SetSecretAsync(name, "never-written", cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        (await store.HasSecretAsync(name, CancellationToken.None)).Should().BeFalse();
    }
}
