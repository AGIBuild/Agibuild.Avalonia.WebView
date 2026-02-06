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
        var args = new WebResourceRequestedEventArgs(new Uri("https://example.test"), "GET")
        {
            ResponseBody = "{\"ok\":true}",
            ResponseContentType = "application/json",
            ResponseStatusCode = 404,
            Handled = true
        };

        Assert.Equal("{\"ok\":true}", args.ResponseBody);
        Assert.Equal("application/json", args.ResponseContentType);
        Assert.Equal(404, args.ResponseStatusCode);
        Assert.True(args.Handled);
    }

    [Fact]
    public void EnvironmentRequestedEventArgs_can_be_created()
    {
        var args = new EnvironmentRequestedEventArgs();
        Assert.NotNull(args);
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
