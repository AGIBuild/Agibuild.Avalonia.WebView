using Agibuild.Avalonia.WebView.Adapters.Abstractions;
using Agibuild.Avalonia.WebView.Testing;
using Avalonia.Platform;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public sealed class ContractSemanticsV1AnyThreadAsyncApiTests
{
    [Fact]
    public void Core_async_apis_called_off_thread_execute_adapter_on_ui_thread()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new AnyThreadCaptureAdapter
        {
            CanGoBack = true,
            CanGoForward = true
        };
        using var core = new WebViewCore(adapter, dispatcher);

        var navigateTask = Task.Run(() => core.NavigateAsync(new Uri("https://example.test/nav")));
        PumpUntilCompleted(dispatcher, navigateTask);
        navigateTask.GetAwaiter().GetResult();
        Assert.Equal(dispatcher.UiThreadId, adapter.NavigateThreadId);

        var navigateToStringTask = Task.Run(() => core.NavigateToStringAsync("<html><body>ok</body></html>"));
        PumpUntilCompleted(dispatcher, navigateToStringTask);
        navigateToStringTask.GetAwaiter().GetResult();
        Assert.Equal(dispatcher.UiThreadId, adapter.NavigateToStringThreadId);

        var invokeScriptTask = Task.Run(() => core.InvokeScriptAsync("1+1"));
        PumpUntilCompleted(dispatcher, invokeScriptTask);
        Assert.Equal("ok", invokeScriptTask.GetAwaiter().GetResult());
        Assert.Equal(dispatcher.UiThreadId, adapter.InvokeScriptThreadId);

        var goBackTask = Task.Run(() => core.GoBackAsync());
        PumpUntilCompleted(dispatcher, goBackTask);
        Assert.True(goBackTask.GetAwaiter().GetResult());
        Assert.Equal(dispatcher.UiThreadId, adapter.GoBackThreadId);

        var goForwardTask = Task.Run(() => core.GoForwardAsync());
        PumpUntilCompleted(dispatcher, goForwardTask);
        Assert.True(goForwardTask.GetAwaiter().GetResult());
        Assert.Equal(dispatcher.UiThreadId, adapter.GoForwardThreadId);

        var refreshTask = Task.Run(() => core.RefreshAsync());
        PumpUntilCompleted(dispatcher, refreshTask);
        Assert.True(refreshTask.GetAwaiter().GetResult());
        Assert.Equal(dispatcher.UiThreadId, adapter.RefreshThreadId);

        adapter.AutoCompleteNavigation = false;
        var pendingNavigationTask = Task.Run(() => core.NavigateAsync(new Uri("https://example.test/pending")));
        ThreadingTestHelper.PumpUntil(dispatcher, () => adapter.HasPendingNavigation);

        var stopTask = Task.Run(() => core.StopAsync());
        PumpUntilCompleted(dispatcher, stopTask);
        Assert.True(stopTask.GetAwaiter().GetResult());
        Assert.Equal(dispatcher.UiThreadId, adapter.StopThreadId);

        pendingNavigationTask.GetAwaiter().GetResult();
    }

    [Fact]
    public void Feature_async_apis_called_off_thread_execute_adapter_on_ui_thread()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new AnyThreadCaptureAdapter();
        using var core = new WebViewCore(adapter, dispatcher);

        var openDevToolsTask = Task.Run(() => core.OpenDevToolsAsync());
        PumpUntilCompleted(dispatcher, openDevToolsTask);
        openDevToolsTask.GetAwaiter().GetResult();
        Assert.Equal(dispatcher.UiThreadId, adapter.OpenDevToolsThreadId);

        var closeDevToolsTask = Task.Run(() => core.CloseDevToolsAsync());
        PumpUntilCompleted(dispatcher, closeDevToolsTask);
        closeDevToolsTask.GetAwaiter().GetResult();
        Assert.Equal(dispatcher.UiThreadId, adapter.CloseDevToolsThreadId);

        var isDevToolsOpenTask = Task.Run(() => core.IsDevToolsOpenAsync());
        PumpUntilCompleted(dispatcher, isDevToolsOpenTask);
        Assert.False(isDevToolsOpenTask.GetAwaiter().GetResult());
        Assert.Equal(dispatcher.UiThreadId, adapter.IsDevToolsOpenThreadId);

        var screenshotTask = Task.Run(() => core.CaptureScreenshotAsync());
        PumpUntilCompleted(dispatcher, screenshotTask);
        Assert.NotEmpty(screenshotTask.GetAwaiter().GetResult());
        Assert.Equal(dispatcher.UiThreadId, adapter.CaptureScreenshotThreadId);

        var printTask = Task.Run(() => core.PrintToPdfAsync(new PdfPrintOptions()));
        PumpUntilCompleted(dispatcher, printTask);
        Assert.NotEmpty(printTask.GetAwaiter().GetResult());
        Assert.Equal(dispatcher.UiThreadId, adapter.PrintToPdfThreadId);

        var getZoomTask = Task.Run(() => core.GetZoomFactorAsync());
        PumpUntilCompleted(dispatcher, getZoomTask);
        Assert.Equal(1.0, getZoomTask.GetAwaiter().GetResult());
        Assert.Equal(dispatcher.UiThreadId, adapter.GetZoomThreadId);

        var setZoomTask = Task.Run(() => core.SetZoomFactorAsync(1.25));
        PumpUntilCompleted(dispatcher, setZoomTask);
        setZoomTask.GetAwaiter().GetResult();
        Assert.Equal(dispatcher.UiThreadId, adapter.SetZoomThreadId);

        var findTask = Task.Run(() => core.FindInPageAsync("hello"));
        PumpUntilCompleted(dispatcher, findTask);
        Assert.Equal(3, findTask.GetAwaiter().GetResult().TotalMatches);
        Assert.Equal(dispatcher.UiThreadId, adapter.FindThreadId);

        var stopFindTask = Task.Run(() => core.StopFindInPageAsync(clearHighlights: false));
        PumpUntilCompleted(dispatcher, stopFindTask);
        stopFindTask.GetAwaiter().GetResult();
        Assert.Equal(dispatcher.UiThreadId, adapter.StopFindThreadId);

        var addPreloadTask = Task.Run(() => core.AddPreloadScriptAsync("console.log('x')"));
        PumpUntilCompleted(dispatcher, addPreloadTask);
        var preloadId = addPreloadTask.GetAwaiter().GetResult();
        Assert.Equal(dispatcher.UiThreadId, adapter.AddPreloadThreadId);

        var removePreloadTask = Task.Run(() => core.RemovePreloadScriptAsync(preloadId));
        PumpUntilCompleted(dispatcher, removePreloadTask);
        removePreloadTask.GetAwaiter().GetResult();
        Assert.Equal(dispatcher.UiThreadId, adapter.RemovePreloadThreadId);
    }

    [Fact]
    public void Manager_async_apis_called_off_thread_execute_adapter_on_ui_thread()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new AnyThreadCaptureAdapter();
        using var core = new WebViewCore(adapter, dispatcher);

        var commandManager = core.TryGetCommandManager();
        Assert.NotNull(commandManager);

        var cookieManager = core.TryGetCookieManager();
        Assert.NotNull(cookieManager);

        var copyTask = Task.Run(() => commandManager!.CopyAsync());
        PumpUntilCompleted(dispatcher, copyTask);
        copyTask.GetAwaiter().GetResult();
        Assert.Equal(dispatcher.UiThreadId, adapter.LastCommandThreadId);

        var setCookieTask = Task.Run(() => cookieManager!.SetCookieAsync(new WebViewCookie("k", "v", ".example.test", "/", null, false, false)));
        PumpUntilCompleted(dispatcher, setCookieTask);
        setCookieTask.GetAwaiter().GetResult();
        Assert.Equal(dispatcher.UiThreadId, adapter.SetCookieThreadId);

        var getCookiesTask = Task.Run(() => cookieManager!.GetCookiesAsync(new Uri("https://example.test/")));
        PumpUntilCompleted(dispatcher, getCookiesTask);
        Assert.Single(getCookiesTask.GetAwaiter().GetResult());
        Assert.Equal(dispatcher.UiThreadId, adapter.GetCookiesThreadId);

        var deleteCookieTask = Task.Run(() => cookieManager!.DeleteCookieAsync(new WebViewCookie("k", "v", ".example.test", "/", null, false, false)));
        PumpUntilCompleted(dispatcher, deleteCookieTask);
        deleteCookieTask.GetAwaiter().GetResult();
        Assert.Equal(dispatcher.UiThreadId, adapter.DeleteCookieThreadId);

        var clearCookiesTask = Task.Run(() => cookieManager!.ClearAllCookiesAsync());
        PumpUntilCompleted(dispatcher, clearCookiesTask);
        clearCookiesTask.GetAwaiter().GetResult();
        Assert.Equal(dispatcher.UiThreadId, adapter.ClearCookiesThreadId);
    }

    private static void PumpUntilCompleted(TestDispatcher dispatcher, Task task)
    {
        ThreadingTestHelper.PumpUntil(dispatcher, () => task.IsCompleted, TimeSpan.FromSeconds(10));
        dispatcher.RunAll();
    }

    private sealed class AnyThreadCaptureAdapter :
        IWebViewAdapter,
        IDevToolsAdapter,
        IScreenshotAdapter,
        IPrintAdapter,
        IZoomAdapter,
        IFindInPageAdapter,
        IPreloadScriptAdapter,
        ICommandAdapter,
        ICookieAdapter
    {
        private bool _initialized;
        private bool _attached;
        private bool _detached;
        private readonly Dictionary<string, WebViewCookie> _cookies = new();
        private readonly Dictionary<string, string> _preloadScripts = new();
        private int _preloadIdCounter;
        private Guid? _pendingNavigationId;
        private Uri? _pendingNavigationUri;
        private double _zoomFactor = 1.0;

        public bool CanGoBack { get; set; }
        public bool CanGoForward { get; set; }
        public bool AutoCompleteNavigation { get; set; } = true;
        public bool HasPendingNavigation => _pendingNavigationId.HasValue;

        public int? NavigateThreadId { get; private set; }
        public int? NavigateToStringThreadId { get; private set; }
        public int? InvokeScriptThreadId { get; private set; }
        public int? GoBackThreadId { get; private set; }
        public int? GoForwardThreadId { get; private set; }
        public int? RefreshThreadId { get; private set; }
        public int? StopThreadId { get; private set; }
        public int? OpenDevToolsThreadId { get; private set; }
        public int? CloseDevToolsThreadId { get; private set; }
        public int? IsDevToolsOpenThreadId { get; private set; }
        public int? CaptureScreenshotThreadId { get; private set; }
        public int? PrintToPdfThreadId { get; private set; }
        public int? GetZoomThreadId { get; private set; }
        public int? SetZoomThreadId { get; private set; }
        public int? FindThreadId { get; private set; }
        public int? StopFindThreadId { get; private set; }
        public int? AddPreloadThreadId { get; private set; }
        public int? RemovePreloadThreadId { get; private set; }
        public int? LastCommandThreadId { get; private set; }
        public int? GetCookiesThreadId { get; private set; }
        public int? SetCookieThreadId { get; private set; }
        public int? DeleteCookieThreadId { get; private set; }
        public int? ClearCookiesThreadId { get; private set; }

        public event EventHandler<NavigationCompletedEventArgs>? NavigationCompleted;
        public event EventHandler<NewWindowRequestedEventArgs>? NewWindowRequested;
        public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
        public event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested;
        public event EventHandler<EnvironmentRequestedEventArgs>? EnvironmentRequested;
        public event EventHandler<double>? ZoomFactorChanged;

        public void Initialize(IWebViewAdapterHost host)
        {
            _initialized = true;
        }

        public void Attach(IPlatformHandle parentHandle)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Adapter must be initialized before attach.");
            }

            _attached = true;
        }

        public void Detach()
        {
            _detached = true;
        }

        public Task NavigateAsync(Guid navigationId, Uri uri)
        {
            NavigateThreadId = Environment.CurrentManagedThreadId;
            _pendingNavigationId = navigationId;
            _pendingNavigationUri = uri;
            if (AutoCompleteNavigation)
            {
                CompletePendingNavigation(NavigationCompletedStatus.Success, error: null);
            }

            return Task.CompletedTask;
        }

        public Task NavigateToStringAsync(Guid navigationId, string html)
            => NavigateToStringAsync(navigationId, html, baseUrl: null);

        public Task NavigateToStringAsync(Guid navigationId, string html, Uri? baseUrl)
        {
            NavigateToStringThreadId = Environment.CurrentManagedThreadId;
            _pendingNavigationId = navigationId;
            _pendingNavigationUri = baseUrl ?? new Uri("about:blank");
            if (AutoCompleteNavigation)
            {
                CompletePendingNavigation(NavigationCompletedStatus.Success, error: null);
            }

            return Task.CompletedTask;
        }

        public Task<string?> InvokeScriptAsync(string script)
        {
            InvokeScriptThreadId = Environment.CurrentManagedThreadId;
            return Task.FromResult<string?>("ok");
        }

        public bool GoBack(Guid navigationId)
        {
            GoBackThreadId = Environment.CurrentManagedThreadId;
            return true;
        }

        public bool GoForward(Guid navigationId)
        {
            GoForwardThreadId = Environment.CurrentManagedThreadId;
            return true;
        }

        public bool Refresh(Guid navigationId)
        {
            RefreshThreadId = Environment.CurrentManagedThreadId;
            return true;
        }

        public bool Stop()
        {
            StopThreadId = Environment.CurrentManagedThreadId;
            if (_pendingNavigationId.HasValue && _pendingNavigationUri is not null)
            {
                CompletePendingNavigation(NavigationCompletedStatus.Canceled, error: null);
            }

            return true;
        }

        public void OpenDevTools()
        {
            OpenDevToolsThreadId = Environment.CurrentManagedThreadId;
        }

        public void CloseDevTools()
        {
            CloseDevToolsThreadId = Environment.CurrentManagedThreadId;
        }

        public bool IsDevToolsOpen
        {
            get
            {
                IsDevToolsOpenThreadId = Environment.CurrentManagedThreadId;
                return false;
            }
        }

        public Task<byte[]> CaptureScreenshotAsync()
        {
            CaptureScreenshotThreadId = Environment.CurrentManagedThreadId;
            return Task.FromResult(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        }

        public Task<byte[]> PrintToPdfAsync(PdfPrintOptions? options)
        {
            PrintToPdfThreadId = Environment.CurrentManagedThreadId;
            return Task.FromResult(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        }

        public double ZoomFactor
        {
            get
            {
                GetZoomThreadId = Environment.CurrentManagedThreadId;
                return _zoomFactor;
            }
            set
            {
                SetZoomThreadId = Environment.CurrentManagedThreadId;
                _zoomFactor = value;
                ZoomFactorChanged?.Invoke(this, value);
            }
        }

        public Task<FindInPageResult> FindAsync(string text, FindInPageOptions? options)
        {
            FindThreadId = Environment.CurrentManagedThreadId;
            return Task.FromResult(new FindInPageResult
            {
                ActiveMatchIndex = 0,
                TotalMatches = 3
            });
        }

        public void StopFind(bool clearHighlights = true)
        {
            StopFindThreadId = Environment.CurrentManagedThreadId;
        }

        public string AddPreloadScript(string javaScript)
        {
            AddPreloadThreadId = Environment.CurrentManagedThreadId;
            var id = $"script_{Interlocked.Increment(ref _preloadIdCounter)}";
            _preloadScripts[id] = javaScript;
            return id;
        }

        public void RemovePreloadScript(string scriptId)
        {
            RemovePreloadThreadId = Environment.CurrentManagedThreadId;
            _preloadScripts.Remove(scriptId);
        }

        public void ExecuteCommand(WebViewCommand command)
        {
            LastCommandThreadId = Environment.CurrentManagedThreadId;
        }

        public Task<IReadOnlyList<WebViewCookie>> GetCookiesAsync(Uri uri)
        {
            GetCookiesThreadId = Environment.CurrentManagedThreadId;
            var host = uri.Host;
            var list = _cookies.Values
                .Where(c => c.Domain.EndsWith(host, StringComparison.OrdinalIgnoreCase) || host.EndsWith(c.Domain, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return Task.FromResult<IReadOnlyList<WebViewCookie>>(list);
        }

        public Task SetCookieAsync(WebViewCookie cookie)
        {
            SetCookieThreadId = Environment.CurrentManagedThreadId;
            _cookies[$"{cookie.Name}|{cookie.Domain}|{cookie.Path}"] = cookie;
            return Task.CompletedTask;
        }

        public Task DeleteCookieAsync(WebViewCookie cookie)
        {
            DeleteCookieThreadId = Environment.CurrentManagedThreadId;
            _cookies.Remove($"{cookie.Name}|{cookie.Domain}|{cookie.Path}");
            return Task.CompletedTask;
        }

        public Task ClearAllCookiesAsync()
        {
            ClearCookiesThreadId = Environment.CurrentManagedThreadId;
            _cookies.Clear();
            return Task.CompletedTask;
        }

        private void CompletePendingNavigation(NavigationCompletedStatus status, Exception? error)
        {
            if (_pendingNavigationId is null || _pendingNavigationUri is null)
            {
                return;
            }

            NavigationCompleted?.Invoke(
                this,
                new NavigationCompletedEventArgs(
                    _pendingNavigationId.Value,
                    _pendingNavigationUri,
                    status,
                    error));

            _pendingNavigationId = null;
            _pendingNavigationUri = null;
        }
    }
}
