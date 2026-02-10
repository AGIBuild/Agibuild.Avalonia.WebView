using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

/// <summary>
/// Tests for event args constructors and validation paths
/// that are not exercised by the higher-level contract tests.
/// </summary>
public sealed class EventArgsConstructorTests
{
    // --- NavigationStartingEventArgs ---

    [Fact]
    public void NavigationStartingEventArgs_single_param_ctor_sets_empty_id()
    {
        var args = new NavigationStartingEventArgs(new Uri("https://example.test"));

        Assert.Equal(Guid.Empty, args.NavigationId);
        Assert.Equal(new Uri("https://example.test"), args.RequestUri);
        Assert.False(args.Cancel);
    }

    [Fact]
    public void NavigationStartingEventArgs_two_param_ctor_sets_all_properties()
    {
        var id = Guid.NewGuid();
        var args = new NavigationStartingEventArgs(id, new Uri("https://example.test"));

        Assert.Equal(id, args.NavigationId);
    }

    // --- NavigationCompletedEventArgs ---

    [Fact]
    public void NavigationCompletedEventArgs_default_ctor_sets_success_status()
    {
        var args = new NavigationCompletedEventArgs();

        Assert.Equal(Guid.Empty, args.NavigationId);
        Assert.Equal(new Uri("about:blank"), args.RequestUri);
        Assert.Equal(NavigationCompletedStatus.Success, args.Status);
        Assert.Null(args.Error);
    }

    [Fact]
    public void NavigationCompletedEventArgs_failure_requires_error()
    {
        var id = Guid.NewGuid();
        var uri = new Uri("https://example.test");

        Assert.Throws<ArgumentNullException>(() =>
            new NavigationCompletedEventArgs(id, uri, NavigationCompletedStatus.Failure, null));
    }

    [Fact]
    public void NavigationCompletedEventArgs_non_failure_rejects_error()
    {
        var id = Guid.NewGuid();
        var uri = new Uri("https://example.test");

        Assert.Throws<ArgumentException>(() =>
            new NavigationCompletedEventArgs(id, uri, NavigationCompletedStatus.Success,
                new Exception("should not be here")));
    }

    [Fact]
    public void NavigationCompletedEventArgs_canceled_status()
    {
        var id = Guid.NewGuid();
        var uri = new Uri("https://example.test");
        var args = new NavigationCompletedEventArgs(id, uri, NavigationCompletedStatus.Canceled, null);

        Assert.Equal(NavigationCompletedStatus.Canceled, args.Status);
        Assert.Null(args.Error);
    }

    [Fact]
    public void NavigationCompletedEventArgs_superseded_status()
    {
        var id = Guid.NewGuid();
        var uri = new Uri("https://example.test");
        var args = new NavigationCompletedEventArgs(id, uri, NavigationCompletedStatus.Superseded, null);

        Assert.Equal(NavigationCompletedStatus.Superseded, args.Status);
    }

    [Fact]
    public void NavigationCompletedEventArgs_failure_with_error()
    {
        var id = Guid.NewGuid();
        var uri = new Uri("https://example.test");
        var error = new WebViewNavigationException("failed", id, uri);
        var args = new NavigationCompletedEventArgs(id, uri, NavigationCompletedStatus.Failure, error);

        Assert.Equal(NavigationCompletedStatus.Failure, args.Status);
        Assert.Same(error, args.Error);
    }

    // --- WebMessageReceivedEventArgs ---

    [Fact]
    public void WebMessageReceivedEventArgs_default_ctor()
    {
        var args = new WebMessageReceivedEventArgs();

        Assert.Equal(string.Empty, args.Body);
        Assert.Equal(string.Empty, args.Origin);
        Assert.Equal(Guid.Empty, args.ChannelId);
        Assert.Equal(1, args.ProtocolVersion);
    }

    [Fact]
    public void WebMessageReceivedEventArgs_three_param_ctor()
    {
        var channelId = Guid.NewGuid();
        var args = new WebMessageReceivedEventArgs("hello", "https://origin.test", channelId);

        Assert.Equal("hello", args.Body);
        Assert.Equal("https://origin.test", args.Origin);
        Assert.Equal(channelId, args.ChannelId);
        Assert.Equal(1, args.ProtocolVersion);
    }

    [Fact]
    public void WebMessageReceivedEventArgs_four_param_ctor()
    {
        var channelId = Guid.NewGuid();
        var args = new WebMessageReceivedEventArgs("body", "origin", channelId, 2);

        Assert.Equal("body", args.Body);
        Assert.Equal("origin", args.Origin);
        Assert.Equal(channelId, args.ChannelId);
        Assert.Equal(2, args.ProtocolVersion);
    }

    // --- WebResourceRequestedEventArgs / EnvironmentRequestedEventArgs ---

    [Fact]
    public void WebResourceRequestedEventArgs_default_ctor()
    {
        var args = new WebResourceRequestedEventArgs();
        Assert.NotNull(args);
        Assert.Null(args.RequestUri);
        Assert.Equal("GET", args.Method);
        Assert.Null(args.ResponseBody);
        Assert.Equal("text/html", args.ResponseContentType);
        Assert.Equal(200, args.ResponseStatusCode);
        Assert.False(args.Handled);
    }

    [Fact]
    public void WebResourceRequestedEventArgs_parameterized_ctor()
    {
        var uri = new Uri("https://example.test/resource");
        var args = new WebResourceRequestedEventArgs(uri, "POST");

        Assert.Equal(uri, args.RequestUri);
        Assert.Equal("POST", args.Method);
        Assert.Null(args.ResponseBody);
        Assert.Equal("text/html", args.ResponseContentType);
        Assert.Equal(200, args.ResponseStatusCode);
        Assert.False(args.Handled);
    }

    [Fact]
    public void WebResourceRequestedEventArgs_response_properties_settable()
    {
        using var stream = new MemoryStream("{\"ok\":true}"u8.ToArray());
        var args = new WebResourceRequestedEventArgs(new Uri("https://example.test"), "GET")
        {
            ResponseBody = stream,
            ResponseContentType = "application/json",
            ResponseStatusCode = 404,
            ResponseHeaders = new Dictionary<string, string> { ["X-Custom"] = "test" },
            Handled = true
        };

        Assert.Same(stream, args.ResponseBody);
        Assert.Equal("application/json", args.ResponseContentType);
        Assert.Equal(404, args.ResponseStatusCode);
        Assert.True(args.Handled);
        Assert.Equal("test", args.ResponseHeaders!["X-Custom"]);
    }

    [Fact]
    public void EnvironmentRequestedEventArgs_can_be_created()
    {
        var args = new EnvironmentRequestedEventArgs();
        Assert.NotNull(args);
    }

    // --- CustomSchemeRegistration ---

    [Fact]
    public void CustomSchemeRegistration_defaults()
    {
        var reg = new CustomSchemeRegistration { SchemeName = "app" };
        Assert.Equal("app", reg.SchemeName);
        Assert.False(reg.HasAuthorityComponent);
        Assert.False(reg.TreatAsSecure);
    }

    [Fact]
    public void CustomSchemeRegistration_with_authority()
    {
        var reg = new CustomSchemeRegistration { SchemeName = "custom", HasAuthorityComponent = true, TreatAsSecure = true };
        Assert.Equal("custom", reg.SchemeName);
        Assert.True(reg.HasAuthorityComponent);
        Assert.True(reg.TreatAsSecure);
    }

    // --- DownloadRequestedEventArgs ---

    [Fact]
    public void DownloadRequestedEventArgs_ctor_sets_properties()
    {
        var uri = new Uri("https://example.test/file.pdf");
        var args = new DownloadRequestedEventArgs(uri, "file.pdf", "application/pdf", 1024);

        Assert.Equal(uri, args.DownloadUri);
        Assert.Equal("file.pdf", args.SuggestedFileName);
        Assert.Equal("application/pdf", args.ContentType);
        Assert.Equal(1024, args.ContentLength);
        Assert.Null(args.DownloadPath);
        Assert.False(args.Cancel);
        Assert.False(args.Handled);
    }

    [Fact]
    public void DownloadRequestedEventArgs_consumer_can_set_path_and_cancel()
    {
        var args = new DownloadRequestedEventArgs(new Uri("https://example.test/f.zip"));
        args.DownloadPath = "/tmp/f.zip";
        Assert.Equal("/tmp/f.zip", args.DownloadPath);

        args.Cancel = true;
        Assert.True(args.Cancel);

        args.Handled = true;
        Assert.True(args.Handled);
    }

    // --- PermissionRequestedEventArgs ---

    [Fact]
    public void PermissionRequestedEventArgs_defaults()
    {
        var args = new PermissionRequestedEventArgs(WebViewPermissionKind.Camera);
        Assert.Equal(WebViewPermissionKind.Camera, args.PermissionKind);
        Assert.Null(args.Origin);
        Assert.Equal(PermissionState.Default, args.State);
    }

    [Fact]
    public void PermissionRequestedEventArgs_with_origin()
    {
        var origin = new Uri("https://example.test");
        var args = new PermissionRequestedEventArgs(WebViewPermissionKind.Geolocation, origin);
        Assert.Equal(WebViewPermissionKind.Geolocation, args.PermissionKind);
        Assert.Equal(origin, args.Origin);
    }

    [Fact]
    public void PermissionRequestedEventArgs_consumer_can_set_state()
    {
        var args = new PermissionRequestedEventArgs(WebViewPermissionKind.Microphone);
        args.State = PermissionState.Allow;
        Assert.Equal(PermissionState.Allow, args.State);

        args.State = PermissionState.Deny;
        Assert.Equal(PermissionState.Deny, args.State);
    }

    [Fact]
    public void WebViewPermissionKind_enum_values()
    {
        Assert.Equal(0, (int)WebViewPermissionKind.Unknown);
        Assert.Equal(1, (int)WebViewPermissionKind.Camera);
        Assert.Equal(2, (int)WebViewPermissionKind.Microphone);
        Assert.Equal(3, (int)WebViewPermissionKind.Geolocation);
    }

    [Fact]
    public void PermissionState_enum_values()
    {
        Assert.Equal(0, (int)PermissionState.Default);
        Assert.Equal(1, (int)PermissionState.Allow);
        Assert.Equal(2, (int)PermissionState.Deny);
    }

    // --- WebAuthCallbackMatcher ---

    [Fact]
    public void WebAuthCallbackMatcher_exact_match_returns_true()
    {
        var expected = new Uri("https://callback.test/auth");
        var actual = new Uri("https://callback.test/auth?code=123");

        Assert.True(WebAuthCallbackMatcher.IsStrictMatch(expected, actual));
    }

    [Fact]
    public void WebAuthCallbackMatcher_different_path_returns_false()
    {
        var expected = new Uri("https://callback.test/auth");
        var actual = new Uri("https://callback.test/other?code=123");

        Assert.False(WebAuthCallbackMatcher.IsStrictMatch(expected, actual));
    }

    [Fact]
    public void WebAuthCallbackMatcher_different_host_returns_false()
    {
        var expected = new Uri("https://callback.test/auth");
        var actual = new Uri("https://evil.test/auth?code=123");

        Assert.False(WebAuthCallbackMatcher.IsStrictMatch(expected, actual));
    }

    [Fact]
    public void WebAuthCallbackMatcher_different_scheme_returns_false()
    {
        var expected = new Uri("https://callback.test/auth");
        var actual = new Uri("http://callback.test/auth?code=123");

        Assert.False(WebAuthCallbackMatcher.IsStrictMatch(expected, actual));
    }

    [Fact]
    public void WebAuthCallbackMatcher_null_expected_throws()
    {
        var actual = new Uri("https://callback.test/auth");
        Assert.Throws<ArgumentNullException>(() => WebAuthCallbackMatcher.IsStrictMatch(null!, actual));
    }

    [Fact]
    public void WebAuthCallbackMatcher_null_actual_throws()
    {
        var expected = new Uri("https://callback.test/auth");
        Assert.Throws<ArgumentNullException>(() => WebAuthCallbackMatcher.IsStrictMatch(expected, null!));
    }

    [Fact]
    public void WebAuthCallbackMatcher_different_port_returns_false()
    {
        var expected = new Uri("https://callback.test:443/auth");
        var actual = new Uri("https://callback.test:8443/auth?code=123");

        Assert.False(WebAuthCallbackMatcher.IsStrictMatch(expected, actual));
    }

    [Fact]
    public void WebAuthCallbackMatcher_relative_uri_returns_false()
    {
        // Using relative URIs (non-absolute) should return false.
        var expected = new Uri("https://callback.test/auth");
        var relative = new Uri("/auth?code=123", UriKind.Relative);

        // Create absolute for expected, relative for actual
        Assert.False(WebAuthCallbackMatcher.IsStrictMatch(expected, relative));
    }
}
