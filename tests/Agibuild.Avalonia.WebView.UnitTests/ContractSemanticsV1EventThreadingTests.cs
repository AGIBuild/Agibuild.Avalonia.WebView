using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public sealed class ContractSemanticsV1EventThreadingTests
{
    [Fact]
    public void All_public_events_are_raised_on_ui_thread()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        webView.EnableWebMessageBridge(new WebMessageBridgeOptions
        {
            // Empty allowlist = allow all (still enforces protocol + channel).
            AllowedOrigins = new HashSet<string>(StringComparer.Ordinal),
            ProtocolVersion = 1
        });

        var startedThreadId = -1;
        var completedThreadId = -1;
        var messageThreadId = -1;

        webView.NavigationStarted += (_, _) => startedThreadId = Environment.CurrentManagedThreadId;
        webView.NavigationCompleted += (_, _) => completedThreadId = Environment.CurrentManagedThreadId;

        using var messageRaised = new ManualResetEventSlim(false);
        webView.WebMessageReceived += (_, _) =>
        {
            messageThreadId = Environment.CurrentManagedThreadId;
            messageRaised.Set();
        };

        var navTask = ThreadingTestHelper.RunOffThread(() => webView.NavigateAsync(new Uri("https://example.test")));
        dispatcher.RunAll();
        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);
        navTask.GetAwaiter().GetResult();

        Assert.Equal(dispatcher.UiThreadId, startedThreadId);
        Assert.Equal(dispatcher.UiThreadId, completedThreadId);

        using var messageEnqueued = new ManualResetEventSlim(false);
        var bgThread = new Thread(() =>
        {
            adapter.RaiseWebMessage("{\"x\":1}", "https://any.test", webView.ChannelId, protocolVersion: 1);
            messageEnqueued.Set();
        })
        {
            IsBackground = true
        };
        bgThread.Start();

        Assert.True(messageEnqueued.Wait(TimeSpan.FromSeconds(5)));
        dispatcher.RunAll();

        Assert.True(messageRaised.Wait(TimeSpan.FromSeconds(5)));
        Assert.Equal(dispatcher.UiThreadId, messageThreadId);
    }

}

