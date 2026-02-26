using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

[Collection("WebViewEnvironmentState")]
public sealed class WebViewEnvironmentTests : IDisposable
{
    public WebViewEnvironmentTests()
    {
        // Reset global state before each test.
        WebViewEnvironment.LoggerFactory = null;
    }

    public void Dispose()
    {
        WebViewEnvironment.LoggerFactory = null;
    }

    [Fact]
    public void Initialize_sets_LoggerFactory()
    {
        var factory = NullLoggerFactory.Instance;
        WebViewEnvironment.Initialize(factory);

        Assert.Same(factory, WebViewEnvironment.LoggerFactory);
    }

    [Fact]
    public void Initialize_does_not_overwrite_existing_LoggerFactory()
    {
        var first = NullLoggerFactory.Instance;
        var second = new LoggerFactory();

        WebViewEnvironment.Initialize(first);
        WebViewEnvironment.Initialize(second);

        Assert.Same(first, WebViewEnvironment.LoggerFactory);

        second.Dispose();
    }

    [Fact]
    public void LoggerFactory_is_null_by_default()
    {
        Assert.Null(WebViewEnvironment.LoggerFactory);
    }

    [Fact]
    public void Initialize_with_null_does_not_throw()
    {
        WebViewEnvironment.Initialize(null);

        Assert.Null(WebViewEnvironment.LoggerFactory);
    }
}
