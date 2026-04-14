using Agibuild.Fulora.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class WebViewCoreEventWiringRuntimeTests
{
    [Fact]
    public void Dispose_unhooks_adapter_events()
    {
        var adapter = MockWebViewAdapter.CreateWithDownload();
        var calls = 0;
        var runtime = new WebViewCoreEventWiringRuntime(
            adapter,
            NullLogger.Instance,
            (_, _) => calls++,
            (_, _) => calls++,
            (_, _) => calls++,
            (_, _) => calls++,
            (_, _) => calls++,
            (_, _) => calls++,
            (_, _) => calls++);

        runtime.Dispose();

        adapter.RaiseNavigationCompleted();
        ((MockWebViewAdapterWithDownload)adapter).RaiseDownloadRequested(new DownloadRequestedEventArgs(new Uri("https://example.test"), "a.txt", "text/plain"));

        Assert.Equal(0, calls);
    }
}
