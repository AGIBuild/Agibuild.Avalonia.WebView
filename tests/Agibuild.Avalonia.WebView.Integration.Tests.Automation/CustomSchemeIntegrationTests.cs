using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Testing;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Automation;

/// <summary>
/// Integration tests for the Custom Scheme registration feature.
/// Exercises WebViewCore → ICustomSchemeAdapter.RegisterCustomSchemes().
///
/// HOW IT WORKS (for newcomers):
///   1. We register custom schemes in WebViewEnvironment.Options BEFORE creating the core.
///   2. We create a WebViewCore with MockWebViewAdapterWithCustomSchemes.
///   3. The ctor detects ICustomSchemeAdapter and calls RegisterCustomSchemes.
///   4. We verify the adapter received the correct scheme registrations.
///
/// NOTE: WebViewEnvironment.Options is a global static. We swap the entire Options
///       object and restore it in a finally block.
/// </summary>
public sealed class CustomSchemeIntegrationTests
{
    private readonly TestDispatcher _dispatcher = new();

    /// <summary>Swap Options to include custom schemes, restore in finally.</summary>
    private IWebViewEnvironmentOptions SwapOptions(params CustomSchemeRegistration[] schemes)
    {
        var original = WebViewEnvironment.Options;
        WebViewEnvironment.Options = new WebViewEnvironmentOptions
        {
            CustomSchemes = schemes
        };
        return original;
    }

    // ──────────────────── Test 1: Schemes registered on construction ────────────────────

    [AvaloniaFact]
    public void CustomSchemes_registered_on_core_creation()
    {
        var original = SwapOptions(
            new CustomSchemeRegistration { SchemeName = "app", HasAuthorityComponent = true, TreatAsSecure = true });

        try
        {
            var adapter = MockWebViewAdapter.CreateWithCustomSchemes();
            using var core = new WebViewCore(adapter, _dispatcher);

            Assert.Equal(1, adapter.RegisterCallCount);
            Assert.NotNull(adapter.RegisteredSchemes);
            Assert.Single(adapter.RegisteredSchemes!);
            Assert.Equal("app", adapter.RegisteredSchemes![0].SchemeName);
            Assert.True(adapter.RegisteredSchemes[0].HasAuthorityComponent);
            Assert.True(adapter.RegisteredSchemes[0].TreatAsSecure);
        }
        finally
        {
            WebViewEnvironment.Options = original;
        }
    }

    // ──────────────────── Test 2: No schemes = no call ────────────────────

    [AvaloniaFact]
    public void No_customSchemes_does_not_call_register()
    {
        var original = SwapOptions(); // empty array

        try
        {
            var adapter = MockWebViewAdapter.CreateWithCustomSchemes();
            using var core = new WebViewCore(adapter, _dispatcher);

            Assert.Equal(0, adapter.RegisterCallCount);
        }
        finally
        {
            WebViewEnvironment.Options = original;
        }
    }

    // ──────────────────── Test 3: Basic adapter ignores schemes ────────────────────

    [AvaloniaFact]
    public void Basic_adapter_without_ICustomSchemeAdapter_ignores_schemes()
    {
        var original = SwapOptions(new CustomSchemeRegistration { SchemeName = "myapp" });

        try
        {
            var adapter = MockWebViewAdapter.Create();
            using var core = new WebViewCore(adapter, _dispatcher);
            // Should not throw — silently ignores
        }
        finally
        {
            WebViewEnvironment.Options = original;
        }
    }

    // ──────────────────── Test 4: WebResourceRequested event ────────────────────

    [AvaloniaFact]
    public void WebResourceRequested_event_fires_through_dialog()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.Create();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        WebResourceRequestedEventArgs? received = null;
        dialog.WebResourceRequested += (_, e) => received = e;

        adapter.RaiseWebResourceRequested();

        Assert.NotNull(received);
    }
}
