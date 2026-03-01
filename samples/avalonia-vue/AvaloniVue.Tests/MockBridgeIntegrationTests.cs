using Agibuild.Fulora;
using AvaloniVue.Bridge.Services;

namespace AvaloniVue.Tests;

public class MockBridgeIntegrationTests
{
    [Fact]
    public void All_JsExport_services_can_be_registered()
    {
        var mock = new MockBridgeService();

        mock.Expose<IAppShellService>(new AppShellService());
        mock.Expose<ISystemInfoService>(new SystemInfoService());
        mock.Expose<IChatService>(new ChatService());
        mock.Expose<IFileService>(new FileService());
        mock.Expose<ISettingsService>(new SettingsService());

        Assert.True(mock.WasExposed<IAppShellService>());
        Assert.True(mock.WasExposed<ISystemInfoService>());
        Assert.True(mock.WasExposed<IChatService>());
        Assert.True(mock.WasExposed<IFileService>());
        Assert.True(mock.WasExposed<ISettingsService>());
        Assert.Equal(5, mock.ExposedCount);
    }

    [Fact]
    public void JsImport_services_can_be_configured_as_proxies()
    {
        var mock = new MockBridgeService();

        mock.SetupProxy<IUiNotificationService>(new StubNotificationService());
        mock.SetupProxy<IThemeService>(new StubThemeService());

        Assert.NotNull(mock.GetProxy<IUiNotificationService>());
        Assert.NotNull(mock.GetProxy<IThemeService>());
    }

    private class StubNotificationService : IUiNotificationService
    {
        public Task ShowNotification(string message, string type) => Task.CompletedTask;
    }

    private class StubThemeService : IThemeService
    {
        public Task SetTheme(string theme) => Task.CompletedTask;
    }
}
