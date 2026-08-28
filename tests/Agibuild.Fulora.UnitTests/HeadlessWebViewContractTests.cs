using Agibuild.Fulora.Testing;
using Agibuild.Fulora.UnitTests.TestDoubles;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class HeadlessWebViewContractTests
{
    [Fact]
    public void Navigation_success_preserves_id_and_completes_after_terminal()
    {
        var dispatcher = new TestDispatcher();
        var backend = new HeadlessWebViewBackend();
        var adapter = new HeadlessWebViewAdapter(backend);
        using var core = new WebViewCore(adapter, dispatcher);
        DispatcherTestPump.Run(dispatcher, () => core.AttachAsync(new TestPlatformHandle(IntPtr.Zero, "h"), CancellationToken.None));

        Guid? startedId = null;
        Guid? completedId = null;
        core.NavigationStarted += (_, e) => startedId = e.NavigationId;
        core.NavigationCompleted += (_, e) => completedId = e.NavigationId;

        Task? nav = null;
        DispatcherTestPump.Run(dispatcher, async () =>
        {
            nav = core.NavigateAsync(new Uri("https://example.test/"));
            await Task.Yield();
        });

        Assert.NotNull(startedId);
        Assert.Equal(startedId, adapter.LastNavigationId);
        Assert.False(nav!.IsCompleted);

        backend.CompleteNavigation();
        dispatcher.RunAll();
        DispatcherTestPump.WaitUntil(dispatcher, () => nav.IsCompleted);

        Assert.Equal(startedId, completedId);
        Assert.True(nav.IsCompletedSuccessfully);
    }

    [Fact]
    public void NavigationStarted_cancel_never_dispatches_to_backend()
    {
        var dispatcher = new TestDispatcher();
        var backend = new HeadlessWebViewBackend();
        var adapter = new HeadlessWebViewAdapter(backend);
        using var core = new WebViewCore(adapter, dispatcher);
        DispatcherTestPump.Run(dispatcher, () => core.AttachAsync(new TestPlatformHandle(IntPtr.Zero, "h"), CancellationToken.None));

        core.NavigationStarted += (_, e) => e.Cancel = true;
        var completed = 0;
        core.NavigationCompleted += (_, e) =>
        {
            completed++;
            Assert.Equal(NavigationCompletedStatus.Canceled, e.Status);
        };

        DispatcherTestPump.Run(dispatcher, () => core.NavigateAsync(new Uri("https://example.test/cancel")));
        Assert.Null(adapter.LastNavigationId);
        Assert.Equal(1, completed);
    }

    [Fact]
    public void Duplicate_backend_completion_is_rejected_and_core_still_has_one_terminal()
    {
        var dispatcher = new TestDispatcher();
        var backend = new HeadlessWebViewBackend();
        var adapter = new HeadlessWebViewAdapter(backend);
        using var core = new WebViewCore(adapter, dispatcher);
        DispatcherTestPump.Run(dispatcher, () => core.AttachAsync(new TestPlatformHandle(IntPtr.Zero, "h"), CancellationToken.None));

        var terminals = 0;
        core.NavigationCompleted += (_, _) => terminals++;
        Task? nav = null;
        DispatcherTestPump.Run(dispatcher, async () =>
        {
            nav = core.NavigateAsync(new Uri("https://example.test/dup"));
            await Task.Yield();
        });

        var id = backend.CompleteNavigation();
        dispatcher.RunAll();
        DispatcherTestPump.WaitUntil(dispatcher, () => nav!.IsCompleted);
        Assert.Throws<InvalidOperationException>(() => backend.CompleteNavigation(id, NavigationCompletedStatus.Success));
        Assert.Equal(1, terminals);
    }

    [Fact]
    public void Script_round_trips_text_and_null_result()
    {
        var dispatcher = new TestDispatcher();
        var backend = new HeadlessWebViewBackend();
        var adapter = new HeadlessWebViewAdapter(backend);
        using var core = new WebViewCore(adapter, dispatcher);
        DispatcherTestPump.Run(dispatcher, () => core.AttachAsync(new TestPlatformHandle(IntPtr.Zero, "h"), CancellationToken.None));

        Task<string?>? script = null;
        DispatcherTestPump.Run(dispatcher, async () =>
        {
            script = core.InvokeScriptAsync("1+1");
            await Task.Yield();
        });

        Assert.Equal("1+1", backend.DispatchedScripts[0]);
        backend.CompleteScript(null);
        DispatcherTestPump.WaitUntil(dispatcher, () => script!.IsCompleted);
        Assert.True(script!.IsCompletedSuccessfully);
        Assert.Null(script.Result);
    }

    [Fact]
    public void Script_failure_becomes_WebViewScriptException()
    {
        var dispatcher = new TestDispatcher();
        var backend = new HeadlessWebViewBackend();
        var adapter = new HeadlessWebViewAdapter(backend);
        using var core = new WebViewCore(adapter, dispatcher);
        DispatcherTestPump.Run(dispatcher, () => core.AttachAsync(new TestPlatformHandle(IntPtr.Zero, "h"), CancellationToken.None));

        Task<string?>? script = null;
        DispatcherTestPump.Run(dispatcher, async () =>
        {
            script = core.InvokeScriptAsync("throw 1");
            await Task.Yield();
        });

        backend.FailScript(new InvalidOperationException("boom"));
        DispatcherTestPump.WaitUntil(dispatcher, () => script!.IsCompleted);
        Assert.True(script!.IsFaulted);
        Assert.IsType<WebViewScriptException>(script.Exception!.GetBaseException());
    }

    [Fact]
    public void Message_after_detach_is_ignored()
    {
        var dispatcher = new TestDispatcher();
        var backend = new HeadlessWebViewBackend();
        var adapter = new HeadlessWebViewAdapter(backend);
        using var core = new WebViewCore(adapter, dispatcher);
        DispatcherTestPump.Run(dispatcher, () => core.AttachAsync(new TestPlatformHandle(IntPtr.Zero, "h"), CancellationToken.None));

        var received = 0;
        core.WebMessageReceived += (_, _) => received++;
        core.Detach();
        backend.EmitMessage(new WebMessageEnvelope("x", "https://example.test", Guid.Empty, 1));
        dispatcher.RunAll();
        Assert.Equal(0, received);
        Assert.Equal(1, adapter.DetachCount);
    }

    [Fact]
    public void Dispose_is_idempotent_and_rejects_new_work()
    {
        var dispatcher = new TestDispatcher();
        var backend = new HeadlessWebViewBackend();
        var adapter = new HeadlessWebViewAdapter(backend);
        var core = new WebViewCore(adapter, dispatcher);
        DispatcherTestPump.Run(dispatcher, () => core.AttachAsync(new TestPlatformHandle(IntPtr.Zero, "h"), CancellationToken.None));

        core.Dispose();
        core.Dispose();
        var navigate = core.NavigateAsync(new Uri("https://example.test/"));
        Assert.True(navigate.IsFaulted);
        Assert.IsType<ObjectDisposedException>(navigate.Exception!.GetBaseException());
    }
}
