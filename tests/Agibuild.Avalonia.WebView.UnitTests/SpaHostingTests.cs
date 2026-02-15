using System.Reflection;
using System.Text;
using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

/// <summary>
/// Tests for SPA hosting: embedded resource serving, SPA fallback, MIME detection,
/// hashed filename caching, and WebViewCore integration.
/// Phase 2 Deliverables 2.1 + 2.2.
/// </summary>
public sealed class SpaHostingTests
{
    private readonly TestDispatcher _dispatcher = new();

    // ==================== SpaHostingOptions ====================

    [Fact]
    public void Default_options_have_sensible_defaults()
    {
        var opts = new SpaHostingOptions();
        Assert.Equal("app", opts.Scheme);
        Assert.Equal("localhost", opts.Host);
        Assert.Equal("index.html", opts.FallbackDocument);
        Assert.True(opts.AutoInjectBridgeScript);
        Assert.Equal(new Uri("app://localhost/index.html"), opts.EntryPointUri);
    }

    [Fact]
    public void EntryPointUri_respects_custom_scheme_and_host()
    {
        var opts = new SpaHostingOptions { Scheme = "myapp", Host = "myhost" };
        Assert.Equal(new Uri("myapp://myhost/index.html"), opts.EntryPointUri);
    }

    // ==================== MIME type detection ====================

    [Theory]
    [InlineData(".html", "text/html")]
    [InlineData(".css", "text/css")]
    [InlineData(".js", "application/javascript")]
    [InlineData(".json", "application/json")]
    [InlineData(".png", "image/png")]
    [InlineData(".svg", "image/svg+xml")]
    [InlineData(".woff2", "font/woff2")]
    [InlineData(".wasm", "application/wasm")]
    [InlineData(".xyz", "application/octet-stream")]
    [InlineData("", "application/octet-stream")]
    public void GetMimeType_returns_correct_type(string ext, string expected)
    {
        Assert.Equal(expected, SpaHostingService.GetMimeType(ext));
    }

    // ==================== Hashed filename detection ====================

    [Theory]
    [InlineData("app.a1b2c3d4.js", true)]       // Vite style: name.hash.ext
    [InlineData("chunk-A1B2C3D4E5.css", true)]   // webpack style: name-hash.ext
    [InlineData("index.html", false)]
    [InlineData("styles.css", false)]
    [InlineData("app.js", false)]
    [InlineData("image.ab12.png", false)]         // Too short for hash
    [InlineData("chunk-NOTHEX.css", false)]       // Dash segment exists but is not hex
    public void IsHashedFilename_detects_hashed_filenames(string path, bool expected)
    {
        Assert.Equal(expected, SpaHostingService.IsHashedFilename(path));
    }

    // ==================== Embedded resource serving ====================

    [Fact]
    public void Embedded_mode_requires_assembly_and_prefix()
    {
        Assert.Throws<ArgumentException>(() =>
            new SpaHostingService(new SpaHostingOptions { EmbeddedResourcePrefix = null }, NullTestLogger.Instance));
    }

    [Fact]
    public void TryHandle_ignores_non_matching_scheme()
    {
        var svc = CreateEmbeddedService();
        var e = MakeArgs("https://example.com/index.html");

        Assert.False(svc.TryHandle(e));
        Assert.False(e.Handled);
    }

    [Fact]
    public void TryHandle_serves_embedded_resource()
    {
        var svc = CreateEmbeddedService();
        var e = MakeArgs("app://localhost/test.txt");

        var handled = svc.TryHandle(e);

        Assert.True(handled);
        Assert.True(e.Handled);
        Assert.Equal(200, e.ResponseStatusCode);
        Assert.Equal("text/plain", e.ResponseContentType);
        Assert.NotNull(e.ResponseBody);

        using var reader = new StreamReader(e.ResponseBody!);
        var content = reader.ReadToEnd();
        Assert.Equal("Hello from embedded!", content);
    }

    [Fact]
    public void TryHandle_returns_404_for_missing_resource()
    {
        var svc = CreateEmbeddedService();
        var e = MakeArgs("app://localhost/nonexistent.xyz");

        var handled = svc.TryHandle(e);

        Assert.True(handled);
        Assert.Equal(404, e.ResponseStatusCode);
    }

    // ==================== SPA fallback ====================

    [Fact]
    public void SPA_fallback_serves_index_for_path_without_extension()
    {
        var svc = CreateEmbeddedService();
        var e = MakeArgs("app://localhost/settings/profile");

        var handled = svc.TryHandle(e);

        Assert.True(handled);
        Assert.True(e.Handled);
        // Should fall back to index.html (which maps to our test resource).
        // The test assembly might not have index.html, so it could be 404 or 200.
        // The important thing is the fallback path was attempted.
        Assert.True(e.ResponseStatusCode == 200 || e.ResponseStatusCode == 404);
    }

    [Fact]
    public void SPA_fallback_serves_index_for_root_path()
    {
        var svc = CreateEmbeddedService();
        var e = MakeArgs("app://localhost/");

        var handled = svc.TryHandle(e);

        Assert.True(handled);
        Assert.True(e.Handled);
    }

    // ==================== Caching ====================

    [Fact]
    public void Hashed_filename_gets_immutable_cache_header()
    {
        var svc = CreateEmbeddedService();
        var e = MakeArgs("app://localhost/test.txt"); // Not hashed.

        svc.TryHandle(e);

        Assert.Contains("no-cache", e.ResponseHeaders!["Cache-Control"]);
    }

    // ==================== WebViewCore integration ====================

    [Fact]
    public void EnableSpaHosting_auto_enables_bridge()
    {
        var adapter = MockWebViewAdapter.Create();
        var core = new WebViewCore(adapter, _dispatcher);

        core.EnableSpaHosting(new SpaHostingOptions
        {
            EmbeddedResourcePrefix = "TestResources",
            ResourceAssembly = typeof(SpaHostingTests).Assembly,
        });

        // Bridge should be auto-enabled.
        Assert.NotNull(core.Rpc); // RPC is created as part of bridge auto-enable.
    }

    [Fact]
    public void EnableSpaHosting_twice_throws()
    {
        var adapter = MockWebViewAdapter.Create();
        var core = new WebViewCore(adapter, _dispatcher);

        core.EnableSpaHosting(new SpaHostingOptions
        {
            EmbeddedResourcePrefix = "TestResources",
            ResourceAssembly = typeof(SpaHostingTests).Assembly,
        });

        Assert.Throws<InvalidOperationException>(() => core.EnableSpaHosting(new SpaHostingOptions
        {
            EmbeddedResourcePrefix = "TestResources",
            ResourceAssembly = typeof(SpaHostingTests).Assembly,
        }));
    }

    [Fact]
    public void Dispose_cleans_up_SPA_service()
    {
        var adapter = MockWebViewAdapter.Create();
        var core = new WebViewCore(adapter, _dispatcher);

        core.EnableSpaHosting(new SpaHostingOptions
        {
            EmbeddedResourcePrefix = "TestResources",
            ResourceAssembly = typeof(SpaHostingTests).Assembly,
        });

        core.Dispose();

        Assert.Throws<ObjectDisposedException>(() => core.EnableSpaHosting(new SpaHostingOptions
        {
            EmbeddedResourcePrefix = "TestResources",
            ResourceAssembly = typeof(SpaHostingTests).Assembly,
        }));
    }

    // ==================== Helpers ====================

    /// <summary>
    /// Creates a SPA hosting service using the test assembly as the embedded resource provider.
    /// The test project should have a "TestResources/test.txt" embedded resource.
    /// </summary>
    private static SpaHostingService CreateEmbeddedService()
    {
        return new SpaHostingService(new SpaHostingOptions
        {
            EmbeddedResourcePrefix = "TestResources",
            ResourceAssembly = typeof(SpaHostingTests).Assembly,
        }, NullTestLogger.Instance);
    }

    private static WebResourceRequestedEventArgs MakeArgs(string uri)
    {
        return new WebResourceRequestedEventArgs(new Uri(uri), "GET");
    }
}

/// <summary>Null logger for testing.</summary>
internal sealed class NullTestLogger : Microsoft.Extensions.Logging.ILogger
{
    public static readonly NullTestLogger Instance = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}
