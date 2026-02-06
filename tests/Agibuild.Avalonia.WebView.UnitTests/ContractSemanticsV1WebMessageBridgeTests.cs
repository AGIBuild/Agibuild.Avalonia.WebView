using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public sealed class ContractSemanticsV1WebMessageBridgeTests
{
    [Fact]
    public void WebMessage_not_forwarded_when_bridge_disabled()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        WebMessageReceivedEventArgs? received = null;
        core.WebMessageReceived += (_, e) => received = e;

        adapter.RaiseWebMessage("hello", "https://origin.test", core.ChannelId);
        dispatcher.RunAll();

        Assert.Null(received); // Bridge not enabled, message dropped
    }

    [Fact]
    public void WebMessage_forwarded_when_bridge_enabled_and_origin_allowed()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        core.EnableWebMessageBridge(new WebMessageBridgeOptions
        {
            AllowedOrigins = new HashSet<string>(StringComparer.Ordinal) { "https://origin.test" },
            ProtocolVersion = 1
        });

        WebMessageReceivedEventArgs? received = null;
        core.WebMessageReceived += (_, e) => received = e;

        adapter.RaiseWebMessage("hello", "https://origin.test", core.ChannelId);
        dispatcher.RunAll();

        Assert.NotNull(received);
        Assert.Equal("hello", received!.Body);
    }

    [Fact]
    public void WebMessage_dropped_when_origin_not_allowed()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        core.EnableWebMessageBridge(new WebMessageBridgeOptions
        {
            AllowedOrigins = new HashSet<string>(StringComparer.Ordinal) { "https://allowed.test" },
            ProtocolVersion = 1
        });

        WebMessageReceivedEventArgs? received = null;
        core.WebMessageReceived += (_, e) => received = e;

        adapter.RaiseWebMessage("hello", "https://evil.test", core.ChannelId);
        dispatcher.RunAll();

        Assert.Null(received); // Dropped by policy
    }

    [Fact]
    public void WebMessage_dropped_when_channel_id_mismatch()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        core.EnableWebMessageBridge(new WebMessageBridgeOptions
        {
            AllowedOrigins = new HashSet<string>(StringComparer.Ordinal) { "https://origin.test" },
            ProtocolVersion = 1
        });

        WebMessageReceivedEventArgs? received = null;
        core.WebMessageReceived += (_, e) => received = e;

        // Wrong channel ID
        adapter.RaiseWebMessage("hello", "https://origin.test", Guid.NewGuid());
        dispatcher.RunAll();

        Assert.Null(received); // Dropped due to channel mismatch
    }

    [Fact]
    public void WebMessage_dropped_when_protocol_version_mismatch()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        core.EnableWebMessageBridge(new WebMessageBridgeOptions
        {
            AllowedOrigins = new HashSet<string>(StringComparer.Ordinal) { "https://origin.test" },
            ProtocolVersion = 1
        });

        WebMessageReceivedEventArgs? received = null;
        core.WebMessageReceived += (_, e) => received = e;

        // Wrong protocol version
        adapter.RaiseWebMessage("hello", "https://origin.test", core.ChannelId, 99);
        dispatcher.RunAll();

        Assert.Null(received); // Dropped due to protocol mismatch
    }

    [Fact]
    public void WebMessage_dropped_with_diagnostics_sink_reports_reason()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        var sink = new TestDropDiagnosticsSink();
        core.EnableWebMessageBridge(new WebMessageBridgeOptions
        {
            AllowedOrigins = new HashSet<string>(StringComparer.Ordinal) { "https://allowed.test" },
            ProtocolVersion = 1,
            DropDiagnosticsSink = sink
        });

        adapter.RaiseWebMessage("hello", "https://evil.test", core.ChannelId);
        dispatcher.RunAll();

        Assert.Single(sink.Diagnostics);
        Assert.Equal(WebMessageDropReason.OriginNotAllowed, sink.Diagnostics[0].Reason);
    }

    [Fact]
    public void DisableWebMessageBridge_stops_forwarding()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        core.EnableWebMessageBridge(new WebMessageBridgeOptions
        {
            AllowedOrigins = new HashSet<string>(StringComparer.Ordinal) { "https://origin.test" },
            ProtocolVersion = 1
        });

        core.DisableWebMessageBridge();

        WebMessageReceivedEventArgs? received = null;
        core.WebMessageReceived += (_, e) => received = e;

        adapter.RaiseWebMessage("hello", "https://origin.test", core.ChannelId);
        dispatcher.RunAll();

        Assert.Null(received);
    }

    [Fact]
    public void EnableWebMessageBridge_null_throws_ArgumentNullException()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        Assert.Throws<ArgumentNullException>(() => core.EnableWebMessageBridge(null!));
    }

    [Fact]
    public void WebMessage_ignored_after_dispose()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        core.EnableWebMessageBridge(new WebMessageBridgeOptions
        {
            AllowedOrigins = new HashSet<string>(StringComparer.Ordinal) { "https://origin.test" },
            ProtocolVersion = 1
        });

        core.Dispose();

        // Should not throw
        adapter.RaiseWebMessage("hello", "https://origin.test", core.ChannelId);
    }

    private sealed class TestDropDiagnosticsSink : IWebMessageDropDiagnosticsSink
    {
        public List<WebMessageDropDiagnostic> Diagnostics { get; } = [];

        public void OnMessageDropped(in WebMessageDropDiagnostic diagnostic)
        {
            Diagnostics.Add(diagnostic);
        }
    }
}
