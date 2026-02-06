using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public sealed class ContractSemanticsV1SourceAndStopTests
{
    [Fact]
    public void Source_set_updates_last_requested_uri_and_starts_navigation()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        Uri? startedUri = null;
        NavigationCompletedEventArgs? completed = null;
        using var completedRaised = new ManualResetEventSlim(false);

        webView.NavigationStarted += (_, args) => startedUri = args.RequestUri;
        webView.NavigationCompleted += (_, args) =>
        {
            completed = args;
            completedRaised.Set();
        };

        var uri = new Uri("https://example.test/source");
        webView.Source = uri;

        Assert.Equal(uri, webView.Source);
        Assert.Equal(uri, adapter.LastNavigationUri);
        Assert.Equal(uri, startedUri);

        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);

        Assert.True(completedRaised.Wait(TimeSpan.FromSeconds(5)));
        Assert.NotNull(completed);
        Assert.Equal(NavigationCompletedStatus.Success, completed!.Status);
    }

    [Fact]
    public void Source_set_null_throws_ArgumentNullException()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        Assert.Throws<ArgumentNullException>(() => webView.Source = null!);
    }

    [Fact]
    public void NavigateToString_sets_Source_to_about_blank_and_Started_uses_about_blank()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        Uri? startedUri = null;
        webView.NavigationStarted += (_, args) => startedUri = args.RequestUri;

        var navTask = RunOffThread(() => webView.NavigateToStringAsync("<html></html>"));
        dispatcher.RunAll();

        Assert.Equal(new Uri("about:blank"), webView.Source);
        Assert.Equal(new Uri("about:blank"), startedUri);

        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);
        navTask.GetAwaiter().GetResult();
    }

    [Fact]
    public void Stop_returns_false_when_idle()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        Assert.False(webView.Stop());
    }

    [Fact]
    public void Stop_cancels_active_navigation_and_completes_as_canceled()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        NavigationCompletedEventArgs? completed = null;
        webView.NavigationCompleted += (_, args) => completed = args;

        var navTask = RunOffThread(() => webView.NavigateAsync(new Uri("https://example.test")));
        dispatcher.RunAll();

        Assert.True(webView.Stop());

        navTask.GetAwaiter().GetResult();

        Assert.NotNull(completed);
        Assert.Equal(NavigationCompletedStatus.Canceled, completed!.Status);
    }

    private static Task RunOffThread(Func<Task> func)
    {
        using var ready = new ManualResetEventSlim(false);
        var tcs = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);

        var thread = new Thread(() =>
        {
            try
            {
                tcs.SetResult(func());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            finally
            {
                ready.Set();
            }
        })
        {
            IsBackground = true
        };

        thread.Start();
        if (!ready.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new TimeoutException("Off-thread invocation did not start within timeout.");
        }

        return tcs.Task.Unwrap();
    }
}
