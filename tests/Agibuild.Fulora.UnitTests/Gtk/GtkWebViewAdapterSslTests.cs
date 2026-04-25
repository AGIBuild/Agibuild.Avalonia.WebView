using Agibuild.Fulora;
using Agibuild.Fulora.Adapters.Abstractions;
using Agibuild.Fulora.Adapters.Gtk;
using Agibuild.Fulora.Security;
using Agibuild.Fulora.UnitTests.TestDoubles;
using Xunit;

namespace Agibuild.Fulora.UnitTests.Gtk;

public sealed class GtkWebViewAdapterSslTests
{
    [Fact]
    public async Task Ssl_completion_invokes_hook_once_and_raises_WebViewSslException_with_certificate_fields()
    {
        var hook = new RecordingNavigationSecurityHooks();
        var adapter = new GtkWebViewAdapter(hook);
        var navId = Guid.NewGuid();
        var uri = new Uri("https://gtk-ssl-meta.example/path");

        var tcs = new TaskCompletionSource<NavigationCompletedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        adapter.NavigationCompleted += (_, e) => tcs.TrySetResult(e);

        adapter.TestOnly_SetActiveNavigationForSslTest(navId, uri);
        var vf = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var vt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        adapter.TestOnly_RaiseNavigationCompletedFromNative(
            uri.AbsoluteUri,
            status: 5,
            errorCode: 7,
            errorMessage: "",
            host: "gtk-ssl-meta.example",
            summary: "BAD_IDENTITY, EXPIRED",
            subject: "CN=leaf",
            issuer: "CN=ca",
            validFromUnix: vf.ToUnixTimeSeconds(),
            validToUnix: vt.ToUnixTimeSeconds());

        var args = await tcs.Task;
        Assert.Equal(NavigationCompletedStatus.Failure, args.Status);
        Assert.Equal(navId, args.NavigationId);
        var ex = Assert.IsType<WebViewSslException>(args.Error);
        Assert.Equal("CN=leaf", ex.CertificateSubject);
        Assert.Equal("CN=ca", ex.CertificateIssuer);
        Assert.Equal(vf, ex.ValidFrom);
        Assert.Equal(vt, ex.ValidTo);
        Assert.Equal("gtk-ssl-meta.example", ex.Host);
        Assert.Equal("BAD_IDENTITY, EXPIRED", ex.ErrorSummary);
        var received = Assert.Single(hook.Received);
        Assert.Equal(uri, received.RequestUri);
        Assert.Equal("gtk-ssl-meta.example", received.Host);
        Assert.Equal("BAD_IDENTITY, EXPIRED", received.ErrorSummary);
        Assert.Equal("CN=leaf", received.CertificateSubject);
        Assert.Equal("CN=ca", received.CertificateIssuer);
        Assert.Equal(vf, received.ValidFrom);
        Assert.Equal(vt, received.ValidTo);
        Assert.Equal(7, received.PlatformRawCode);
    }

    [Fact]
    public async Task Ssl_completion_with_missing_optional_cert_fields_still_invokes_hook_once()
    {
        var hook = new RecordingNavigationSecurityHooks();
        var adapter = new GtkWebViewAdapter(hook);
        var navId = Guid.NewGuid();
        var uri = new Uri("https://gtk-ssl-min.example/");

        var tcs = new TaskCompletionSource<NavigationCompletedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        adapter.NavigationCompleted += (_, e) => tcs.TrySetResult(e);

        adapter.TestOnly_SetActiveNavigationForSslTest(navId, uri);
        adapter.TestOnly_RaiseNavigationCompletedFromNative(
            uri.AbsoluteUri,
            status: 5,
            errorCode: 3,
            errorMessage: "",
            host: null,
            summary: null,
            subject: null,
            issuer: null,
            validFromUnix: 0,
            validToUnix: 0);

        var args = await tcs.Task;
        Assert.Equal(NavigationCompletedStatus.Failure, args.Status);
        var ex = Assert.IsType<WebViewSslException>(args.Error);
        Assert.Null(ex.CertificateSubject);
        Assert.Null(ex.CertificateIssuer);
        Assert.Null(ex.ValidFrom);
        Assert.Null(ex.ValidTo);
        Assert.Equal(uri.Host, ex.Host);
        Assert.Equal("TLS certificate error", ex.ErrorSummary);
        var received = Assert.Single(hook.Received);
        Assert.Null(received.CertificateSubject);
        Assert.Null(received.CertificateIssuer);
        Assert.Null(received.ValidFrom);
        Assert.Null(received.ValidTo);
    }

    [Fact]
    public async Task Success_completion_does_not_invoke_security_hook()
    {
        var hook = new RecordingNavigationSecurityHooks();
        var adapter = new GtkWebViewAdapter(hook);
        var navId = Guid.NewGuid();
        var uri = new Uri("https://gtk-ok.example/");

        var tcs = new TaskCompletionSource<NavigationCompletedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        adapter.NavigationCompleted += (_, e) => tcs.TrySetResult(e);

        adapter.TestOnly_SetActiveNavigationForSslTest(navId, uri);
        adapter.TestOnly_RaiseNavigationCompletedFromNative(
            uri.AbsoluteUri,
            status: 0,
            errorCode: 0,
            errorMessage: "",
            host: null,
            summary: null,
            subject: null,
            issuer: null,
            validFromUnix: 0,
            validToUnix: 0);

        var args = await tcs.Task;
        Assert.Equal(NavigationCompletedStatus.Success, args.Status);
        Assert.Null(args.Error);
        Assert.Empty(hook.Received);
    }

    [Fact]
    public async Task Network_failure_uses_NavigationErrorFactory_and_skips_security_hook()
    {
        var hook = new RecordingNavigationSecurityHooks();
        var adapter = new GtkWebViewAdapter(hook);
        var navId = Guid.NewGuid();
        var uri = new Uri("https://gtk-net.example/");

        var tcs = new TaskCompletionSource<NavigationCompletedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        adapter.NavigationCompleted += (_, e) => tcs.TrySetResult(e);

        adapter.TestOnly_SetActiveNavigationForSslTest(navId, uri);
        adapter.TestOnly_RaiseNavigationCompletedFromNative(
            uri.AbsoluteUri,
            status: 4,
            errorCode: 42,
            errorMessage: "Transport failed",
            host: null,
            summary: null,
            subject: null,
            issuer: null,
            validFromUnix: 0,
            validToUnix: 0);

        var args = await tcs.Task;
        Assert.Equal(NavigationCompletedStatus.Failure, args.Status);
        _ = Assert.IsType<WebViewNetworkException>(args.Error);
        Assert.Empty(hook.Received);
    }
}
