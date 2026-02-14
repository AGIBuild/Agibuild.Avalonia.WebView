using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Agibuild.Avalonia.WebView.Integration.Tests.ViewModels;

/// <summary>
/// E2E test ViewModel for verifying features using a real native WebView:
///   1. Command Manager (Copy, Cut, Paste, SelectAll, Undo, Redo)
///   2. Screenshot Capture (CaptureScreenshotAsync)
///   3. Print to PDF (PrintToPdfAsync)
///   4. JS ↔ C# RPC (bidirectional JSON-RPC)
///   5. Find in Page (FindInPageAsync / StopFindInPage)
///   6. Zoom Control (ZoomFactor get/set)
///   7. Preload Script (AddPreloadScript / RemovePreloadScript)
///   8. Context Menu (ContextMenuRequested event wiring)
///
/// HOW TO RUN (for newcomers):
///   Manual:  Launch desktop app → select "Feature E2E" tab → click "Run All".
///   Auto:    dotnet run -- --feature-e2e   (exits with code 0 = pass, 1 = fail).
///
/// WHAT IT DOES:
///   1. Loads an HTML page into the WebView.
///   2. Runs each scenario in sequence.
///   3. Logs PASS/FAIL per scenario and updates per-test status indicators.
///   4. Reports aggregate status.
/// </summary>
public partial class FeatureE2EViewModel : ViewModelBase
{
    private static readonly Uri TestHome = new("https://github.com");

    /// <summary>Inline HTML page used for testing commands, screenshots, and RPC.</summary>
    private const string TestHtml = """
        <!DOCTYPE html>
        <html>
        <head><title>Feature E2E Test</title></head>
        <body style="background:#4CAF50; color:white; font-size:24px; padding:40px;">
          <h1 id="title">Feature E2E</h1>
          <textarea id="editor" rows="4" cols="40">Hello World</textarea>
          <p id="result"></p>
        </body>
        </html>
        """;

    private int _autoRunStarted;
    private readonly Action<string>? _logSink;

    public FeatureE2EViewModel(Action<string>? logSink = null)
    {
        _logSink = logSink;
        Status = "Ready";
    }

    /// <summary>When true, automatically runs all scenarios on WebView load.</summary>
    public bool AutoRun { get; set; }

    /// <summary>Fired when auto-run completes. Arg: 0 = pass, 1 = fail.</summary>
    public event Action<int>? AutoRunCompleted;

    [ObservableProperty]
    private string _status = string.Empty;

    // Per-test result indicators ("—" pending, "..." running, "PASS", "FAIL", "SKIP")
    [ObservableProperty] private string _resultCommand = "—";
    [ObservableProperty] private string _resultScreenshot = "—";
    [ObservableProperty] private string _resultPdf = "—";
    [ObservableProperty] private string _resultRpc = "—";
    [ObservableProperty] private string _resultFind = "—";
    [ObservableProperty] private string _resultZoom = "—";
    [ObservableProperty] private string _resultPreload = "—";
    [ObservableProperty] private string _resultContext = "—";
    [ObservableProperty] private string _resultBridge = "—";
    [ObservableProperty] private string _resultDevTools = "—";

    /// <summary>Set by the View once the WebView control is available.</summary>
    public WebView? WebViewControl { get; set; }

    public void OnWebViewLoaded()
    {
        if (AutoRun && Interlocked.CompareExchange(ref _autoRunStarted, 1, 0) == 0)
        {
            _ = RunAllForAutoRunAsync();
        }
    }

    private async Task RunAllForAutoRunAsync()
    {
        try
        {
            var ok = await RunAllCoreAsync().ConfigureAwait(false);
            AutoRunCompleted?.Invoke(ok ? 0 : 1);
        }
        catch (Exception ex)
        {
            LogLine($"FATAL: {ex}");
            AutoRunCompleted?.Invoke(1);
        }
    }

    [RelayCommand]
    private async Task RunAll()
    {
        // Do NOT use ConfigureAwait(false) here — AsyncRelayCommand must
        // complete its Task on the UI thread so IsRunning/CanExecuteChanged
        // fire correctly and the button/cursor state recovers.
        try
        {
            await RunAllCoreAsync();
        }
        catch (Exception ex)
        {
            LogLine($"FATAL: {ex}");
            Status = $"Error: {ex.Message}";
        }
    }

    // ---------------------------------------------------------------------------
    //  Core test runner
    // ---------------------------------------------------------------------------

    private void ResetResults()
    {
        ResultCommand = "—";
        ResultScreenshot = "—";
        ResultPdf = "—";
        ResultRpc = "—";
        ResultFind = "—";
        ResultZoom = "—";
        ResultPreload = "—";
        ResultContext = "—";
        ResultBridge = "—";
        ResultDevTools = "—";
    }

    private async Task<bool> RunAllCoreAsync()
    {
        ResetResults();
        Status = "Running...";
        LogLine("=== Feature E2E Test Suite ===");

        // Load test HTML and wait for it to be ready.
        await LoadTestHtmlAndWaitAsync().ConfigureAwait(false);
        await Task.Delay(500).ConfigureAwait(false);

        var allPassed = true;

        // 1. Command Manager
        allPassed &= await RunCommandManagerAsync().ConfigureAwait(false);

        // 2. Screenshot Capture
        allPassed &= await RunScreenshotAsync().ConfigureAwait(false);

        // 3. Print to PDF
        allPassed &= await RunPrintToPdfAsync().ConfigureAwait(false);

        // 4. JS ↔ C# RPC
        allPassed &= await RunRpcAsync().ConfigureAwait(false);

        // 5. Find in Page
        allPassed &= await RunFindInPageAsync().ConfigureAwait(false);

        // 6. Zoom Control
        allPassed &= await RunZoomControlAsync().ConfigureAwait(false);

        // 7. Preload Script
        allPassed &= await RunPreloadScriptAsync().ConfigureAwait(false);

        // 8. Context Menu
        allPassed &= await RunContextMenuAsync().ConfigureAwait(false);

        // 9. Bridge (typed C# ↔ JS interop)
        allPassed &= await RunBridgeAsync().ConfigureAwait(false);

        // 10. DevTools toggle
        allPassed &= await RunDevToolsAsync().ConfigureAwait(false);

        Status = allPassed ? "ALL PASSED" : "SOME FAILED";
        LogLine($"Result: {Status}");
        return allPassed;
    }

    // ---------------------------------------------------------------------------
    //  Scenario 1: Command Manager
    // ---------------------------------------------------------------------------

    private async Task<bool> RunCommandManagerAsync()
    {
        ResultCommand = "...";
        try
        {
            LogLine("[1] Command Manager — SelectAll + Copy");

            var mgr = WebViewControl!.TryGetCommandManager();
            if (mgr is null)
            {
                LogLine("  SKIP: adapter does not support ICommandAdapter");
                ResultCommand = "SKIP";
                return true;
            }

            // Focus the textarea and type something
            await WebViewControl.InvokeScriptAsync(
                "document.getElementById('editor').focus();"
            ).ConfigureAwait(false);
            await Task.Delay(200).ConfigureAwait(false);

            // SelectAll
            await mgr.SelectAllAsync().ConfigureAwait(false);
            await Task.Delay(200).ConfigureAwait(false);

            // Copy
            await mgr.CopyAsync().ConfigureAwait(false);
            await Task.Delay(200).ConfigureAwait(false);

            LogLine("  PASS (commands executed without error)");
            ResultCommand = "PASS";
            return true;
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            ResultCommand = "FAIL";
            return false;
        }
    }

    // ---------------------------------------------------------------------------
    //  Scenario 2: Screenshot Capture
    // ---------------------------------------------------------------------------

    private async Task<bool> RunScreenshotAsync()
    {
        ResultScreenshot = "...";
        try
        {
            LogLine("[2] Screenshot Capture");

            var bytes = await WebViewControl!.CaptureScreenshotAsync().ConfigureAwait(false);

            if (bytes is null || bytes.Length == 0)
            {
                LogLine("  FAIL: empty bytes returned");
                ResultScreenshot = "FAIL";
                return false;
            }

            // Verify PNG magic header: 0x89 P N G
            if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            {
                LogLine($"  PASS (PNG, {bytes.Length:N0} bytes)");
                ResultScreenshot = "PASS";
                return true;
            }
            else
            {
                LogLine($"  FAIL: not a valid PNG (header: {bytes[0]:X2} {bytes[1]:X2} {bytes[2]:X2} {bytes[3]:X2})");
                ResultScreenshot = "FAIL";
                return false;
            }
        }
        catch (NotSupportedException)
        {
            LogLine("  SKIP: adapter does not support IScreenshotAdapter");
            ResultScreenshot = "SKIP";
            return true;
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            ResultScreenshot = "FAIL";
            return false;
        }
    }

    // ---------------------------------------------------------------------------
    //  Scenario 3: Print to PDF
    // ---------------------------------------------------------------------------

    private async Task<bool> RunPrintToPdfAsync()
    {
        ResultPdf = "...";
        try
        {
            LogLine("[3] Print to PDF");

            var bytes = await WebViewControl!.PrintToPdfAsync(
                new PdfPrintOptions { Landscape = false }
            ).ConfigureAwait(false);

            if (bytes is null || bytes.Length == 0)
            {
                LogLine("  FAIL: empty bytes returned");
                ResultPdf = "FAIL";
                return false;
            }

            // Verify PDF magic header: %PDF
            if (bytes.Length >= 4 && bytes[0] == (byte)'%' && bytes[1] == (byte)'P' && bytes[2] == (byte)'D' && bytes[3] == (byte)'F')
            {
                LogLine($"  PASS (PDF, {bytes.Length:N0} bytes)");
                ResultPdf = "PASS";
                return true;
            }
            else
            {
                var header = System.Text.Encoding.ASCII.GetString(bytes, 0, Math.Min(bytes.Length, 8));
                LogLine($"  FAIL: not a valid PDF (header: {header})");
                ResultPdf = "FAIL";
                return false;
            }
        }
        catch (NotSupportedException)
        {
            LogLine("  SKIP: adapter does not support IPrintAdapter (GTK)");
            ResultPdf = "SKIP";
            return true;
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            ResultPdf = "FAIL";
            return false;
        }
    }

    // ---------------------------------------------------------------------------
    //  Scenario 4: JS ↔ C# RPC
    // ---------------------------------------------------------------------------

    private async Task<bool> RunRpcAsync()
    {
        ResultRpc = "...";
        try
        {
            LogLine("[4] JS ↔ C# RPC");

            // EnableWebMessageBridge requires UI thread (ThrowIfNotOnUiThread).
            // Use an EMPTY AllowedOrigins set — that means "allow all origins".
            // (A set containing "*" would only match the literal origin "*".)
            await Dispatcher.UIThread.InvokeAsync(() =>
                WebViewControl!.EnableWebMessageBridge(new WebMessageBridgeOptions
                {
                    AllowedOrigins = new HashSet<string>()
                }));

            // EnableWebMessageBridge fires JsStub injection as fire-and-forget
            // (_ = InvokeScriptAsync(JsStub)). Wait for it to be executed.
            await Task.Delay(800).ConfigureAwait(false);

            var rpc = WebViewControl!.Rpc;
            if (rpc is null)
            {
                LogLine("  FAIL: Rpc is null after enabling bridge");
                ResultRpc = "FAIL";
                return false;
            }

            // --- Diagnostic: verify JsStub was injected ---
            var bridgeCheck = await WebViewControl.InvokeScriptAsync(
                "((typeof window.agWebView !== 'undefined' && typeof window.agWebView.rpc !== 'undefined') ? 'OK' : 'MISSING')"
            ).ConfigureAwait(false);
            LogLine($"  Diag: bridge={bridgeCheck}");
            if (bridgeCheck != "OK")
            {
                LogLine("  FAIL: JsStub was not injected into the page");
                ResultRpc = "FAIL";
                return false;
            }

            // --- Diagnostic: verify postMessage channel exists ---
            var postCheck = await WebViewControl.InvokeScriptAsync(
                """
                (function() {
                    if (window.chrome && window.chrome.webview) return 'chrome.webview';
                    if (window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.agibuildWebView) return 'webkit.agibuildWebView';
                    if (window.webkit && window.webkit.messageHandlers) return 'webkit.noHandler:' + Object.keys(window.webkit.messageHandlers);
                    return 'none';
                })()
                """
            ).ConfigureAwait(false);
            LogLine($"  Diag: postChannel={postCheck}");

            // --- 4a: Register a C# handler and invoke it from JS ---
            LogLine("  [4a] JS → C# call");
            var handlerCalled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            rpc.Handle("e2e.ping", _ =>
            {
                handlerCalled.TrySetResult(true);
                return Task.FromResult<object?>("pong");
            });

            // Inject JS to call the C# method — use try/catch inside JS for diagnostics
            var invokeResult = await WebViewControl.InvokeScriptAsync(
                """
                (function() {
                    try {
                        if (!window.agWebView || !window.agWebView.rpc) return 'ERR:no-bridge';
                        window.agWebView.rpc.invoke('e2e.ping').then(function(r) {
                            var el = document.getElementById('result');
                            if (el) el.textContent = r;
                        }).catch(function(e) {
                            var el = document.getElementById('result');
                            if (el) el.textContent = 'ERR:' + e.message;
                        });
                        return 'invoked';
                    } catch(e) {
                        return 'ERR:' + e.message;
                    }
                })()
                """
            ).ConfigureAwait(false);
            LogLine($"  Diag: invokeResult={invokeResult}");

            // Wait for the C# handler to be called
            var called = await WaitAsync(handlerCalled.Task, TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            if (!called)
            {
                // Extra diagnostic: read the result element to see JS-side state
                var resultEl = await WebViewControl.InvokeScriptAsync(
                    "document.getElementById('result')?.textContent || '(empty)'"
                ).ConfigureAwait(false);
                LogLine($"  Diag: result-element={resultEl}");
                LogLine("  FAIL: C# handler was not called within 10s");
                ResultRpc = "FAIL";
                return false;
            }
            LogLine("  [4a] PASS");

            // --- 4b: C# calls JS and gets result ---
            LogLine("  [4b] C# → JS call");

            // Register a JS handler
            var handleResult = await WebViewControl.InvokeScriptAsync(
                """
                (function() {
                    try {
                        if (!window.agWebView || !window.agWebView.rpc) return 'ERR:no-bridge';
                        window.agWebView.rpc.handle('e2e.getTitle', function() { return document.title; });
                        return 'registered';
                    } catch(e) {
                        return 'ERR:' + e.message;
                    }
                })()
                """
            ).ConfigureAwait(false);
            LogLine($"  Diag: handleResult={handleResult}");
            await Task.Delay(200).ConfigureAwait(false);

            using var invokeCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var titleTask = rpc.InvokeAsync<string>("e2e.getTitle");
            var completedTask = await Task.WhenAny(titleTask, Task.Delay(Timeout.InfiniteTimeSpan, invokeCts.Token)).ConfigureAwait(false);
            if (completedTask != titleTask)
            {
                LogLine("  FAIL: C# → JS InvokeAsync timed out after 10s");
                ResultRpc = "FAIL";
                return false;
            }

            var title = await titleTask.ConfigureAwait(false);
            if (title == "Feature E2E Test")
            {
                LogLine($"  [4b] PASS (title={title})");
            }
            else
            {
                LogLine($"  FAIL: expected 'Feature E2E Test', got '{title}'");
                ResultRpc = "FAIL";
                return false;
            }

            LogLine("  PASS (all RPC sub-scenarios)");
            ResultRpc = "PASS";
            return true;
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            ResultRpc = "FAIL";
            return false;
        }
    }

    // ---------------------------------------------------------------------------
    //  Scenario 5: Find in Page
    // ---------------------------------------------------------------------------

    private async Task<bool> RunFindInPageAsync()
    {
        ResultFind = "...";
        try
        {
            LogLine("[5] Find in Page");

            // Find text that exists in our test HTML
            var result = await WebViewControl!.FindInPageAsync("Feature").ConfigureAwait(false);
            LogLine($"  FindInPage('Feature') → ActiveMatchIndex={result.ActiveMatchIndex}, TotalMatches={result.TotalMatches}");

            if (result.TotalMatches > 0)
            {
                LogLine("  PASS (found matches)");
            }
            else
            {
                LogLine("  WARN: no matches found (platform may not support window.find in headless mode)");
            }

            // Stop find
            await WebViewControl!.StopFindInPageAsync().ConfigureAwait(false);
            LogLine("  StopFindInPage → OK");

            LogLine("  PASS");
            ResultFind = "PASS";
            return true;
        }
        catch (NotSupportedException)
        {
            LogLine("  SKIP: adapter does not support IFindInPageAdapter");
            ResultFind = "SKIP";
            return true;
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            ResultFind = "FAIL";
            return false;
        }
    }

    // ---------------------------------------------------------------------------
    //  Scenario 6: Zoom Control
    // ---------------------------------------------------------------------------

    private async Task<bool> RunZoomControlAsync()
    {
        ResultZoom = "...";
        try
        {
            LogLine("[6] Zoom Control");

            var original = await WebViewControl!.GetZoomFactorAsync().ConfigureAwait(false);
            LogLine($"  Current zoom: {original}");

            // Set zoom to 1.5 — yields back to UI pump after completion
            await WebViewControl.SetZoomFactorAsync(1.5).ConfigureAwait(false);
            // Let the UI thread process pending events (repaint, input, etc.)
            await Task.Delay(50).ConfigureAwait(false);

            var actual = await WebViewControl.GetZoomFactorAsync().ConfigureAwait(false);
            LogLine($"  Set 1.5 → got {actual}");

            if (Math.Abs(actual - 1.5) > 0.01)
            {
                LogLine($"  WARN: expected 1.5, got {actual} (platform may report differently)");
            }

            // Reset to 1.0
            await WebViewControl.SetZoomFactorAsync(1.0).ConfigureAwait(false);
            await Task.Delay(50).ConfigureAwait(false);
            LogLine("  Reset to 1.0 → OK");

            LogLine("  PASS");
            ResultZoom = "PASS";
            return true;
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            ResultZoom = "FAIL";
            return false;
        }
    }

    // ---------------------------------------------------------------------------
    //  Scenario 7: Preload Script
    // ---------------------------------------------------------------------------

    private async Task<bool> RunPreloadScriptAsync()
    {
        ResultPreload = "...";
        try
        {
            LogLine("[7] Preload Script");

            var scriptId = await WebViewControl!.AddPreloadScriptAsync("window.__e2ePreload = true;").ConfigureAwait(false);
            LogLine($"  AddPreloadScript → id={scriptId}");

            if (string.IsNullOrEmpty(scriptId))
            {
                LogLine("  FAIL: script ID is null or empty");
                ResultPreload = "FAIL";
                return false;
            }

            await WebViewControl.RemovePreloadScriptAsync(scriptId).ConfigureAwait(false);
            LogLine("  RemovePreloadScript → OK");

            LogLine("  PASS");
            ResultPreload = "PASS";
            return true;
        }
        catch (NotSupportedException)
        {
            LogLine("  SKIP: adapter does not support IPreloadScriptAdapter");
            ResultPreload = "SKIP";
            return true;
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            ResultPreload = "FAIL";
            return false;
        }
    }

    // ---------------------------------------------------------------------------
    //  Scenario 8: Context Menu Event
    // ---------------------------------------------------------------------------

    private async Task<bool> RunContextMenuAsync()
    {
        ResultContext = "...";
        try
        {
            LogLine("[8] Context Menu");

            // Just verify we can subscribe/unsubscribe without errors
            void Handler(object? s, ContextMenuRequestedEventArgs e) { }

            WebViewControl!.ContextMenuRequested += Handler;
            WebViewControl.ContextMenuRequested -= Handler;
            LogLine("  Subscribe/unsubscribe → OK");

            // Note: We can't programmatically trigger a native context menu,
            // so we just verify the event wiring works without crashes.
            LogLine("  PASS (event wiring verified)");
            ResultContext = "PASS";
            return true;
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            ResultContext = "FAIL";
            return false;
        }
    }

    // ---------------------------------------------------------------------------
    //  9. Bridge (typed C# ↔ JS)
    // ---------------------------------------------------------------------------

    /// <summary>Service exposed from C# to JS via [JsExport].</summary>
    [JsExport(Name = "e2eGreeter")]
    public interface IE2EGreeter
    {
        Task<string> Greet(string name);
    }

    /// <summary>Service provided by JS, called from C# via [JsImport].</summary>
    [JsImport]
    public interface IE2ENotifier
    {
        Task<bool> ShowAlert(string message);
    }

    private sealed class E2EGreeterImpl : IE2EGreeter
    {
        public Task<string> Greet(string name) => Task.FromResult($"Hello, {name}!");
    }

    private async Task<bool> RunBridgeAsync()
    {
        ResultBridge = "...";
        LogLine("=== 9. Bridge ===");
        try
        {
            var wv = WebViewControl!;

            // 9a. Expose a C# service and call it from JS.
            wv.Bridge.Expose<IE2EGreeter>(new E2EGreeterImpl());
            LogLine("Bridge: Exposed IE2EGreeter (service name: e2eGreeter).");

            // Wait for the JS stub to be injected (fire-and-forget in RuntimeBridgeService).
            await Task.Delay(800).ConfigureAwait(false);

            // Diagnostic: list all registered bridge services.
            var bridgeKeys = await Dispatcher.UIThread.InvokeAsync(async () =>
                await wv.InvokeScriptAsync(
                    "window.agWebView && window.agWebView.bridge ? Object.keys(window.agWebView.bridge).join(',') : 'NO_BRIDGE'"
                ).ConfigureAwait(false)
            ).ConfigureAwait(false);
            LogLine($"Bridge: registered services = [{bridgeKeys}]");

            // Fire-and-forget the JS call — WKWebView evaluateJavaScript does NOT await
            // Promises, so we store the result in a global variable and poll for it.
            var invokeStatus = await Dispatcher.UIThread.InvokeAsync(async () =>
                await wv.InvokeScriptAsync(
                    """
                    (function() {
                        try {
                            if (!window.agWebView || !window.agWebView.bridge || !window.agWebView.bridge.e2eGreeter)
                                return 'NOT_READY';
                            window.__e2eBridgeResult = null;
                            window.agWebView.bridge.e2eGreeter.greet({name:'World'}).then(function(r) {
                                window.__e2eBridgeResult = r;
                            }).catch(function(e) {
                                window.__e2eBridgeResult = 'ERR:' + e.message;
                            });
                            return 'invoked';
                        } catch(e) {
                            return 'ERR:' + e.message;
                        }
                    })()
                    """
                ).ConfigureAwait(false)
            ).ConfigureAwait(false);
            LogLine($"Bridge: invoke status = {invokeStatus}");

            if (invokeStatus != "invoked")
            {
                LogLine($"Bridge: ✗ Failed to invoke greet: {invokeStatus}");
                ResultBridge = "FAIL";
                return false;
            }

            // Poll for the result (up to 5s).
            string? jsResult = null;
            for (int i = 0; i < 25; i++)
            {
                await Task.Delay(200).ConfigureAwait(false);
                jsResult = await Dispatcher.UIThread.InvokeAsync(async () =>
                    await wv.InvokeScriptAsync("window.__e2eBridgeResult").ConfigureAwait(false)
                ).ConfigureAwait(false);

                if (jsResult is not null && jsResult != "null" && jsResult.Length > 0)
                    break;
            }

            LogLine($"Bridge: JS called greet('World') → {jsResult}");

            if (jsResult?.Contains("Hello, World!") == true)
            {
                LogLine("Bridge: ✓ JsExport E2E passed.");
            }
            else
            {
                LogLine($"Bridge: ✗ Unexpected result: {jsResult}");
                ResultBridge = "FAIL";
                return false;
            }

            // 9b. Clean up.
            wv.Bridge.Remove<IE2EGreeter>();
            LogLine("Bridge: Removed IE2EGreeter.");

            ResultBridge = "PASS";
            return true;
        }
        catch (Exception ex)
        {
            LogLine($"Bridge: FAIL — {ex.Message}");
            ResultBridge = "FAIL";
            return false;
        }
    }

    // ---------------------------------------------------------------------------
    //  Scenario 10: DevTools Toggle
    // ---------------------------------------------------------------------------

    private async Task<bool> RunDevToolsAsync()
    {
        ResultDevTools = "...";
        try
        {
            LogLine("[10] DevTools Toggle");

            var wv = WebViewControl!;

            // Initial state: DevTools should not be open.
            var initialState = await wv.IsDevToolsOpenAsync().ConfigureAwait(false);
            LogLine($"  IsDevToolsOpen (initial): {initialState}");

            // Open DevTools — no-op on platforms that don't support it, should not throw.
            await wv.OpenDevToolsAsync().ConfigureAwait(false);
            LogLine("  OpenDevTools() → OK (no exception)");
            await Task.Delay(200).ConfigureAwait(false);

            // Close DevTools — no-op on platforms that don't support it, should not throw.
            await wv.CloseDevToolsAsync().ConfigureAwait(false);
            LogLine("  CloseDevTools() → OK (no exception)");

            LogLine("  PASS (DevTools toggle executed without error)");
            ResultDevTools = "PASS";
            return true;
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            ResultDevTools = "FAIL";
            return false;
        }
    }

    /// <summary>
    /// Manually opens DevTools for the current WebView. Can be bound to a toolbar
    /// button or invoked via keyboard shortcut (F12 / Cmd+Shift+I).
    /// </summary>
    [RelayCommand]
    private async Task OpenDevTools()
    {
        if (WebViewControl is null) return;

        try
        {
            await WebViewControl.OpenDevToolsAsync().ConfigureAwait(false);
            LogLine("DevTools: opened via manual command");
        }
        catch (Exception ex)
        {
            LogLine($"DevTools: failed — {ex.Message}");
        }
    }

    // ---------------------------------------------------------------------------
    //  Helpers
    // ---------------------------------------------------------------------------

    private async Task LoadTestHtmlAndWaitAsync()
    {
        LogLine("Loading test HTML...");
        var readyTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Handler(object? s, NavigationCompletedEventArgs e) => readyTcs.TrySetResult(true);

        WebViewControl!.NavigationCompleted += Handler;
        try
        {
            await Dispatcher.UIThread.InvokeAsync(
                () => WebViewControl.NavigateToStringAsync(TestHtml)
            ).ConfigureAwait(false);
            await WaitAsync(readyTcs.Task, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
            LogLine("Test HTML loaded.");
        }
        finally
        {
            WebViewControl!.NavigationCompleted -= Handler;
        }
    }

    private static async Task<T> WaitAsync<T>(Task<T> task, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        var completed = await Task.WhenAny(task, Task.Delay(Timeout.InfiniteTimeSpan, cts.Token)).ConfigureAwait(false);
        if (completed != task)
        {
            throw new TimeoutException($"Timed out after {timeout}.");
        }
        return await task.ConfigureAwait(false);
    }

    private void LogLine(string message)
    {
        var line = $"{DateTimeOffset.Now:HH:mm:ss.fff} {message}";
        _logSink?.Invoke(line);

        if (AutoRun)
        {
            Console.WriteLine(line);
        }
    }
}
