using System.Reflection;

namespace ScreenMind.Core.Tests.Architecture;

/// <summary>
/// Guards the phase-01 architectural invariant: ScreenMind.Core must stay free of
/// UI, platform, HTTP-SDK and provider dependencies.
/// </summary>
public sealed class CoreDependencyTests
{
    private static readonly string[] _forbiddenReferencePrefixes =
    [
        "Avalonia",
        "ScreenMind.UI",
        "ScreenMind.Platform",
        "ScreenMind.Infrastructure",
        "ScreenMind.Providers",
        "ScreenMind.AI",
        "ScreenMind.App",
        "Microsoft.Extensions.Http",
        "System.Net.Http.Json",
    ];

    [Fact]
    public void Core_assembly_does_not_reference_forbidden_dependencies()
    {
        var coreAssembly = typeof(CoreAssemblyMarker).Assembly;

        var references = coreAssembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        foreach (var _forbidden in _forbiddenReferencePrefixes)
        {
            references.Should().NotContain(
                name => name.StartsWith(_forbidden, StringComparison.Ordinal),
                $"ScreenMind.Core must not depend on assemblies starting with '{_forbidden}'");
        }
    }

    [Fact]
    public void Core_assembly_targets_net8()
    {
        var coreAssembly = typeof(CoreAssemblyMarker).Assembly;

        var targetFramework = coreAssembly
            .GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>();

        targetFramework.Should().NotBeNull();
        targetFramework!.FrameworkName.Should().StartWith(".NETCoreApp,Version=v8.0");
    }
}
