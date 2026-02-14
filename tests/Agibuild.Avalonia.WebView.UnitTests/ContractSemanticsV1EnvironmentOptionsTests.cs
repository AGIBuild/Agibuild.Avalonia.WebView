using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

/// <summary>
/// Contract tests for M2 environment options: DevTools, UserAgent, Ephemeral.
/// Verifies that WebViewCore passes options to adapters implementing IWebViewAdapterOptions.
/// </summary>
[Collection("WebViewEnvironmentState")]
public sealed class ContractSemanticsV1EnvironmentOptionsTests
{
    private readonly TestDispatcher _dispatcher = new();

    [Fact]
    public void Options_applied_to_adapter_during_construction()
    {
        // Arrange: set global environment options.
        var savedOptions = WebViewEnvironment.Options;
        try
        {
            WebViewEnvironment.Options = new WebViewEnvironmentOptions
            {
                EnableDevTools = true,
                CustomUserAgent = "TestAgent/1.0",
                UseEphemeralSession = true,
            };

            var adapter = MockWebViewAdapter.CreateWithOptions();

            // Act: construct WebViewCore (constructor applies options).
            using var core = new WebViewCore(adapter, _dispatcher);

            // Assert
            Assert.Equal(1, adapter.ApplyOptionsCallCount);
            Assert.NotNull(adapter.AppliedOptions);
            Assert.True(adapter.AppliedOptions!.EnableDevTools);
            Assert.Equal("TestAgent/1.0", adapter.AppliedOptions.CustomUserAgent);
            Assert.True(adapter.AppliedOptions.UseEphemeralSession);
        }
        finally
        {
            WebViewEnvironment.Options = savedOptions;
        }
    }

    [Fact]
    public void Options_not_applied_when_adapter_does_not_implement_IWebViewAdapterOptions()
    {
        // Arrange: plain adapter without options support.
        var adapter = MockWebViewAdapter.Create();

        // Act: should not throw.
        using var core = new WebViewCore(adapter, _dispatcher);

        // Assert: no crash, no options applied (adapter doesn't support the interface).
        Assert.NotNull(core);
    }

    [Fact]
    public void SetCustomUserAgent_delegates_to_adapter()
    {
        var adapter = MockWebViewAdapter.CreateWithOptions();
        using var core = new WebViewCore(adapter, _dispatcher);

        // Act
        core.SetCustomUserAgent("Custom/2.0");

        // Assert
        Assert.Equal(1, adapter.SetUserAgentCallCount);
        Assert.Equal("Custom/2.0", adapter.AppliedUserAgent);
    }

    [Fact]
    public void SetCustomUserAgent_null_resets_to_default()
    {
        var adapter = MockWebViewAdapter.CreateWithOptions();
        using var core = new WebViewCore(adapter, _dispatcher);

        core.SetCustomUserAgent("Custom/1.0");
        core.SetCustomUserAgent(null);

        Assert.Equal(2, adapter.SetUserAgentCallCount);
        Assert.Null(adapter.AppliedUserAgent);
    }

    [Fact]
    public void SetCustomUserAgent_noop_when_adapter_unsupported()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);

        // Act: should not throw.
        core.SetCustomUserAgent("Anything");
    }

    [Fact]
    public void SetCustomUserAgent_after_dispose_throws()
    {
        var adapter = MockWebViewAdapter.CreateWithOptions();
        var core = new WebViewCore(adapter, _dispatcher);
        core.Dispose();

        Assert.Throws<ObjectDisposedException>(() => core.SetCustomUserAgent("X"));
    }

    [Fact]
    public void WebViewEnvironment_Initialize_with_options_sets_Options()
    {
        var savedOptions = WebViewEnvironment.Options;
        try
        {
            var custom = new WebViewEnvironmentOptions
            {
                EnableDevTools = true,
                CustomUserAgent = "MyApp/3.0",
                UseEphemeralSession = false,
            };

            WebViewEnvironment.Initialize(null, custom);

            Assert.Same(custom, WebViewEnvironment.Options);
            Assert.True(WebViewEnvironment.Options.EnableDevTools);
            Assert.Equal("MyApp/3.0", WebViewEnvironment.Options.CustomUserAgent);
            Assert.False(WebViewEnvironment.Options.UseEphemeralSession);
        }
        finally
        {
            WebViewEnvironment.Options = savedOptions;
        }
    }

    [Fact]
    public void Explicit_environment_options_are_instance_scoped_and_do_not_mutate_global_state()
    {
        var savedOptions = WebViewEnvironment.Options;
        try
        {
            var global = new WebViewEnvironmentOptions
            {
                EnableDevTools = false,
                CustomUserAgent = "Global/1.0",
                UseEphemeralSession = false
            };
            WebViewEnvironment.Options = global;

            var instanceOptions = new WebViewEnvironmentOptions
            {
                EnableDevTools = true,
                CustomUserAgent = "Instance/2.0",
                UseEphemeralSession = true
            };

            var adapter = MockWebViewAdapter.CreateWithOptions();
            using var core = new WebViewCore(
                adapter,
                _dispatcher,
                NullLogger<WebViewCore>.Instance,
                instanceOptions);

            Assert.Equal(1, adapter.ApplyOptionsCallCount);
            Assert.NotNull(adapter.AppliedOptions);
            Assert.True(adapter.AppliedOptions!.EnableDevTools);
            Assert.Equal("Instance/2.0", adapter.AppliedOptions.CustomUserAgent);
            Assert.True(adapter.AppliedOptions.UseEphemeralSession);
            Assert.Same(global, WebViewEnvironment.Options);
        }
        finally
        {
            WebViewEnvironment.Options = savedOptions;
        }
    }

    [Fact]
    public void WebViewEnvironmentOptions_default_values()
    {
        var opts = new WebViewEnvironmentOptions();

        Assert.False(opts.EnableDevTools);
        Assert.Null(opts.CustomUserAgent);
        Assert.False(opts.UseEphemeralSession);
    }
}
