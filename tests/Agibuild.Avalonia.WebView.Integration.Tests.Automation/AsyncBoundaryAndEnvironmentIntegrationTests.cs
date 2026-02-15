using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Testing;
using Avalonia.Headless.XUnit;
using Avalonia.Platform;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Automation;

public sealed class AsyncBoundaryAndEnvironmentIntegrationTests
{
    [AvaloniaFact]
    public async Task TryGetWebViewHandleAsync_returns_null_before_core_attach()
    {
        var webView = new WebView();

        var handle = await webView.TryGetWebViewHandleAsync();

        Assert.Null(handle);
    }

    [AvaloniaFact]
    public async Task TryGetWebViewHandleAsync_returns_handle_after_attach_and_null_after_dispose()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new ThreadAwareHandleAdapter
        {
            HandleToReturn = new TestWindowsWebView2PlatformHandle((nint)123, (nint)456, (nint)789)
        };
        using var core = new WebViewCore(adapter, dispatcher, NullLogger<WebViewCore>.Instance);
        var webView = new WebView();

        core.Attach(new PlatformHandle(nint.Zero, "test-parent"));
        webView.TestOnlyAttachCore(core);
        webView.TestOnlySubscribeCoreEvents();

        var attachedHandle = await webView.TryGetWebViewHandleAsync();
        Assert.NotNull(attachedHandle);

        core.Dispose();
        var disposedHandle = await webView.TryGetWebViewHandleAsync();
        Assert.Null(disposedHandle);
    }

    [AvaloniaFact]
    public async Task TryGetWebViewHandleAsync_off_thread_marshals_to_ui_thread()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new ThreadAwareHandleAdapter
        {
            HandleToReturn = new TestWindowsWebView2PlatformHandle((nint)42, (nint)43, (nint)44)
        };
        using var core = new WebViewCore(adapter, dispatcher, NullLogger<WebViewCore>.Instance);

        var task = ThreadingTestHelper.RunOffThread(() => core.TryGetWebViewHandleAsync());
        DispatcherTestPump.WaitUntil(dispatcher, () => task.IsCompleted);

        var handle = await task;
        Assert.NotNull(handle);
        Assert.Equal(dispatcher.UiThreadId, adapter.LastTryGetHandleThreadId);
    }

    [AvaloniaFact]
    public async Task NavigateAsync_off_thread_marshals_to_ui_thread()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        adapter.AutoCompleteNavigation = true;
        using var core = new WebViewCore(adapter, dispatcher, NullLogger<WebViewCore>.Instance);

        var task = ThreadingTestHelper.RunOffThread(() => core.NavigateAsync(new Uri("https://async-boundary.example")));
        DispatcherTestPump.WaitUntil(dispatcher, () => task.IsCompleted);
        await task;

        Assert.Equal(dispatcher.UiThreadId, adapter.LastNavigateThreadId);
    }

    [AvaloniaFact]
    public void Instance_environment_options_are_isolated_between_cores()
    {
        var original = WebViewEnvironment.Options;
        try
        {
            var globalOptions = new WebViewEnvironmentOptions
            {
                EnableDevTools = false,
                UseEphemeralSession = false,
                CustomUserAgent = "global-agent"
            };
            WebViewEnvironment.Options = globalOptions;

            var adapter1 = MockWebViewAdapter.CreateWithOptions();
            var adapter2 = MockWebViewAdapter.CreateWithOptions();
            var options1 = new WebViewEnvironmentOptions
            {
                EnableDevTools = true,
                UseEphemeralSession = false,
                CustomUserAgent = "instance-agent-1"
            };
            var options2 = new WebViewEnvironmentOptions
            {
                EnableDevTools = false,
                UseEphemeralSession = true,
                CustomUserAgent = "instance-agent-2"
            };

            using var _ = new WebViewCore(adapter1, new TestDispatcher(), NullLogger<WebViewCore>.Instance, options1);
            using var __ = new WebViewCore(adapter2, new TestDispatcher(), NullLogger<WebViewCore>.Instance, options2);

            Assert.Equal(1, adapter1.ApplyOptionsCallCount);
            Assert.Equal(1, adapter2.ApplyOptionsCallCount);
            Assert.Same(options1, adapter1.AppliedOptions);
            Assert.Same(options2, adapter2.AppliedOptions);
            Assert.NotSame(adapter1.AppliedOptions, adapter2.AppliedOptions);
            Assert.Same(globalOptions, WebViewEnvironment.Options);
            Assert.Equal("global-agent", WebViewEnvironment.Options.CustomUserAgent);
        }
        finally
        {
            WebViewEnvironment.Options = original;
        }
    }

    [AvaloniaFact]
    public void AvaloniaWebDialog_options_do_not_mutate_global_environment_options()
    {
        var original = WebViewEnvironment.Options;
        try
        {
            var globalOptions = new WebViewEnvironmentOptions
            {
                EnableDevTools = false,
                UseEphemeralSession = false,
                CustomUserAgent = "global-agent"
            };
            WebViewEnvironment.Options = globalOptions;

            var dialogOptions = new WebViewEnvironmentOptions
            {
                EnableDevTools = true,
                UseEphemeralSession = true,
                CustomUserAgent = "dialog-agent",
                CustomSchemes = [new CustomSchemeRegistration { SchemeName = "app" }],
                PreloadScripts = ["window.__dialog = true;"]
            };

            using var dialog = new AvaloniaWebDialog(dialogOptions);

            Assert.Same(globalOptions, WebViewEnvironment.Options);
            Assert.Equal("global-agent", WebViewEnvironment.Options.CustomUserAgent);

            var innerWebView = dialog.TestOnlyInnerWebView;
            var instanceOptions = Assert.IsType<WebViewEnvironmentOptions>(innerWebView.EnvironmentOptions);
            Assert.NotSame(dialogOptions, instanceOptions);
            Assert.Equal("dialog-agent", instanceOptions.CustomUserAgent);
            Assert.NotSame(dialogOptions.CustomSchemes, instanceOptions.CustomSchemes);
            Assert.NotSame(dialogOptions.PreloadScripts, instanceOptions.PreloadScripts);
        }
        finally
        {
            WebViewEnvironment.Options = original;
        }
    }

    private sealed class ThreadAwareHandleAdapter : MockWebViewAdapter, INativeWebViewHandleProvider
    {
        public IPlatformHandle? HandleToReturn { get; set; }
        public int? LastTryGetHandleThreadId { get; private set; }

        public IPlatformHandle? TryGetWebViewHandle()
        {
            LastTryGetHandleThreadId = Environment.CurrentManagedThreadId;
            return HandleToReturn;
        }
    }
}
