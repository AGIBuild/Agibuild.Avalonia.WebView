using System.Text.Json;
using Agibuild.Fulora;
using Agibuild.Fulora.Adapters.Abstractions;
using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed partial class CoverageGapTests
{
    [Fact]
    public void WebViewCookie_value_equality()
    {
        var expires = DateTimeOffset.UtcNow.AddDays(1);
        var a = new WebViewCookie("name", "value", ".example.com", "/", expires, true, false);
        var b = new WebViewCookie("name", "value", ".example.com", "/", expires, true, false);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void WebViewCookie_inequality_different_name()
    {
        var a = new WebViewCookie("name1", "value", ".example.com", "/", null, false, false);
        var b = new WebViewCookie("name2", "value", ".example.com", "/", null, false, false);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void WebViewCookie_nullable_Expires_equality()
    {
        var a = new WebViewCookie("n", "v", ".d", "/", null, false, false);
        var b = new WebViewCookie("n", "v", ".d", "/", null, false, false);

        Assert.Equal(a, b);
    }

    [Fact]
    public void WebViewCookie_nullable_Expires_inequality()
    {
        var a = new WebViewCookie("n", "v", ".d", "/", null, false, false);
        var b = new WebViewCookie("n", "v", ".d", "/", DateTimeOffset.UtcNow, false, false);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void WebViewCookie_ToString_contains_fields()
    {
        var cookie = new WebViewCookie("session", "abc123", ".example.com", "/path", null, true, true);

        var str = cookie.ToString();
        Assert.Contains("session", str);
        Assert.Contains("abc123", str);
        Assert.Contains(".example.com", str);
    }

    [Fact]
    public void WebViewCookie_deconstruction()
    {
        var cookie = new WebViewCookie("n", "v", "d", "p", null, true, false);

        var (name, value, domain, path, expires, isSecure, isHttpOnly) = cookie;
        Assert.Equal("n", name);
        Assert.Equal("v", value);
        Assert.Equal("d", domain);
        Assert.Equal("p", path);
        Assert.Null(expires);
        Assert.True(isSecure);
        Assert.False(isHttpOnly);
    }
}
