using Agibuild.Fulora.Shell;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class ShellActivationOrchestrationTests
{
    [Fact]
    public void First_registration_becomes_primary_second_registration_is_secondary()
    {
        var appId = $"shell-app-{Guid.NewGuid():N}";
        using var primary = WebViewShellActivationCoordinator.Register(appId, (_, _) => Task.CompletedTask);
        using var secondary = WebViewShellActivationCoordinator.Register(appId, (_, _) => Task.CompletedTask);

        Assert.True(primary.IsPrimary);
        Assert.False(secondary.IsPrimary);
    }

    [Fact]
    public async Task Secondary_forward_is_delivered_to_primary_handler()
    {
        var appId = $"shell-app-{Guid.NewGuid():N}";
        WebViewShellActivationRequest? observed = null;

        using var primary = WebViewShellActivationCoordinator.Register(appId, (request, _) =>
        {
            observed = request;
            return Task.CompletedTask;
        });
        using var secondary = WebViewShellActivationCoordinator.Register(appId, (_, _) => Task.CompletedTask);

        var request = new WebViewShellActivationRequest(
            new Uri("myapp://host/path"),
            new Dictionary<string, string> { ["source"] = "secondary" });

        var result = await secondary.ForwardAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(WebViewShellActivationForwardStatus.Delivered, result.Status);
        Assert.NotNull(observed);
        Assert.Equal("myapp://host/path", observed!.DeepLinkUri.ToString());
        Assert.Equal("secondary", observed.Metadata["source"]);
    }

    [Fact]
    public async Task Forward_returns_no_active_primary_when_owner_is_not_present()
    {
        var appId = $"shell-app-{Guid.NewGuid():N}";
        var primary = WebViewShellActivationCoordinator.Register(appId, (_, _) => Task.CompletedTask);
        using var secondary = WebViewShellActivationCoordinator.Register(appId, (_, _) => Task.CompletedTask);
        primary.Dispose();

        var request = new WebViewShellActivationRequest(new Uri("myapp://host/no-primary"));
        var result = await secondary.ForwardAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(WebViewShellActivationForwardStatus.NoActivePrimary, result.Status);
        Assert.Equal("no-active-primary", result.Reason);
    }

    [Fact]
    public void Releasing_primary_allows_next_registration_to_take_primary()
    {
        var appId = $"shell-app-{Guid.NewGuid():N}";
        using (var primary = WebViewShellActivationCoordinator.Register(appId, (_, _) => Task.CompletedTask))
        {
            Assert.True(primary.IsPrimary);
        }

        using var next = WebViewShellActivationCoordinator.Register(appId, (_, _) => Task.CompletedTask);
        Assert.True(next.IsPrimary);
    }

    [Fact]
    public void Relative_deep_link_uri_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new WebViewShellActivationRequest(new Uri("/relative", UriKind.Relative)));
    }
}
