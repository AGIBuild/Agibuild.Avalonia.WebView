using Agibuild.Fulora.Adapters.Abstractions;
using Agibuild.Fulora.Platforms;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class GtkWebViewRuntimeProbeTests
{
    [Fact]
    public void Probe_false_platform_returns_unsupported_without_resolving_or_loading()
    {
        var resolveCount = 0;
        var loadCount = 0;

        var result = GtkWebViewRuntimeProbe.Probe(
            isLinux: false,
            () =>
            {
                resolveCount++;
                return "/tmp/libAgibuildWebViewGtk.so";
            },
            _ =>
            {
                loadCount++;
                return true;
            });

        Assert.False(result.IsAvailable);
        Assert.False(result.IsPlatformSupported);
        Assert.Equal(0, resolveCount);
        Assert.Equal(0, loadCount);
        Assert.False(string.IsNullOrWhiteSpace(result.UnavailableReason));
    }

    [Fact]
    public void Probe_missing_shim_returns_runtime_unavailable_without_loading()
    {
        var loadCount = 0;

        var result = GtkWebViewRuntimeProbe.Probe(
            isLinux: true,
            () => null,
            _ =>
            {
                loadCount++;
                return true;
            });

        AssertRuntimeUnavailable(result);
        Assert.Equal(0, loadCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Probe_blank_shim_path_returns_runtime_unavailable_without_loading(string path)
    {
        var loadCount = 0;

        var result = GtkWebViewRuntimeProbe.Probe(
            isLinux: true,
            () => path,
            _ =>
            {
                loadCount++;
                return true;
            });

        AssertRuntimeUnavailable(result);
        Assert.Equal(0, loadCount);
    }

    [Fact]
    public void Probe_loadable_shim_returns_available()
    {
        var loadedPath = string.Empty;

        var result = GtkWebViewRuntimeProbe.Probe(
            isLinux: true,
            () => "/app/runtimes/linux-x64/native/libAgibuildWebViewGtk.so",
            path =>
            {
                loadedPath = path;
                return true;
            });

        Assert.True(result.IsAvailable);
        Assert.True(result.IsPlatformSupported);
        Assert.True(result.IsRuntimeAvailable);
        Assert.Null(result.UnavailableReason);
        Assert.Equal("/app/runtimes/linux-x64/native/libAgibuildWebViewGtk.so", loadedPath);
    }

    [Fact]
    public void Probe_unloaded_shim_returns_runtime_unavailable()
    {
        var result = GtkWebViewRuntimeProbe.Probe(
            isLinux: true,
            () => "/app/libAgibuildWebViewGtk.so",
            _ => false);

        AssertRuntimeUnavailable(result);
    }

    [Fact]
    public void Probe_dll_not_found_returns_runtime_unavailable()
    {
        var result = GtkWebViewRuntimeProbe.Probe(
            isLinux: true,
            () => "/app/libAgibuildWebViewGtk.so",
            _ => throw new DllNotFoundException());

        AssertRuntimeUnavailable(result);
    }

    [Fact]
    public void Probe_entry_point_not_found_returns_runtime_unavailable()
    {
        var result = GtkWebViewRuntimeProbe.Probe(
            isLinux: true,
            () => "/app/libAgibuildWebViewGtk.so",
            _ => throw new EntryPointNotFoundException());

        AssertRuntimeUnavailable(result);
    }

    [Fact]
    public void Probe_bad_image_format_returns_runtime_unavailable()
    {
        var result = GtkWebViewRuntimeProbe.Probe(
            isLinux: true,
            () => "/app/libAgibuildWebViewGtk.so",
            _ => throw new BadImageFormatException());

        AssertRuntimeUnavailable(result);
    }

    [Fact]
    public void Probe_unexpected_exception_propagates()
    {
        Assert.Throws<InvalidOperationException>(() =>
            GtkWebViewRuntimeProbe.Probe(
                isLinux: true,
                () => "/app/libAgibuildWebViewGtk.so",
                _ => throw new InvalidOperationException()));
    }

    [Fact]
    public void Probe_failure_reason_is_non_empty_and_does_not_include_stack_trace_or_path()
    {
        var result = GtkWebViewRuntimeProbe.Probe(
            isLinux: true,
            () => "/secret/machine/libAgibuildWebViewGtk.so",
            _ => throw new DllNotFoundException());

        Assert.False(string.IsNullOrWhiteSpace(result.UnavailableReason));
        Assert.False(result.UnavailableReason.Contains(Environment.NewLine, StringComparison.Ordinal));
        Assert.False(result.UnavailableReason.Contains("/secret/", StringComparison.Ordinal));
        Assert.False(result.UnavailableReason.Contains(nameof(DllNotFoundException), StringComparison.Ordinal));
    }

    [Fact]
    public void GetCandidatePaths_prefers_packaged_runtime_then_app_directory()
    {
        var paths = AgibuildWebViewGtkNativeLibrary.GetCandidatePaths("/app");

        Assert.Equal(
            [
                Path.Combine("/app", "runtimes", "linux-x64", "native", AgibuildWebViewGtkNativeLibrary.FileName),
                Path.Combine("/app", AgibuildWebViewGtkNativeLibrary.FileName),
            ],
            paths);
    }

    [Fact]
    public void ResolveExistingPath_prefers_packaged_runtime_over_flat_copy()
    {
        var root = Path.Combine(Path.GetTempPath(), "fulora-gtk-probe-" + Guid.NewGuid().ToString("N"));
        var packagedDir = Path.Combine(root, "runtimes", "linux-x64", "native");
        Directory.CreateDirectory(packagedDir);

        try
        {
            var packaged = Path.Combine(packagedDir, AgibuildWebViewGtkNativeLibrary.FileName);
            var flat = Path.Combine(root, AgibuildWebViewGtkNativeLibrary.FileName);
            File.WriteAllBytes(packaged, [0]);
            File.WriteAllBytes(flat, [0]);

            Assert.Equal(packaged, AgibuildWebViewGtkNativeLibrary.ResolveExistingPath(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Probe_returns_a_valid_result_for_the_current_host()
    {
        var result = GtkWebViewRuntimeProbe.Probe();

        if (!OperatingSystem.IsLinux())
        {
            Assert.False(result.IsAvailable);
            Assert.False(result.IsPlatformSupported);
            return;
        }

        Assert.True(result.IsAvailable || (result.IsPlatformSupported && !result.IsRuntimeAvailable));
        if (!result.IsAvailable)
        {
            Assert.False(string.IsNullOrWhiteSpace(result.UnavailableReason));
        }
    }

    private static void AssertRuntimeUnavailable(WebViewPlatformProbeResult result)
    {
        Assert.False(result.IsAvailable);
        Assert.True(result.IsPlatformSupported);
        Assert.False(result.IsRuntimeAvailable);
        Assert.False(string.IsNullOrWhiteSpace(result.UnavailableReason));
    }
}
