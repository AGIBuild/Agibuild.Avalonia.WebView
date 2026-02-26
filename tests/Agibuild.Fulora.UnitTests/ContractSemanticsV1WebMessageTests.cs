using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class ContractSemanticsV1WebMessageTests
{
    [Fact]
    public void WebMessage_bridge_is_disabled_by_default()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        var raised = false;
        webView.WebMessageReceived += (_, _) => raised = true;

        adapter.RaiseWebMessage("{\"x\":1}", "https://example.test", webView.ChannelId);

        Assert.False(raised);
    }

    [Fact]
    public void WebMessage_drops_are_observable_with_drop_reason()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        var sink = new CaptureDropSink();

        webView.EnableWebMessageBridge(new WebMessageBridgeOptions
        {
            AllowedOrigins = new HashSet<string>(StringComparer.Ordinal) { "https://allowed.test" },
            ProtocolVersion = 1,
            DropDiagnosticsSink = sink
        });

        adapter.RaiseWebMessage("{\"x\":1}", "https://denied.test", webView.ChannelId, protocolVersion: 1);

        Assert.True(sink.HasValue);
        Assert.Equal(WebMessageDropReason.OriginNotAllowed, sink.Last.Reason);
        Assert.Equal("https://denied.test", sink.Last.Origin);
        Assert.Equal(webView.ChannelId, sink.Last.ChannelId);
    }

    [Fact]
    public void WebMessage_protocol_mismatch_is_dropped_and_observable()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        var sink = new CaptureDropSink();

        webView.EnableWebMessageBridge(new WebMessageBridgeOptions
        {
            AllowedOrigins = new HashSet<string>(StringComparer.Ordinal),
            ProtocolVersion = 2,
            DropDiagnosticsSink = sink
        });

        adapter.RaiseWebMessage("{\"x\":1}", "https://any.test", webView.ChannelId, protocolVersion: 1);

        Assert.True(sink.HasValue);
        Assert.Equal(WebMessageDropReason.ProtocolMismatch, sink.Last.Reason);
    }

    [Fact]
    public void WebMessage_channel_mismatch_is_dropped_and_observable()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        var sink = new CaptureDropSink();

        webView.EnableWebMessageBridge(new WebMessageBridgeOptions
        {
            AllowedOrigins = new HashSet<string>(StringComparer.Ordinal),
            ProtocolVersion = 1,
            DropDiagnosticsSink = sink
        });

        adapter.RaiseWebMessage("{\"x\":1}", "https://any.test", Guid.NewGuid(), protocolVersion: 1);

        Assert.True(sink.HasValue);
        Assert.Equal(WebMessageDropReason.ChannelMismatch, sink.Last.Reason);
    }

    private sealed class CaptureDropSink : IWebMessageDropDiagnosticsSink
    {
        public bool HasValue { get; private set; }
        public WebMessageDropDiagnostic Last { get; private set; }

        public void OnMessageDropped(in WebMessageDropDiagnostic diagnostic)
        {
            HasValue = true;
            Last = diagnostic;
        }
    }
}

