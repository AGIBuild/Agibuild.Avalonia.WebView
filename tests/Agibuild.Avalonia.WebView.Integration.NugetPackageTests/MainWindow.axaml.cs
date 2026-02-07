using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Agibuild.Avalonia.WebView.Integration.NugetPackageTests;

public partial class MainWindow : Window
{
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
            // Step 1: WebView is created via XAML with DI-initialized environment
            UpdateStatus("Step 1/4: WebView created via XAML (DI initialized).");

            // Give the native control time to attach
            await Task.Delay(1000);

            // Step 2: Navigate to HTML content
            UpdateStatus("Step 2/4: Navigating to HTML content...");
            await WebViewControl.NavigateToStringAsync(
                "<html><body><h1 id='heading'>NuGet Smoke Test OK</h1></body></html>");

            // Step 3: Execute JavaScript
            UpdateStatus("Step 3/4: Executing JavaScript...");
            var result = await WebViewControl.InvokeScriptAsync(
                "document.getElementById('heading').textContent");

            // Step 4: Validate result
            if (result?.Contains("NuGet Smoke Test OK") == true)
            {
                UpdateStatus("PASSED: All 4 smoke tests passed.");
                WriteResult("PASSED");
            }
            else
            {
                UpdateStatus($"FAILED: Script result mismatch. Got: '{result}'");
                WriteResult($"FAILED: script result = '{result}'");
            }

            if (_isSmokeTest)
            {
                await Task.Delay(500);
                Dispatcher.UIThread.Post(() => Close());
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"FAILED: {ex.Message}");
            WriteResult($"FAILED: {ex}");

            if (_isSmokeTest)
            {
                await Task.Delay(500);
                Dispatcher.UIThread.Post(() => Close());
            }
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
}
