using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

/// <summary>
/// Tests for MockBridgeService (deliverable 1.5).
/// </summary>
public sealed class MockBridgeServiceTests
{
    [Fact]
    public void Expose_records_implementation()
    {
        var mock = new MockBridgeService();
        var impl = new FakeAppService();

        mock.Expose<IAppService>(impl);

        Assert.True(mock.WasExposed<IAppService>());
        Assert.Same(impl, mock.GetExposedImplementation<IAppService>());
    }

    [Fact]
    public void WasExposed_returns_false_when_not_exposed()
    {
        var mock = new MockBridgeService();
        Assert.False(mock.WasExposed<IAppService>());
    }

    [Fact]
    public void GetProxy_returns_configured_proxy()
    {
        var mock = new MockBridgeService();
        var fakeUi = new FakeUiController();
        mock.SetupProxy<IUiController>(fakeUi);

        var proxy = mock.GetProxy<IUiController>();

        Assert.Same(fakeUi, proxy);
    }

    [Fact]
    public void GetProxy_without_setup_throws()
    {
        var mock = new MockBridgeService();

        Assert.Throws<InvalidOperationException>(() => mock.GetProxy<IUiController>());
    }

    [Fact]
    public void Remove_clears_exposed_service()
    {
        var mock = new MockBridgeService();
        mock.Expose<IAppService>(new FakeAppService());

        mock.Remove<IAppService>();

        Assert.False(mock.WasExposed<IAppService>());
    }

    [Fact]
    public void Reset_clears_all_state()
    {
        var mock = new MockBridgeService();
        mock.Expose<IAppService>(new FakeAppService());
        mock.SetupProxy<IUiController>(new FakeUiController());

        mock.Reset();

        Assert.Equal(0, mock.ExposedCount);
        Assert.Throws<InvalidOperationException>(() => mock.GetProxy<IUiController>());
    }

    [Fact]
    public void Dispose_makes_operations_throw()
    {
        var mock = new MockBridgeService();
        mock.Dispose();

        Assert.Throws<ObjectDisposedException>(() => mock.Expose<IAppService>(new FakeAppService()));
        Assert.Throws<ObjectDisposedException>(() => mock.GetProxy<IUiController>());
    }

    [Fact]
    public void ExposedCount_tracks_services()
    {
        var mock = new MockBridgeService();
        Assert.Equal(0, mock.ExposedCount);

        mock.Expose<IAppService>(new FakeAppService());
        Assert.Equal(1, mock.ExposedCount);
    }

    // Fake IUiController for mock proxy tests.
    private class FakeUiController : IUiController
    {
        public Task ShowNotification(string message, string? title = null) => Task.CompletedTask;
        public Task<bool> ConfirmDialog(string prompt) => Task.FromResult(true);
        public Task UpdateTheme(ThemeOptions options) => Task.CompletedTask;
    }
}
