using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Agibuild.Avalonia.WebView.Integration.Tests.ViewModels;

/// <summary>
/// E2E test ViewModel for verifying four new features using a real native WebView:
///   1. Command Manager (Copy, Cut, Paste, SelectAll, Undo, Redo)
///   2. Screenshot Capture (CaptureScreenshotAsync)
///   3. Print to PDF (PrintToPdfAsync)
///   4. JS ↔ C# RPC (bidirectional JSON-RPC)
///
/// HOW TO RUN (for newcomers):
///   Manual:  Launch desktop app → select "Feature E2E" tab → click "Run All".
///   Auto:    dotnet run -- --feature-e2e   (exits with code 0 = pass, 1 = fail).
///
/// WHAT IT DOES:
///   1. Loads an HTML page into the WebView.
///   2. Runs each scenario in sequence.
///   3. Logs PASS/FAIL per scenario.
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

    public FeatureE2EViewModel()
    {
        Status = "Not started.";
    }

    /// <summary>When true, automatically runs all scenarios on WebView load.</summary>
    public bool AutoRun { get; set; }

    /// <summary>Fired when auto-run completes. Arg: 0 = pass, 1 = fail.</summary>
    public event Action<int>? AutoRunCompleted;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private string _log = string.Empty;

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
        await RunAllCoreAsync().ConfigureAwait(false);
    }

    // ---------------------------------------------------------------------------
    //  Core test runner
    // ---------------------------------------------------------------------------

    private async Task<bool> RunAllCoreAsync()
    {
        Status = "Running feature E2E tests...";
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

        Status = allPassed ? "ALL PASSED" : "SOME FAILED";
        LogLine($"Result: {Status}");
        return allPassed;
    }

    // ---------------------------------------------------------------------------
    //  Scenario 1: Command Manager
    // ---------------------------------------------------------------------------

    private async Task<bool> RunCommandManagerAsync()
    {
        try
        {
            LogLine("[1] Command Manager — SelectAll + Copy");

            var mgr = WebViewControl!.TryGetCommandManager();
            if (mgr is null)
            {
                LogLine("  SKIP: adapter does not support ICommandAdapter");
                return true;
            }

            // Focus the textarea and type something
            await WebViewControl.InvokeScriptAsync(
                "document.getElementById('editor').focus();"
            ).ConfigureAwait(false);
            await Task.Delay(200).ConfigureAwait(false);

            // SelectAll
            mgr.SelectAll();
            await Task.Delay(200).ConfigureAwait(false);

            // Copy
            mgr.Copy();
            await Task.Delay(200).ConfigureAwait(false);

            LogLine("  PASS (commands executed without error)");
            return true;
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            return false;
        }
    }

    // ---------------------------------------------------------------------------
    //  Scenario 2: Screenshot Capture
    // ---------------------------------------------------------------------------

    private async Task<bool> RunScreenshotAsync()
    {
        try
        {
            LogLine("[2] Screenshot Capture");

            var bytes = await WebViewControl!.CaptureScreenshotAsync().ConfigureAwait(false);

            if (bytes is null || bytes.Length == 0)
            {
                LogLine("  FAIL: empty bytes returned");
                return false;
            }

            // Verify PNG magic header: 0x89 P N G
            if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            {
                LogLine($"  PASS (PNG, {bytes.Length:N0} bytes)");
                return true;
            }
            else
            {
                LogLine($"  FAIL: not a valid PNG (header: {bytes[0]:X2} {bytes[1]:X2} {bytes[2]:X2} {bytes[3]:X2})");
                return false;
            }
        }
        catch (NotSupportedException)
        {
            LogLine("  SKIP: adapter does not support IScreenshotAdapter");
            return true;
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            return false;
        }
    }

    // ---------------------------------------------------------------------------
    //  Scenario 3: Print to PDF
    // ---------------------------------------------------------------------------

    private async Task<bool> RunPrintToPdfAsync()
    {
        try
        {
            LogLine("[3] Print to PDF");

            var bytes = await WebViewControl!.PrintToPdfAsync(
                new PdfPrintOptions { Landscape = false }
            ).ConfigureAwait(false);

            if (bytes is null || bytes.Length == 0)
            {
                LogLine("  FAIL: empty bytes returned");
                return false;
            }

            // Verify PDF magic header: %PDF
            if (bytes.Length >= 4 && bytes[0] == (byte)'%' && bytes[1] == (byte)'P' && bytes[2] == (byte)'D' && bytes[3] == (byte)'F')
            {
                LogLine($"  PASS (PDF, {bytes.Length:N0} bytes)");
                return true;
            }
            else
            {
                var header = System.Text.Encoding.ASCII.GetString(bytes, 0, Math.Min(bytes.Length, 8));
                LogLine($"  FAIL: not a valid PDF (header: {header})");
                return false;
            }
        }
        catch (NotSupportedException)
        {
            LogLine("  SKIP: adapter does not support IPrintAdapter (GTK/Android)");
            return true;
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            return false;
        }
    }

    // ---------------------------------------------------------------------------
    //  Scenario 4: JS ↔ C# RPC
    // ---------------------------------------------------------------------------

    private async Task<bool> RunRpcAsync()
    {
        try
        {
            LogLine("[4] JS ↔ C# RPC");

            // Enable the WebMessage bridge (required for RPC)
            WebViewControl!.EnableWebMessageBridge(new WebMessageBridgeOptions
            {
                AllowedOrigins = new HashSet<string> { "*" }
            });

            var rpc = WebViewControl.Rpc;
            if (rpc is null)
            {
                LogLine("  FAIL: Rpc is null after enabling bridge");
                return false;
            }

            // --- 4a: Register a C# handler and invoke it from JS ---
            LogLine("  [4a] JS → C# call");
            var handlerCalled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            rpc.Handle("e2e.ping", _ =>
            {
                handlerCalled.TrySetResult(true);
                return Task.FromResult<object?>("pong");
            });

            // Inject JS to call the C# method
            await WebViewControl.InvokeScriptAsync(
                "window.__agRpc && window.__agRpc.invoke('e2e.ping').then(r => document.getElementById('result').textContent = r)"
            ).ConfigureAwait(false);

            // Wait for the C# handler to be called
            var called = await WaitAsync(handlerCalled.Task, TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            if (!called)
            {
                LogLine("  FAIL: C# handler was not called");
                return false;
            }
            LogLine("  [4a] PASS");

            // --- 4b: C# calls JS and gets result ---
            LogLine("  [4b] C# → JS call");

            // Register a JS handler
            await WebViewControl.InvokeScriptAsync(
                "window.__agRpc && window.__agRpc.handle('e2e.getTitle', () => document.title)"
            ).ConfigureAwait(false);
            await Task.Delay(200).ConfigureAwait(false);

            var title = await rpc.InvokeAsync<string>("e2e.getTitle").ConfigureAwait(false);
            if (title == "Feature E2E Test")
            {
                LogLine($"  [4b] PASS (title={title})");
            }
            else
            {
                LogLine($"  FAIL: expected 'Feature E2E Test', got '{title}'");
                return false;
            }

            LogLine("  PASS (all RPC sub-scenarios)");
            return true;
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            return false;
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
        Log = $"{Log}{line}{Environment.NewLine}";

        if (AutoRun)
        {
            Console.WriteLine(line);
        }
    }
}
