using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Testing;
using Avalonia.Headless.XUnit;
using Avalonia.Platform;
using Xunit;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Automation;

/// <summary>
/// Headless integration tests for WebAuthBroker OAuth flow simulation.
/// Uses MockWebDialogFactory to verify the auth orchestration without
/// requiring a real platform window or network.
/// </summary>
public sealed class WebAuthBrokerIntegrationTests
{
    private readonly TestDispatcher _dispatcher = new();

    private static AuthOptions MakeOptions(
        string authorizeUrl = "https://auth.example.com/authorize?client_id=test",
        string callbackUrl = "https://example.com/callback",
        bool ephemeral = true,
        TimeSpan? timeout = null)
    {
        return new AuthOptions
        {
            AuthorizeUri = new Uri(authorizeUrl),
            CallbackUri = new Uri(callbackUrl),
            UseEphemeralSession = ephemeral,
            Timeout = timeout
        };
    }

    // --- Success Flow ---

    [AvaloniaFact]
    public async Task Success_flow_returns_callback_uri()
    {
        var factory = new AuthTestDialogFactory(_dispatcher);
        var broker = new WebAuthBroker(factory);
        var owner = new TestTopLevelWindow();

        var options = MakeOptions();

        // Set up: after navigation, simulate redirect to callback URI.
        factory.OnDialogNavigated = (host, adapter) =>
        {
            // Simulate the OAuth server redirecting to callback.
            var callbackUri = new Uri("https://example.com/callback?code=auth_code_123&state=xyz");
            adapter.SimulateNativeNavigationStartingAsync(callbackUri).AsTask().GetAwaiter().GetResult();
        };

        var result = await broker.AuthenticateAsync(owner, options);

        Assert.Equal(WebAuthStatus.Success, result.Status);
        Assert.NotNull(result.CallbackUri);
        Assert.Contains("code=auth_code_123", result.CallbackUri!.Query);
    }

    [AvaloniaFact]
    public async Task Success_flow_with_local_callback_uri_does_not_depend_on_network()
    {
        var factory = new AuthTestDialogFactory(_dispatcher);
        var broker = new WebAuthBroker(factory);
        var owner = new TestTopLevelWindow();

        // Local custom-scheme callback simulation: should complete without external navigation.
        var options = MakeOptions(
            authorizeUrl: "agibuild-auth://callback?code=local_simulated_code&state=deterministic",
            callbackUrl: "agibuild-auth://callback",
            timeout: TimeSpan.FromSeconds(2));

        var result = await broker.AuthenticateAsync(owner, options);

        Assert.Equal(WebAuthStatus.Success, result.Status);
        Assert.NotNull(result.CallbackUri);
        Assert.Equal("agibuild-auth", result.CallbackUri!.Scheme);
        Assert.Contains("code=local_simulated_code", result.CallbackUri.Query, StringComparison.Ordinal);
    }

    // --- User Cancel Flow ---

    [AvaloniaFact]
    public async Task User_cancel_returns_UserCancel()
    {
        var factory = new AuthTestDialogFactory(_dispatcher);
        var broker = new WebAuthBroker(factory);
        var owner = new TestTopLevelWindow();

        var options = MakeOptions();

        // Set up: after navigation, simulate user closing the dialog.
        factory.OnDialogNavigated = (host, adapter) =>
        {
            // Simulate user clicking the close button.
            host.SimulateUserClose();
        };

        var result = await broker.AuthenticateAsync(owner, options);

        Assert.Equal(WebAuthStatus.UserCancel, result.Status);
        Assert.Null(result.CallbackUri);
    }

    // --- Timeout Flow ---

    [AvaloniaFact]
    public async Task Timeout_returns_Timeout_status()
    {
        var factory = new AuthTestDialogFactory(_dispatcher);
        var broker = new WebAuthBroker(factory);
        var owner = new TestTopLevelWindow();

        // Short timeout — do NOT simulate redirect or close.
        var options = MakeOptions(timeout: TimeSpan.FromMilliseconds(200));

        factory.OnDialogNavigated = (host, adapter) =>
        {
            // Do nothing — let it time out.
        };

        var result = await broker.AuthenticateAsync(owner, options);

        Assert.Equal(WebAuthStatus.Timeout, result.Status);
        Assert.NotNull(result.Error);
        Assert.Contains("timed out", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // --- Parameter Validation ---

    [AvaloniaFact]
    public async Task Null_owner_throws()
    {
        var factory = new AuthTestDialogFactory(_dispatcher);
        var broker = new WebAuthBroker(factory);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            broker.AuthenticateAsync(null!, MakeOptions()));
    }

    [AvaloniaFact]
    public async Task Null_options_throws()
    {
        var factory = new AuthTestDialogFactory(_dispatcher);
        var broker = new WebAuthBroker(factory);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            broker.AuthenticateAsync(new TestTopLevelWindow(), null!));
    }

    [AvaloniaFact]
    public async Task Missing_authorizeUri_throws()
    {
        var factory = new AuthTestDialogFactory(_dispatcher);
        var broker = new WebAuthBroker(factory);

        var options = new AuthOptions
        {
            CallbackUri = new Uri("https://example.com/callback")
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            broker.AuthenticateAsync(new TestTopLevelWindow(), options));
    }

    [AvaloniaFact]
    public async Task Missing_callbackUri_throws()
    {
        var factory = new AuthTestDialogFactory(_dispatcher);
        var broker = new WebAuthBroker(factory);

        var options = new AuthOptions
        {
            AuthorizeUri = new Uri("https://auth.example.com/authorize")
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            broker.AuthenticateAsync(new TestTopLevelWindow(), options));
    }

    // --- Ephemeral Session ---

    [AvaloniaFact]
    public async Task Ephemeral_session_creates_dialog_with_options()
    {
        var factory = new AuthTestDialogFactory(_dispatcher);
        var broker = new WebAuthBroker(factory);
        var owner = new TestTopLevelWindow();

        var options = MakeOptions(ephemeral: true);

        factory.OnDialogNavigated = (host, adapter) =>
        {
            var callbackUri = new Uri("https://example.com/callback?code=test");
            adapter.SimulateNativeNavigationStartingAsync(callbackUri).AsTask().GetAwaiter().GetResult();
        };

        var result = await broker.AuthenticateAsync(owner, options);
        Assert.Equal(WebAuthStatus.Success, result.Status);

        // Verify ephemeral options were passed to the factory.
        Assert.True(factory.LastCreatedWithEphemeral);
    }

    // --- Helper Classes ---

    /// <summary>
    /// Test factory that creates WebDialog with MockDialogHost and MockWebViewAdapter,
    /// and wires up auto-complete navigation with a configurable post-navigation callback.
    /// The callback receives the MockDialogHost and MockWebViewAdapter for simulating
    /// post-navigation events (e.g., OAuth redirect or user close).
    /// </summary>
    private sealed class AuthTestDialogFactory : IWebDialogFactory
    {
        private readonly TestDispatcher _dispatcher;

        public AuthTestDialogFactory(TestDispatcher dispatcher) => _dispatcher = dispatcher;

        public Action<MockDialogHost, MockWebViewAdapter>? OnDialogNavigated { get; set; }
        public MockDialogHost? LastHost { get; private set; }
        public bool LastCreatedWithEphemeral { get; private set; }

        public IWebDialog Create(IWebViewEnvironmentOptions? options = null)
        {
            LastCreatedWithEphemeral = options?.UseEphemeralSession ?? false;
            var host = new MockDialogHost();
            LastHost = host;
            var adapter = MockWebViewAdapter.Create();
            adapter.AutoCompleteNavigation = true;

            // Wire up: after navigation auto-completes, invoke the test callback.
            adapter.OnNavigationAutoCompleted = () =>
            {
                OnDialogNavigated?.Invoke(host, adapter);
            };

            return new WebDialog(host, adapter, _dispatcher);
        }
    }

    private sealed class TestTopLevelWindow : ITopLevelWindow
    {
        public IPlatformHandle? PlatformHandle => null;
    }
}
