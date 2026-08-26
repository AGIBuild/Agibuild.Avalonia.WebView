using Agibuild.Fulora.Adapters.Abstractions;
using Agibuild.Fulora.Platforms;
using Microsoft.Web.WebView2.Core;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class WindowsWebView2RuntimeProbeTests
{
    [Fact]
    public void Probe_false_platform_returns_unsupported_without_invoking_loader()
    {
        var loaderCallCount = 0;

        var result = WindowsWebView2RuntimeProbe.Probe(
            isWindows: false,
            () =>
            {
                loaderCallCount++;
                return "version";
            });

        Assert.False(result.IsAvailable);
        Assert.False(result.IsPlatformSupported);
        Assert.False(result.IsRuntimeAvailable);
        Assert.Equal(0, loaderCallCount);
        Assert.False(string.IsNullOrWhiteSpace(result.UnavailableReason));
    }

    [Fact]
    public void Probe_non_empty_version_returns_available()
    {
        var result = WindowsWebView2RuntimeProbe.Probe(isWindows: true, () => "123.0.0.0");

        Assert.True(result.IsAvailable);
        Assert.True(result.IsPlatformSupported);
        Assert.True(result.IsRuntimeAvailable);
        Assert.Null(result.UnavailableReason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Probe_missing_version_returns_runtime_unavailable(string? version)
    {
        var result = WindowsWebView2RuntimeProbe.Probe(isWindows: true, () => version);

        Assert.False(result.IsAvailable);
        Assert.True(result.IsPlatformSupported);
        Assert.False(result.IsRuntimeAvailable);
        Assert.False(string.IsNullOrWhiteSpace(result.UnavailableReason));
    }

    [Fact]
    public void Probe_runtime_not_found_exception_returns_runtime_unavailable()
    {
        var result = WindowsWebView2RuntimeProbe.Probe(
            isWindows: true,
            () => throw new WebView2RuntimeNotFoundException());

        AssertRuntimeUnavailable(result);
    }

    [Fact]
    public void Probe_dll_not_found_returns_runtime_unavailable()
    {
        var result = WindowsWebView2RuntimeProbe.Probe(
            isWindows: true,
            () => throw new DllNotFoundException());

        AssertRuntimeUnavailable(result);
    }

    [Fact]
    public void Probe_entry_point_not_found_returns_runtime_unavailable()
    {
        var result = WindowsWebView2RuntimeProbe.Probe(
            isWindows: true,
            () => throw new EntryPointNotFoundException());

        AssertRuntimeUnavailable(result);
    }

    [Fact]
    public void Probe_bad_image_format_returns_runtime_unavailable()
    {
        var result = WindowsWebView2RuntimeProbe.Probe(
            isWindows: true,
            () => throw new BadImageFormatException());

        AssertRuntimeUnavailable(result);
    }

    [Fact]
    public void Probe_unexpected_exception_propagates()
    {
        Assert.Throws<InvalidOperationException>(() =>
            WindowsWebView2RuntimeProbe.Probe(
                isWindows: true,
                () => throw new InvalidOperationException()));
    }

    [Fact]
    public void Probe_failure_reason_is_non_empty_and_does_not_include_stack_trace()
    {
        var result = WindowsWebView2RuntimeProbe.Probe(
            isWindows: true,
            () => throw new DllNotFoundException());

        Assert.False(string.IsNullOrWhiteSpace(result.UnavailableReason));
        Assert.False(result.UnavailableReason.Contains(Environment.NewLine, StringComparison.Ordinal));
        Assert.False(result.UnavailableReason.Contains(nameof(DllNotFoundException), StringComparison.Ordinal));
    }

    [Fact]
    public void Probe_returns_a_valid_result_for_the_current_host()
    {
        var result = WindowsWebView2RuntimeProbe.Probe();

        if (!OperatingSystem.IsWindows())
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
