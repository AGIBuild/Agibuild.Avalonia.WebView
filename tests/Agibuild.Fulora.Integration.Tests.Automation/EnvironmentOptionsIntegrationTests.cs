using Agibuild.Fulora;
using Agibuild.Fulora.Testing;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Agibuild.Fulora.Integration.Tests.Automation;

/// <summary>
/// Headless integration tests for M2 environment options (DevTools, UserAgent, Ephemeral)
/// and their propagation through the WebViewCore → Adapter stack.
/// </summary>
public sealed class EnvironmentOptionsIntegrationTests
{
    private readonly TestDispatcher _dispatcher = new();

    // --- WebViewEnvironment Global Options ---

    [AvaloniaFact]
    public void Default_options_have_safe_defaults()
    {
        // Reset to a fresh state for this test.
        var options = new WebViewEnvironmentOptions();

        Assert.False(options.EnableDevTools);
        Assert.False(options.UseEphemeralSession);
        Assert.Null(options.CustomUserAgent);
    }

    [AvaloniaFact]
    public void Initialize_with_options_sets_global_options()
    {
        var original = WebViewEnvironment.Options;
        try
        {
            var custom = new WebViewEnvironmentOptions
            {
                EnableDevTools = true,
                UseEphemeralSession = true,
                CustomUserAgent = "Test/1.0"
            };

            WebViewEnvironment.Initialize(null, custom);
            Assert.Same(custom, WebViewEnvironment.Options);
        }
        finally
        {
            // Restore original.
            WebViewEnvironment.Options = original;
        }
    }

    [AvaloniaFact]
    public void Initialize_with_null_options_preserves_existing()
    {
        var original = WebViewEnvironment.Options;
        try
        {
            var custom = new WebViewEnvironmentOptions { EnableDevTools = true };
            WebViewEnvironment.Options = custom;

            WebViewEnvironment.Initialize(null, null);
            Assert.Same(custom, WebViewEnvironment.Options);
        }
        finally
        {
            WebViewEnvironment.Options = original;
        }
    }

    // --- Environment Options Applied to Adapter ---

    [AvaloniaFact]
    public void Options_applied_to_supporting_adapter_at_construction()
    {
        var original = WebViewEnvironment.Options;
        try
        {
            var opts = new WebViewEnvironmentOptions
            {
                EnableDevTools = true,
                UseEphemeralSession = true,
                CustomUserAgent = "IntegrationTest/2.0"
            };
            WebViewEnvironment.Options = opts;

            var adapter = MockWebViewAdapter.CreateWithOptions();
            var core = new WebViewCore(adapter, _dispatcher);

            Assert.NotNull(adapter.AppliedOptions);
            Assert.True(adapter.AppliedOptions!.EnableDevTools);
            Assert.True(adapter.AppliedOptions!.UseEphemeralSession);
            Assert.Equal("IntegrationTest/2.0", adapter.AppliedOptions!.CustomUserAgent);

            // ApplyEnvironmentOptions is called, SetCustomUserAgent is NOT called at construction
            // (it's a separate runtime API). Verify options carry the UA.
            Assert.Equal(1, adapter.ApplyOptionsCallCount);

            core.Dispose();
        }
        finally
        {
            WebViewEnvironment.Options = original;
        }
    }

    [AvaloniaFact]
    public void Options_not_applied_to_basic_adapter()
    {
        var original = WebViewEnvironment.Options;
        try
        {
            WebViewEnvironment.Options = new WebViewEnvironmentOptions { EnableDevTools = true };

            // Basic MockWebViewAdapter does NOT implement IWebViewAdapterOptions.
            var adapter = MockWebViewAdapter.Create();
            var core = new WebViewCore(adapter, _dispatcher);

            // Should not throw — just skips the options application.
            // No assertion needed — the test is that it doesn't crash.

            core.Dispose();
        }
        finally
        {
            WebViewEnvironment.Options = original;
        }
    }

    // --- SetCustomUserAgent ---

    [AvaloniaFact]
    public void SetCustomUserAgent_delegates_to_supporting_adapter()
    {
        var adapter = MockWebViewAdapter.CreateWithOptions();
        var core = new WebViewCore(adapter, _dispatcher);

        core.SetCustomUserAgent("MyCustomUA/3.0");
        Assert.Equal("MyCustomUA/3.0", adapter.AppliedUserAgent);

        core.SetCustomUserAgent(null);
        Assert.Null(adapter.AppliedUserAgent);

        core.Dispose();
    }

    [AvaloniaFact]
    public void SetCustomUserAgent_noop_on_basic_adapter()
    {
        var adapter = MockWebViewAdapter.Create();
        var core = new WebViewCore(adapter, _dispatcher);

        // Should not throw on adapter without IWebViewAdapterOptions.
        core.SetCustomUserAgent("Test/1.0");
        core.SetCustomUserAgent(null);

        core.Dispose();
    }

    // --- WebDialog with Environment Options ---

    [AvaloniaFact]
    public async Task WebDialog_with_options_adapter_receives_options()
    {
        var original = WebViewEnvironment.Options;
        try
        {
            var opts = new WebViewEnvironmentOptions { EnableDevTools = true };
            WebViewEnvironment.Options = opts;

            var host = new MockDialogHost();
            var adapter = MockWebViewAdapter.CreateWithOptions();
            adapter.AutoCompleteNavigation = true;
            var dialog = new WebDialog(host, adapter, _dispatcher);

            // Verify options were applied.
            Assert.NotNull(adapter.AppliedOptions);
            Assert.True(adapter.AppliedOptions!.EnableDevTools);

            dialog.Show();
            await dialog.NavigateAsync(new Uri("https://example.com"));

            dialog.Dispose();
        }
        finally
        {
            WebViewEnvironment.Options = original;
        }
    }
}
