using Avalonia.Platform;
using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

/// <summary>
/// Contract tests for WebAuthBroker — verifies the full OAuth authentication flow
/// using mock dialogs (success, user cancel, timeout, validation).
/// </summary>
public sealed class WebAuthBrokerTests
{
    private readonly TestDispatcher _dispatcher = new();

    [Fact]
    public void Success_flow_returns_callback_uri()
    {
        var factory = new AuthTestDialogFactory(_dispatcher);
        var broker = new WebAuthBroker(factory);
        var owner = new DummyTopLevelWindow();
        var options = new AuthOptions
        {
            AuthorizeUri = new Uri("https://auth.example.com/authorize?client_id=test"),
            CallbackUri = new Uri("myapp://auth/callback"),
        };

        // The mock adapter auto-completes NavigateAsync.
        // After initial navigation completes, simulate the OAuth redirect to callback.
        factory.OnDialogCreated = (dialog, adapter) =>
        {
            adapter.AutoCompleteNavigation = true;
            adapter.OnNavigationAutoCompleted = () =>
            {
                // Simulate the OAuth provider redirecting to the callback URI.
                _ = adapter.SimulateNativeNavigationStartingAsync(
                    new Uri("myapp://auth/callback?code=abc123"));
            };
        };

        var result = DispatcherTestPump.Run(_dispatcher, () => broker.AuthenticateAsync(owner, options), TimeSpan.FromSeconds(10));

        Assert.Equal(WebAuthStatus.Success, result.Status);
        Assert.NotNull(result.CallbackUri);
        Assert.StartsWith("myapp://auth/callback", result.CallbackUri!.AbsoluteUri);
    }

    [Fact]
    public void User_cancel_returns_UserCancel()
    {
        var factory = new AuthTestDialogFactory(_dispatcher);
        var broker = new WebAuthBroker(factory);
        var owner = new DummyTopLevelWindow();
        var options = new AuthOptions
        {
            AuthorizeUri = new Uri("https://auth.example.com/authorize"),
            CallbackUri = new Uri("myapp://auth/callback"),
        };

        // After initial navigation completes, simulate user closing dialog.
        factory.OnDialogCreated = (dialog, adapter) =>
        {
            adapter.AutoCompleteNavigation = true;
            adapter.OnNavigationAutoCompleted = () =>
            {
                factory.LastHost?.SimulateUserClose();
            };
        };

        var result = DispatcherTestPump.Run(_dispatcher, () => broker.AuthenticateAsync(owner, options), TimeSpan.FromSeconds(10));

        Assert.Equal(WebAuthStatus.UserCancel, result.Status);
    }

    [Fact]
    public void Timeout_returns_Timeout()
    {
        var factory = new AuthTestDialogFactory(_dispatcher);
        var broker = new WebAuthBroker(factory);
        var owner = new DummyTopLevelWindow();
        var options = new AuthOptions
        {
            AuthorizeUri = new Uri("https://auth.example.com/authorize"),
            CallbackUri = new Uri("myapp://auth/callback"),
            Timeout = TimeSpan.FromMilliseconds(100),
        };

        // Navigate completes but no callback redirect — let timeout fire.
        factory.OnDialogCreated = (dialog, adapter) =>
        {
            adapter.AutoCompleteNavigation = true;
        };

        var result = DispatcherTestPump.Run(_dispatcher, () => broker.AuthenticateAsync(owner, options), TimeSpan.FromSeconds(10));

        Assert.Equal(WebAuthStatus.Timeout, result.Status);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task Missing_AuthorizeUri_throws()
    {
        var factory = new AuthTestDialogFactory(_dispatcher);
        var broker = new WebAuthBroker(factory);
        var owner = new DummyTopLevelWindow();

        var options = new AuthOptions
        {
            CallbackUri = new Uri("myapp://auth/callback"),
        };

        await Assert.ThrowsAsync<ArgumentException>(() => broker.AuthenticateAsync(owner, options));
    }

    [Fact]
    public async Task Missing_CallbackUri_throws()
    {
        var factory = new AuthTestDialogFactory(_dispatcher);
        var broker = new WebAuthBroker(factory);
        var owner = new DummyTopLevelWindow();

        var options = new AuthOptions
        {
            AuthorizeUri = new Uri("https://auth.example.com/authorize"),
        };

        await Assert.ThrowsAsync<ArgumentException>(() => broker.AuthenticateAsync(owner, options));
    }

    [Fact]
    public void Constructor_null_factory_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new WebAuthBroker(null!));
    }

    // ---- Test helpers ----

    private sealed class DummyTopLevelWindow : ITopLevelWindow
    {
        public IPlatformHandle? PlatformHandle => null;
    }

    /// <summary>
    /// Auth-specific dialog factory that creates real WebDialog instances backed by mocks,
    /// and allows test code to hook into the dialog lifecycle (e.g., simulate redirects).
    /// </summary>
    private sealed class AuthTestDialogFactory : IWebDialogFactory
    {
        private readonly TestDispatcher _dispatcher;

        public AuthTestDialogFactory(TestDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public MockDialogHost? LastHost { get; private set; }

        /// <summary>
        /// Called after a dialog is created. Test code can hook into adapter events here
        /// to simulate OAuth redirects, user actions, etc.
        /// </summary>
        public Action<WebDialog, MockWebViewAdapter>? OnDialogCreated { get; set; }

        public IWebDialog Create(IWebViewEnvironmentOptions? options = null)
        {
            var host = new MockDialogHost();
            var adapter = MockWebViewAdapter.Create();
            var dialog = new WebDialog(host, adapter, _dispatcher);
            LastHost = host;

            OnDialogCreated?.Invoke(dialog, adapter);
            return dialog;
        }
    }
}
