using System.Reflection;
using System.Text.Json;
using Agibuild.Fulora;
using Agibuild.Fulora.Adapters.Abstractions;
using Agibuild.Fulora.Shell;
using Agibuild.Fulora.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

// Shared fixtures and reflection seams for the Round-3 branch-coverage suite.
// Lives in its own partial-class file so per-SUT files (WebViewCore / Shell / Rpc / ...) stay focused.
public sealed partial class BranchCoverageRound3Tests
{
    private static FullWebView CreateFullWebView() => new();

    // Reflection shim: previously these tests poked _disposed / _adapterDestroyed fields directly
    // on WebViewCore. After the lifecycle flags moved into WebViewLifecycleStateMachine (owned by
    // WebViewCoreContext), the simulation hops through _context.Lifecycle. Kept here rather than
    // in a shared helper because no other test needs this seam.
    private static void SetLifecycleFlag(WebViewCore core, string flagFieldName, bool value)
    {
        var contextField = typeof(WebViewCore).GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(contextField);
        var context = contextField!.GetValue(core);
        Assert.NotNull(context);

        var lifecycleProp = context!.GetType().GetProperty("Lifecycle", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(lifecycleProp);
        var machine = lifecycleProp!.GetValue(context);
        Assert.NotNull(machine);

        var innerField = machine!.GetType().GetField(flagFieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(innerField);
        innerField!.SetValue(machine, value);
    }

    private sealed class FullWebView : IWebView
    {
        public Uri Source { get; set; } = new("about:blank");
        public bool CanGoBack => false;
        public bool CanGoForward => false;
        public bool IsLoading => false;
        public Guid ChannelId { get; } = Guid.NewGuid();
        public ICommandManager? CommandManager { get; init; }
        public bool IsDisposed { get; private set; }
        private bool _isDevToolsOpen;

        public event EventHandler<NavigationStartingEventArgs>? NavigationStarted { add { } remove { } }
        public event EventHandler<NavigationCompletedEventArgs>? NavigationCompleted { add { } remove { } }
        public event EventHandler<NewWindowRequestedEventArgs>? NewWindowRequested { add { } remove { } }
        public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived { add { } remove { } }
        public event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested { add { } remove { } }
        public event EventHandler<EnvironmentRequestedEventArgs>? EnvironmentRequested { add { } remove { } }
        public event EventHandler<DownloadRequestedEventArgs>? DownloadRequested { add { } remove { } }
        public event EventHandler<PermissionRequestedEventArgs>? PermissionRequested { add { } remove { } }
        public event EventHandler<AdapterCreatedEventArgs>? AdapterCreated { add { } remove { } }
        public event EventHandler? AdapterDestroyed { add { } remove { } }
        public event EventHandler<ContextMenuRequestedEventArgs>? ContextMenuRequested { add { } remove { } }

        public Task NavigateAsync(Uri uri) => Task.CompletedTask;
        public Task NavigateToStringAsync(string html) => Task.CompletedTask;
        public Task NavigateToStringAsync(string html, Uri? baseUrl) => Task.CompletedTask;
        public Task<string?> InvokeScriptAsync(string script) => Task.FromResult<string?>(null);
        public Task<bool> GoBackAsync() => Task.FromResult(false);
        public Task<bool> GoForwardAsync() => Task.FromResult(false);
        public Task<bool> RefreshAsync() => Task.FromResult(false);
        public Task<bool> StopAsync() => Task.FromResult(false);
        public ICookieManager? TryGetCookieManager() => null;
        public ICommandManager? TryGetCommandManager() => CommandManager;
        public Task<INativeHandle?> TryGetWebViewHandleAsync() => Task.FromResult<INativeHandle?>(null);
        public IWebViewRpcService? Rpc => null;
        public IBridgeService Bridge => throw new NotSupportedException();
        public IBridgeTracer? BridgeTracer { get; set; }
        public Task<byte[]> CaptureScreenshotAsync() => Task.FromException<byte[]>(new NotSupportedException());
        public Task<byte[]> PrintToPdfAsync(PdfPrintOptions? options = null) => Task.FromException<byte[]>(new NotSupportedException());
        public Task<double> GetZoomFactorAsync() => Task.FromResult(1.0);
        public Task SetZoomFactorAsync(double zoomFactor) => Task.CompletedTask;
        public Task<FindInPageEventArgs> FindInPageAsync(string text, FindInPageOptions? options = null) => Task.FromException<FindInPageEventArgs>(new NotSupportedException());
        public Task StopFindInPageAsync(bool clearHighlights = true) => Task.CompletedTask;
        public Task<string> AddPreloadScriptAsync(string javaScript) => Task.FromException<string>(new NotSupportedException());
        public Task RemovePreloadScriptAsync(string scriptId) => Task.FromException(new NotSupportedException());
        public Task OpenDevToolsAsync() { _isDevToolsOpen = true; return Task.CompletedTask; }
        public Task CloseDevToolsAsync() { _isDevToolsOpen = false; return Task.CompletedTask; }
        public Task<bool> IsDevToolsOpenAsync() => Task.FromResult(_isDevToolsOpen);

        public void Dispose()
        {
            IsDisposed = true;
            GC.SuppressFinalize(this);
        }
    }

    private sealed class MinimalHostCapabilityProvider : IWebViewHostCapabilityProvider
    {
        public string? ReadClipboardText() => null;
        public void WriteClipboardText(string text) { }
        public WebViewFileDialogResult ShowOpenFileDialog(WebViewOpenFileDialogRequest request) => new() { IsCanceled = true };
        public WebViewFileDialogResult ShowSaveFileDialog(WebViewSaveFileDialogRequest request) => new() { IsCanceled = true };
        public void OpenExternal(Uri uri) { }
        public void ShowNotification(WebViewNotificationRequest request) { }
        public void ApplyMenuModel(WebViewMenuModelRequest request) { }
        public void UpdateTrayState(WebViewTrayStateRequest request) { }
        public void ExecuteSystemAction(WebViewSystemActionRequest request) { }
    }

    private sealed class NullReasonDenyPolicy : IWebViewHostCapabilityPolicy
    {
        public WebViewHostCapabilityDecision Evaluate(in WebViewHostCapabilityRequestContext context)
            => WebViewHostCapabilityDecision.Deny(null);
    }

    [JsImport]
    public interface INoArgImport
    {
        Task DoAsync();
    }

    public interface ISyncReturnImport
    {
        string GetValue();
    }

    private sealed class LambdaRpcService : IWebViewRpcService
    {
        private readonly Func<string, object?, Task> _invoker;

        public LambdaRpcService(Func<string, object?, Task> invoker) => _invoker = invoker;

        public void Handle(string method, Func<JsonElement?, Task<object?>> handler) { }
        public void Handle(string method, Func<JsonElement?, object?> handler) { }
        public void UnregisterHandler(string method) { }
        public Task<JsonElement> InvokeAsync(string method, object? args = null)
        {
            _invoker(method, args);
            return Task.FromResult(default(JsonElement));
        }
        public Task<T?> InvokeAsync<T>(string method, object? args = null) => Task.FromResult<T?>(default);
        public bool TryProcessMessage(string body) => false;
        public void RegisterEnumerator(string token, Func<Task<(object? Value, bool Finished)>> moveNext, Func<Task> dispose) { }
    }

    private sealed class FakeOffThreadDispatcher : IWebViewDispatcher
    {
        public bool InvokeAsyncCalled { get; private set; }
        public bool CheckAccess() => false;
        public Task InvokeAsync(Action action) { InvokeAsyncCalled = true; return Task.CompletedTask; }
        public Task<T> InvokeAsync<T>(Func<T> func) => Task.FromResult(func());
        public Task InvokeAsync(Func<Task> func) { InvokeAsyncCalled = true; return Task.CompletedTask; }
        public Task<T> InvokeAsync<T>(Func<Task<T>> func) => func();
    }
}
