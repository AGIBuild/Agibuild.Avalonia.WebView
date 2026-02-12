using Agibuild.Avalonia.WebView;
using HybridApp.Bridge;
using Xunit;

namespace HybridApp.Tests;

public class GreeterServiceTests
{
    [Fact]
    public async Task Greet_returns_hello_message()
    {
        var svc = new GreeterServiceImpl();
        var result = await svc.Greet("World");
        Assert.Contains("World", result);
    }

    [Fact]
    public void Bridge_can_expose_greeter_via_mock()
    {
        var mock = new MockBridgeService();
        mock.Expose<IGreeterService>(new GreeterServiceImpl());

        Assert.True(mock.WasExposed<IGreeterService>());
    }

    [Fact]
    public void Bridge_proxy_can_be_setup_for_notification()
    {
        var mock = new MockBridgeService();
        // Create a simple mock for the JS-side service
        mock.SetupProxy<INotificationService>(
            new NotificationServiceStub());

        var proxy = mock.GetProxy<INotificationService>();
        Assert.NotNull(proxy);
    }

    private sealed class NotificationServiceStub : INotificationService
    {
        public Task ShowNotification(string message) => Task.CompletedTask;
    }
}
