using System.Reflection;
using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public sealed class WebViewCoreHotspotCoverageTests
{
    private readonly TestDispatcher _dispatcher = new();

    [Fact]
    public void OnNativeNavigationStartingOnUiThread_disposed_denies_navigation()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);
        core.Dispose();

        var info = new NativeNavigationStartingInfo(
            Guid.NewGuid(),
            new Uri("https://hotspot.test/native"),
            IsMainFrame: true);

        var method = RequireInstanceMethod(
            nameof(OnNativeNavigationStartingOnUiThread_disposed_denies_navigation),
            "OnNativeNavigationStartingOnUiThread",
            typeof(NativeNavigationStartingInfo));
        var result = Assert.IsType<NativeNavigationStartingDecision>(method.Invoke(core, [info]));

        Assert.False(result.IsAllowed);
        Assert.Equal(Guid.Empty, result.NavigationId);
    }

    [Fact]
    public async Task InvokeAsyncOnUiThread_non_generic_disposed_returns_disposed_error()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);
        core.Dispose();

        var method = RequireInstanceMethod(
            nameof(InvokeAsyncOnUiThread_non_generic_disposed_returns_disposed_error),
            "InvokeAsyncOnUiThread",
            typeof(Func<Task>));
        var task = Assert.IsType<Task>(method.Invoke(core, [(Func<Task>)(() => Task.CompletedTask)]));

        var ex = await Assert.ThrowsAsync<ObjectDisposedException>(async () => await task);
        Assert.True(WebViewOperationFailure.TryGetCategory(ex, out var category));
        Assert.Equal(WebViewOperationFailureCategory.Disposed, category);
    }

    [Fact]
    public async Task InvokeAsyncOnUiThread_generic_disposed_returns_disposed_error()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);
        core.Dispose();

        var genericMethod = typeof(WebViewCore)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(m => m.Name == "InvokeAsyncOnUiThread"
                         && m.IsGenericMethodDefinition
                         && m.GetParameters().Length == 1);
        var closedMethod = genericMethod.MakeGenericMethod(typeof(int));
        var task = Assert.IsType<Task<int>>(closedMethod.Invoke(core, [(Func<Task<int>>)(() => Task.FromResult(7))]));

        var ex = await Assert.ThrowsAsync<ObjectDisposedException>(async () => await task);
        Assert.True(WebViewOperationFailure.TryGetCategory(ex, out var category));
        Assert.Equal(WebViewOperationFailureCategory.Disposed, category);
    }

    [Fact]
    public async Task StartNavigationCoreAsync_wrapper_overload_completes_when_adapter_finishes()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);
        var requestUri = new Uri("https://hotspot.test/wrapper");

        var method = RequireInstanceMethod(
            nameof(StartNavigationCoreAsync_wrapper_overload_completes_when_adapter_finishes),
            "StartNavigationCoreAsync",
            typeof(Uri),
            typeof(Func<Guid, Task>));
        var task = Assert.IsAssignableFrom<Task>(method.Invoke(core, [requestUri, (Func<Guid, Task>)(id =>
        {
            adapter.RaiseNavigationCompleted(id, requestUri, NavigationCompletedStatus.Success);
            return Task.CompletedTask;
        })]));

        await task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public void EnableSpaHosting_registers_custom_scheme_when_adapter_supports_it()
    {
        var adapter = MockWebViewAdapter.CreateWithCustomSchemes();
        using var core = new WebViewCore(adapter, _dispatcher);

        core.EnableSpaHosting(new SpaHostingOptions
        {
            EmbeddedResourcePrefix = "TestResources",
            ResourceAssembly = typeof(SpaHostingTests).Assembly
        });

        Assert.Equal(1, adapter.RegisterCallCount);
        Assert.NotNull(adapter.RegisteredSchemes);
        Assert.Single(adapter.RegisteredSchemes!);
    }

    [Fact]
    public async Task NavigationCompleted_is_ignored_when_adapter_is_destroyed()
    {
        var adapter = MockWebViewAdapter.Create();
        var core = new WebViewCore(adapter, _dispatcher);

        NavigationCompletedEventArgs? completed = null;
        core.NavigationCompleted += (_, e) => completed = e;

        var requestUri = new Uri("https://hotspot.test/navigation");
        var navTask = core.NavigateAsync(requestUri);
        var navId = adapter.LastNavigationId;
        Assert.NotNull(navId);

        MarkAdapterDestroyed(core);
        adapter.RaiseNavigationCompleted(navId!.Value, requestUri, NavigationCompletedStatus.Success);

        Assert.Null(completed);
        Assert.False(navTask.IsCompleted);

        core.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => navTask);
    }

    [Fact]
    public void NewWindowRequested_is_ignored_when_adapter_is_destroyed()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);

        NewWindowRequestedEventArgs? args = null;
        core.NewWindowRequested += (_, e) => args = e;

        MarkAdapterDestroyed(core);
        adapter.RaiseNewWindowRequested(new Uri("https://hotspot.test/new-window"));

        Assert.Null(args);
    }

    [Fact]
    public void WebMessageReceived_is_ignored_when_adapter_is_destroyed()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);
        core.EnableWebMessageBridge(new WebMessageBridgeOptions
        {
            AllowedOrigins = new HashSet<string> { "*" }
        });

        WebMessageReceivedEventArgs? args = null;
        core.WebMessageReceived += (_, e) => args = e;

        MarkAdapterDestroyed(core);
        adapter.RaiseWebMessage("""{"type":"destroyed"}""", "*", core.ChannelId);

        Assert.Null(args);
    }

    [Fact]
    public void WebResourceRequested_is_ignored_when_adapter_is_destroyed()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);

        WebResourceRequestedEventArgs? args = null;
        core.WebResourceRequested += (_, e) => args = e;

        MarkAdapterDestroyed(core);
        adapter.RaiseWebResourceRequested(new WebResourceRequestedEventArgs(
            new Uri("https://hotspot.test/resource"),
            "GET"));

        Assert.Null(args);
    }

    [Fact]
    public void EnvironmentRequested_is_ignored_when_adapter_is_destroyed()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);

        EnvironmentRequestedEventArgs? args = null;
        core.EnvironmentRequested += (_, e) => args = e;

        MarkAdapterDestroyed(core);
        adapter.RaiseEnvironmentRequested();

        Assert.Null(args);
    }

    [Fact]
    public void WebMessageReceived_drops_message_when_policy_is_null()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);
        core.EnableWebMessageBridge(new WebMessageBridgeOptions
        {
            AllowedOrigins = new HashSet<string> { "*" }
        });

        SetPrivateField(core, "_webMessagePolicy", null);

        WebMessageReceivedEventArgs? args = null;
        core.WebMessageReceived += (_, e) => args = e;

        adapter.RaiseWebMessage("""{"type":"policy-null"}""", "*", core.ChannelId);

        Assert.Null(args);
    }

    [Fact]
    public void OnSpaWebResourceRequested_without_service_is_noop()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);

        var method = RequireInstanceMethod(
            nameof(OnSpaWebResourceRequested_without_service_is_noop),
            "OnSpaWebResourceRequested",
            typeof(object),
            typeof(WebResourceRequestedEventArgs));
        var args = new WebResourceRequestedEventArgs(new Uri("app://localhost/no-service"), "GET");

        _ = method.Invoke(core, [this, args]);

        Assert.False(args.Handled);
    }

    private static MethodInfo RequireInstanceMethod(
        string testName,
        string methodName,
        params Type[] parameterTypes)
    {
        var method = typeof(WebViewCore).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: parameterTypes,
            modifiers: null);
        Assert.True(method is not null, $"{testName}: method not found -> {methodName}");
        return method!;
    }

    private static void SetPrivateField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.True(field is not null, $"field not found: {fieldName}");
        field!.SetValue(instance, value);
    }

    private static void MarkAdapterDestroyed(WebViewCore core)
    {
        var method = RequireInstanceMethod(
            nameof(MarkAdapterDestroyed),
            "RaiseAdapterDestroyedOnce");
        _ = method.Invoke(core, []);
    }
}
