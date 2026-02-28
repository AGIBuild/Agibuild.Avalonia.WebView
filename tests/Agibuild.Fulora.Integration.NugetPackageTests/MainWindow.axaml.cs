using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Agibuild.Fulora.Integration.NugetPackageTests;

public partial class MainWindow : Window
{
    private const int TotalSteps = 7;
    private bool _isSmokeTest;

    public MainWindow()
    {
        InitializeComponent();
        _isSmokeTest = Array.Exists(
            Environment.GetCommandLineArgs(), a => a == "--smoke-test");

        Loaded += async (_, _) => await RunSmokeTestAsync();
    }

    private async Task RunSmokeTestAsync()
    {
        try
        {
            // ── Step 1: Agibuild.Fulora — WebView created via XAML ──
            UpdateStatus($"Step 1/{TotalSteps}: WebView created via XAML (DI initialized).");
            await Task.Delay(1000);

            // ── Step 2: Agibuild.Fulora — HTML navigation ──
            UpdateStatus($"Step 2/{TotalSteps}: Navigating to HTML content...");
            await WebViewControl.NavigateToStringAsync(
                "<html><body><h1 id='heading'>NuGet Smoke Test OK</h1></body></html>");

            // ── Step 3: Agibuild.Fulora — JavaScript execution ──
            UpdateStatus($"Step 3/{TotalSteps}: Executing JavaScript...");
            var jsResult = await WebViewControl.InvokeScriptAsync(
                "document.getElementById('heading').textContent");

            if (jsResult?.Contains("NuGet Smoke Test OK") != true)
            {
                Fail($"Script result mismatch. Got: '{jsResult}'");
                return;
            }

            // ── Step 4: Agibuild.Fulora.Core — MockBridgeService + [JsExport] ──
            UpdateStatus($"Step 4/{TotalSteps}: Core — MockBridgeService.Expose<ISmokeExportService>...");
            var mock = new MockBridgeService();
            mock.Expose<ISmokeExportService>(new SmokeExportService());
            if (!mock.WasExposed<ISmokeExportService>())
            {
                Fail("MockBridgeService.WasExposed<ISmokeExportService>() returned false.");
                return;
            }

            // ── Step 5: Agibuild.Fulora.Core — MockBridgeService + [JsImport] proxy ──
            UpdateStatus($"Step 5/{TotalSteps}: Core — MockBridgeService.GetProxy<ISmokeImportService>...");
            mock.SetupProxy<ISmokeImportService>(new StubImportService());
            var proxy = mock.GetProxy<ISmokeImportService>();
            if (proxy is null)
            {
                Fail("MockBridgeService.GetProxy<ISmokeImportService>() returned null.");
                return;
            }

            // ── Step 6: Agibuild.Fulora.Bridge.Generator — source-gen verification ──
            UpdateStatus($"Step 6/{TotalSteps}: Bridge.Generator — verifying source-generated registration...");
            var registrationAttrs = Assembly.GetExecutingAssembly()
                .GetCustomAttributes<BridgeRegistrationAttribute>()
                .ToList();

            var hasExportReg = registrationAttrs.Any(a => a.InterfaceType == typeof(ISmokeExportService));
            if (!hasExportReg)
            {
                Fail("Bridge.Generator did not emit [BridgeRegistration] for ISmokeExportService. " +
                     $"Found {registrationAttrs.Count} registration(s).");
                return;
            }

            // ── Step 7: Bridge.Expose on real WebView uses source-generated proxy ──
            UpdateStatus($"Step 7/{TotalSteps}: Bridge.Expose<ISmokeExportService> on real WebView bridge...");
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                WebViewControl.Bridge.Expose<ISmokeExportService>(new SmokeExportService());
                // If we reach here without exception, Expose worked (source-gen or reflection fallback).
                // Clean up to leave bridge in original state.
                WebViewControl.Bridge.Remove<ISmokeExportService>();
            });

            UpdateStatus($"PASSED: All {TotalSteps} smoke tests passed.");
            WriteResult("PASSED");

            if (_isSmokeTest)
            {
                await Task.Delay(500);
                Dispatcher.UIThread.Post(() => Close());
            }
        }
        catch (Exception ex)
        {
            Fail(ex.ToString());
        }
    }

    private void Fail(string reason)
    {
        UpdateStatus($"FAILED: {reason}");
        WriteResult($"FAILED: {reason}");

        if (_isSmokeTest)
        {
            Task.Delay(500).ContinueWith(_ =>
                Dispatcher.UIThread.Post(() => Close()));
        }
    }

    private void UpdateStatus(string message)
    {
        Console.WriteLine($"[SmokeTest] {message}");
        Dispatcher.UIThread.Post(() => StatusText.Text = message);
    }

    private static void WriteResult(string result)
    {
        try
        {
            var dir = AppContext.BaseDirectory;
            var path = Path.Combine(dir, "smoke-test-result.txt");
            File.WriteAllText(path, result);
            Console.WriteLine($"[SmokeTest] Result written to: {path}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SmokeTest] Failed to write result: {ex.Message}");
        }
    }

    private class StubImportService : ISmokeImportService
    {
        public Task Notify(string message) => Task.CompletedTask;
    }
}
