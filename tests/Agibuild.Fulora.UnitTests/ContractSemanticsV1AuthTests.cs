using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class ContractSemanticsV1AuthTests
{
    [Fact]
    public async Task Auth_CallbackUriRequired_ThrowsArgumentException()
    {
        var inner = new FakeAuthBroker();
        var broker = new WebAuthBrokerWithSemantics(inner);

        await Assert.ThrowsAsync<ArgumentException>(() => broker.AuthenticateAsync(new DummyWindow(), new AuthOptions()));
    }

    [Fact]
    public async Task Auth_StrictCallbackMatch_IgnoresQueryAndFragment()
    {
        var inner = new FakeAuthBroker
        {
            Result = new WebAuthResult
            {
                Status = WebAuthStatus.Success,
                CallbackUri = new Uri("myapp://auth/callback?code=123#frag")
            }
        };
        var broker = new WebAuthBrokerWithSemantics(inner);

        var options = new AuthOptions { CallbackUri = new Uri("myapp://auth/callback") };
        var result = await broker.AuthenticateAsync(new DummyWindow(), options);

        Assert.Equal(WebAuthStatus.Success, result.Status);
    }

    [Fact]
    public async Task Auth_StrictCallbackMismatch_DowngradesToError()
    {
        var inner = new FakeAuthBroker
        {
            Result = new WebAuthResult
            {
                Status = WebAuthStatus.Success,
                CallbackUri = new Uri("myapp://auth/other")
            }
        };
        var broker = new WebAuthBrokerWithSemantics(inner);

        var options = new AuthOptions { CallbackUri = new Uri("myapp://auth/callback") };
        var result = await broker.AuthenticateAsync(new DummyWindow(), options);

        Assert.Equal(WebAuthStatus.Error, result.Status);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task Auth_EphemeralDefault_IsRequestedByCore()
    {
        var inner = new FakeAuthBroker
        {
            Result = new WebAuthResult { Status = WebAuthStatus.UserCancel }
        };
        var broker = new WebAuthBrokerWithSemantics(inner);

        var options = new AuthOptions
        {
            CallbackUri = new Uri("myapp://auth/callback"),
            UseEphemeralSession = false
        };

        _ = await broker.AuthenticateAsync(new DummyWindow(), options);

        Assert.NotNull(inner.ReceivedOptions);
        Assert.True(inner.ReceivedOptions!.UseEphemeralSession);
    }

    [Fact]
    public async Task Auth_UserCancel_ReturnsCanceledResult()
    {
        var inner = new FakeAuthBroker
        {
            Result = new WebAuthResult { Status = WebAuthStatus.UserCancel }
        };
        var broker = new WebAuthBrokerWithSemantics(inner);

        var result = await broker.AuthenticateAsync(new DummyWindow(), new AuthOptions { CallbackUri = new Uri("myapp://auth/callback") });

        Assert.Equal(WebAuthStatus.UserCancel, result.Status);
    }

    [Fact]
    public async Task Auth_Timeout_ReturnsTimeoutResult()
    {
        var inner = new FakeAuthBroker
        {
            Result = new WebAuthResult { Status = WebAuthStatus.Timeout }
        };
        var broker = new WebAuthBrokerWithSemantics(inner);

        var result = await broker.AuthenticateAsync(new DummyWindow(), new AuthOptions { CallbackUri = new Uri("myapp://auth/callback") });

        Assert.Equal(WebAuthStatus.Timeout, result.Status);
    }

    private sealed class DummyWindow : ITopLevelWindow
    {
        public INativeHandle? PlatformHandle => null;
    }

    private sealed class FakeAuthBroker : IWebAuthBroker
    {
        public AuthOptions? ReceivedOptions { get; private set; }
        public WebAuthResult Result { get; init; } = new WebAuthResult { Status = WebAuthStatus.Error };

        public Task<WebAuthResult> AuthenticateAsync(ITopLevelWindow owner, AuthOptions options)
        {
            ReceivedOptions = options;
            return Task.FromResult(Result);
        }
    }
}

