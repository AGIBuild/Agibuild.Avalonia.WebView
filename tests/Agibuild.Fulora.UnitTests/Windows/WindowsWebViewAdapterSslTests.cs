using Agibuild.Fulora;
using Agibuild.Fulora.Adapters.Abstractions;
using Agibuild.Fulora.Adapters.Windows;
using Agibuild.Fulora.Security;
using Agibuild.Fulora.UnitTests.TestDoubles;
using Microsoft.Web.WebView2.Core;
using Xunit;

namespace Agibuild.Fulora.UnitTests.Windows;

public sealed class WindowsWebViewAdapterSslTests
{
    [Fact]
    public async Task Deferred_ssl_completion_uses_certificate_context_and_invokes_hook_once()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var hook = new RecordingNavigationSecurityHooks();
        var adapter = new WindowsWebViewAdapter(hook);
        var navId = Guid.NewGuid();
        var uri = new Uri("https://ssl-deferred.example/page");

        var tcs = new TaskCompletionSource<NavigationCompletedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        adapter.NavigationCompleted += (_, e) => tcs.TrySetResult(e);

        adapter.TestOnly_EnqueueDeferredSslNavigation(navId, uri, CoreWebView2WebErrorStatus.CertificateCommonNameIsIncorrect);

        var ctx = new ServerCertificateErrorContext(
            uri,
            uri.Host,
            "TlsCertUnknownCa",
            99,
            "CN=leaf",
            "CN=ca",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddYears(1));

        adapter.TestOnly_ApplyServerCertificateContext(ctx);

        var args = await tcs.Task;
        Assert.Equal(NavigationCompletedStatus.Failure, args.Status);
        var ex = Assert.IsType<WebViewSslException>(args.Error);
        Assert.Equal("CN=leaf", ex.CertificateSubject);
        Assert.Equal("CN=ca", ex.CertificateIssuer);
        Assert.Equal(navId, args.NavigationId);
        var received = Assert.Single(hook.Received);
        Assert.Equal(uri, received.RequestUri);
        Assert.Equal("CN=leaf", received.CertificateSubject);
    }

    [Fact]
    public async Task Deferred_ssl_fallback_synthesizes_context_and_invokes_hook_once()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var hook = new RecordingNavigationSecurityHooks();
        var adapter = new WindowsWebViewAdapter(hook);
        var navId = Guid.NewGuid();
        var uri = new Uri("https://ssl-fallback.example/");

        var tcs = new TaskCompletionSource<NavigationCompletedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        adapter.NavigationCompleted += (_, e) => tcs.TrySetResult(e);

        adapter.TestOnly_EnqueueDeferredSslNavigation(navId, uri, CoreWebView2WebErrorStatus.CertificateExpired);
        adapter.TestOnly_FlushDeferredSslFallbackSynchronously(uri);

        var args = await tcs.Task;
        Assert.Equal(NavigationCompletedStatus.Failure, args.Status);
        var ex = Assert.IsType<WebViewSslException>(args.Error);
        Assert.Null(ex.CertificateSubject);
        Assert.Null(ex.CertificateIssuer);
        Assert.Null(ex.ValidFrom);
        Assert.Null(ex.ValidTo);
        Assert.Equal(uri.Host, ex.Host);
        Assert.Equal(CoreWebView2WebErrorStatus.CertificateExpired.ToString(), ex.ErrorSummary);
        Assert.Single(hook.Received);
    }
}
