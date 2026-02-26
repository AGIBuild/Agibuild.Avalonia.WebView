using Agibuild.Fulora;
using Agibuild.Fulora.Testing;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Agibuild.Fulora.Integration.Tests.Automation;

/// <summary>
/// Headless integration tests for WebDialog lifecycle, window management,
/// and IWebView delegation through MockDialogHost.
/// These tests exercise the full WebDialog → WebViewCore → MockAdapter stack
/// in an Avalonia headless environment.
/// </summary>
public sealed class WebDialogIntegrationTests
{
    private readonly TestDispatcher _dispatcher = new();

    private (WebDialog Dialog, MockDialogHost Host, MockWebViewAdapter Adapter) CreateDialog()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.Create();
        var dialog = new WebDialog(host, adapter, _dispatcher);
        return (dialog, host, adapter);
    }

    // --- Lifecycle ---

    [AvaloniaFact]
    public void Create_show_close_lifecycle()
    {
        var (dialog, host, _) = CreateDialog();

        Assert.False(host.IsShown);
        dialog.Show();
        Assert.True(host.IsShown);

        dialog.Close();
        Assert.True(host.IsClosed);
    }

    [AvaloniaFact]
    public void Dispose_closes_host_and_is_idempotent()
    {
        var (dialog, host, _) = CreateDialog();
        dialog.Show();

        dialog.Dispose();
        Assert.True(host.IsClosed);
        Assert.Equal(1, host.CloseCallCount);

        // Second dispose is a no-op.
        dialog.Dispose();
        Assert.Equal(1, host.CloseCallCount);
    }

    // --- Window Management ---

    [AvaloniaFact]
    public void Title_round_trips_through_host()
    {
        var (dialog, host, _) = CreateDialog();
        dialog.Title = "Integration Title";

        Assert.Equal("Integration Title", dialog.Title);
        Assert.Equal("Integration Title", host.Title);
    }

    [AvaloniaFact]
    public void Resize_and_move_delegate_to_host()
    {
        var (dialog, host, _) = CreateDialog();

        Assert.True(dialog.Resize(1024, 768));
        Assert.Equal((1024, 768), host.LastResize);

        Assert.True(dialog.Move(50, 100));
        Assert.Equal((50, 100), host.LastMove);
    }

    [AvaloniaFact]
    public void CanUserResize_round_trips()
    {
        var (dialog, _, _) = CreateDialog();

        dialog.CanUserResize = true;
        Assert.True(dialog.CanUserResize);

        dialog.CanUserResize = false;
        Assert.False(dialog.CanUserResize);
    }

    // --- Closing Events ---

    [AvaloniaFact]
    public void User_close_raises_Closing_event()
    {
        var (dialog, host, _) = CreateDialog();
        dialog.Show();

        var closingRaised = false;
        dialog.Closing += (_, _) => closingRaised = true;

        host.SimulateUserClose();
        Assert.True(closingRaised);
    }

    [AvaloniaFact]
    public void Dispose_does_not_raise_Closing()
    {
        var (dialog, _, _) = CreateDialog();
        dialog.Show();

        var closingRaised = false;
        dialog.Closing += (_, _) => closingRaised = true;

        dialog.Dispose();
        Assert.False(closingRaised);
    }

    // --- IWebView Navigation ---

    [AvaloniaFact]
    public async Task NavigateAsync_flows_through_adapter()
    {
        var (dialog, _, adapter) = CreateDialog();
        adapter.AutoCompleteNavigation = true;

        var uri = new Uri("https://dialog-test.example.com");
        await dialog.NavigateAsync(uri);

        Assert.Equal(uri, adapter.LastNavigationUri);
        Assert.NotNull(adapter.LastNavigationId);
    }

    [AvaloniaFact]
    public async Task NavigateToStringAsync_flows_through_adapter()
    {
        var (dialog, _, adapter) = CreateDialog();
        adapter.AutoCompleteNavigation = true;

        const string html = "<html><body>Dialog HTML</body></html>";
        await dialog.NavigateToStringAsync(html);

        Assert.NotNull(adapter.LastNavigationId);
    }

    [AvaloniaFact]
    public async Task NavigateToStringAsync_with_baseUrl_flows_through_adapter()
    {
        var (dialog, _, adapter) = CreateDialog();
        adapter.AutoCompleteNavigation = true;
        var baseUrl = new Uri("https://base.example.com/");

        await dialog.NavigateToStringAsync("<h1>Test</h1>", baseUrl);

        Assert.Equal(baseUrl, adapter.LastBaseUrl);
    }

    [AvaloniaFact]
    public async Task InvokeScriptAsync_returns_adapter_result()
    {
        var (dialog, _, adapter) = CreateDialog();
        adapter.ScriptResult = "dialog-result";

        var result = await dialog.InvokeScriptAsync("return 'dialog-result'");
        Assert.Equal("dialog-result", result);
    }

    // --- Navigation Commands ---

    [AvaloniaFact]
    public async Task GoBack_and_GoForward_delegate()
    {
        var (dialog, _, adapter) = CreateDialog();

        // No history → should return false.
        Assert.False(await dialog.GoBackAsync());
        Assert.False(await dialog.GoForwardAsync());

        // With history.
        adapter.CanGoBack = true;
        adapter.GoBackAccepted = true;
        Assert.True(await dialog.GoBackAsync());

        adapter.CanGoForward = true;
        adapter.GoForwardAccepted = true;
        Assert.True(await dialog.GoForwardAsync());
    }

    [AvaloniaFact]
    public async Task Refresh_delegates()
    {
        var (dialog, _, adapter) = CreateDialog();
        adapter.RefreshAccepted = true;

        Assert.True(await dialog.RefreshAsync());
        Assert.Equal(1, adapter.RefreshCallCount);
    }

    [AvaloniaFact]
    public async Task Stop_cancels_active_navigation()
    {
        var (dialog, _, adapter) = CreateDialog();
        adapter.StopAccepted = true;

        // Start a navigation (doesn't auto-complete).
        var navTask = dialog.NavigateAsync(new Uri("https://slow.example.com"));

        // Stop it.
        Assert.True(await dialog.StopAsync());

        // Task should complete (canceled).
        await navTask;
    }

    // --- Navigation Events ---

    [AvaloniaFact]
    public async Task NavigationStarted_and_Completed_events_fire()
    {
        var (dialog, _, adapter) = CreateDialog();
        adapter.AutoCompleteNavigation = true;

        var startedRaised = false;
        var completedRaised = false;

        dialog.NavigationStarted += (_, _) => startedRaised = true;
        dialog.NavigationCompleted += (_, _) => completedRaised = true;

        await dialog.NavigateAsync(new Uri("https://events.example.com"));

        Assert.True(startedRaised);
        Assert.True(completedRaised);
    }

    [AvaloniaFact]
    public void NewWindowRequested_event_fires()
    {
        var (dialog, _, adapter) = CreateDialog();
        Uri? requestedUri = null;
        dialog.NewWindowRequested += (_, e) => requestedUri = e.Uri;

        adapter.RaiseNewWindowRequested(new Uri("https://popup.example.com"));

        Assert.Equal(new Uri("https://popup.example.com"), requestedUri);
    }

    [AvaloniaFact]
    public void WebResourceRequested_event_fires()
    {
        var (dialog, _, adapter) = CreateDialog();
        var raised = false;
        dialog.WebResourceRequested += (_, _) => raised = true;

        adapter.RaiseWebResourceRequested();
        Assert.True(raised);
    }

    // --- MockWebDialogFactory ---

    [AvaloniaFact]
    public void Factory_creates_distinct_dialogs()
    {
        var factory = new MockWebDialogFactory(_dispatcher);

        using var d1 = factory.Create();
        using var d2 = factory.Create();
        using var d3 = factory.Create(new WebViewEnvironmentOptions { UseEphemeralSession = true });

        Assert.Equal(3, factory.CreatedDialogs.Count);
        Assert.NotSame(d1, d2);
        Assert.NotSame(d2, d3);
    }
}
