using System.Net;
using System.Text.Json;
using Agibuild.Fulora.Auth;
using Xunit;

namespace Agibuild.Fulora.Auth.OAuth.Tests;

public class OAuthPkceClientTests
{
    private static OAuthPkceOptions CreateOptions() => new()
    {
        Authority = "https://login.example.com/tenant",
        ClientId = "test-client-id",
        RedirectUri = "myapp://auth/callback",
        Scopes = ["openid", "profile", "offline_access"],
    };

    [Fact]
    public void Constructor_MissingAuthority_ThrowsArgument()
    {
        var options = CreateOptions();
        options.Authority = "";
        Assert.Throws<ArgumentException>(() =>
            new OAuthPkceClient(new HttpClient(), options));
    }

    [Fact]
    public void Constructor_MissingClientId_ThrowsArgument()
    {
        var options = CreateOptions();
        options.ClientId = "";
        Assert.Throws<ArgumentException>(() =>
            new OAuthPkceClient(new HttpClient(), options));
    }

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgument()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OAuthPkceClient(null!, CreateOptions()));
    }

    [Fact]
    public void BuildAuthorizationUrl_ContainsRequiredParams()
    {
        var client = new OAuthPkceClient(new HttpClient(), CreateOptions());
        var url = client.BuildAuthorizationUrl("test-challenge", "test-state");

        Assert.Contains("response_type=code", url);
        Assert.Contains("client_id=test-client-id", url);
        Assert.Contains("redirect_uri=", url);
        Assert.Contains("code_challenge=test-challenge", url);
        Assert.Contains("code_challenge_method=S256", url);
        Assert.Contains("state=test-state", url);
        Assert.Contains("scope=openid%20profile%20offline_access", url);
    }

    [Fact]
    public void BuildAuthorizationUrl_UsesDefaultEndpoint()
    {
        var client = new OAuthPkceClient(new HttpClient(), CreateOptions());
        var url = client.BuildAuthorizationUrl("challenge");

        Assert.StartsWith("https://login.example.com/tenant/oauth2/v2.0/authorize?", url);
    }

    [Fact]
    public void BuildAuthorizationUrl_UsesCustomEndpoint()
    {
        var options = CreateOptions();
        options.AuthorizationEndpoint = "https://custom.auth.com/authorize";
        var client = new OAuthPkceClient(new HttpClient(), options);

        var url = client.BuildAuthorizationUrl("challenge");
        Assert.StartsWith("https://custom.auth.com/authorize?", url);
    }

    [Fact]
    public void BuildAuthorizationUrl_NoState_OmitsStateParam()
    {
        var client = new OAuthPkceClient(new HttpClient(), CreateOptions());
        var url = client.BuildAuthorizationUrl("challenge");
        Assert.DoesNotContain("state=", url);
    }

    [Fact]
    public async Task ExchangeCodeAsync_Success_ReturnsTokens()
    {
        var tokenResponse = new OAuthTokenResponse
        {
            AccessToken = "access-123",
            RefreshToken = "refresh-456",
            ExpiresIn = 3600,
            TokenType = "Bearer",
        };

        var handler = new MockHttpHandler(HttpStatusCode.OK, JsonSerializer.Serialize(tokenResponse));
        var httpClient = new HttpClient(handler);
        var client = new OAuthPkceClient(httpClient, CreateOptions());

        var result = await client.ExchangeCodeAsync("auth-code", "code-verifier", TestContext.Current.CancellationToken);

        Assert.Equal("access-123", result.AccessToken);
        Assert.Equal("refresh-456", result.RefreshToken);
        Assert.Equal(3600, result.ExpiresIn);
    }

    [Fact]
    public async Task ExchangeCodeAsync_Error_ThrowsOAuthException()
    {
        var errorJson = """{"error":"invalid_grant","error_description":"Code expired"}""";
        var handler = new MockHttpHandler(HttpStatusCode.BadRequest, errorJson);
        var httpClient = new HttpClient(handler);
        var client = new OAuthPkceClient(httpClient, CreateOptions());

        var ex = await Assert.ThrowsAsync<OAuthException>(() =>
            client.ExchangeCodeAsync("bad-code", "verifier", TestContext.Current.CancellationToken));

        Assert.Equal("invalid_grant", ex.ErrorCode);
        Assert.Equal("Code expired", ex.ErrorDescription);
    }

    [Fact]
    public async Task ExchangeCodeAsync_NonJsonError_ThrowsOAuthException()
    {
        var handler = new MockHttpHandler(HttpStatusCode.InternalServerError, "Server Error");
        var httpClient = new HttpClient(handler);
        var client = new OAuthPkceClient(httpClient, CreateOptions());

        var ex = await Assert.ThrowsAsync<OAuthException>(() =>
            client.ExchangeCodeAsync("code", "verifier", TestContext.Current.CancellationToken));

        Assert.Contains("500", ex.Message);
    }

    [Fact]
    public async Task RefreshTokenAsync_Success_ReturnsNewTokens()
    {
        var tokenResponse = new OAuthTokenResponse
        {
            AccessToken = "new-access",
            RefreshToken = "new-refresh",
            ExpiresIn = 3600,
        };

        var handler = new MockHttpHandler(HttpStatusCode.OK, JsonSerializer.Serialize(tokenResponse));
        var httpClient = new HttpClient(handler);
        var client = new OAuthPkceClient(httpClient, CreateOptions());

        var result = await client.RefreshTokenAsync("old-refresh-token", TestContext.Current.CancellationToken);

        Assert.Equal("new-access", result.AccessToken);
        Assert.Equal("new-refresh", result.RefreshToken);
    }

    [Fact]
    public async Task RefreshTokenAsync_Error_ThrowsOAuthException()
    {
        var errorJson = """{"error":"invalid_grant","error_description":"Refresh token expired"}""";
        var handler = new MockHttpHandler(HttpStatusCode.BadRequest, errorJson);
        var httpClient = new HttpClient(handler);
        var client = new OAuthPkceClient(httpClient, CreateOptions());

        var ex = await Assert.ThrowsAsync<OAuthException>(() =>
            client.RefreshTokenAsync("expired-token", TestContext.Current.CancellationToken));

        Assert.Equal("invalid_grant", ex.ErrorCode);
    }

    [Fact]
    public async Task ExchangeCodeAsync_VerifiesRequestBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK,
            """{"access_token":"t","expires_in":100}""");
        var httpClient = new HttpClient(handler);
        var client = new OAuthPkceClient(httpClient, CreateOptions());

        await client.ExchangeCodeAsync("my-code", "my-verifier", TestContext.Current.CancellationToken);

        Assert.NotNull(handler.LastRequestContent);
        Assert.Contains("grant_type=authorization_code", handler.LastRequestContent);
        Assert.Contains("code=my-code", handler.LastRequestContent);
        Assert.Contains("code_verifier=my-verifier", handler.LastRequestContent);
        Assert.Contains("client_id=test-client-id", handler.LastRequestContent);
    }

    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseBody;

        public string? LastRequestContent { get; private set; }

        public MockHttpHandler(HttpStatusCode statusCode, string responseBody)
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content != null)
                LastRequestContent = await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }
}
