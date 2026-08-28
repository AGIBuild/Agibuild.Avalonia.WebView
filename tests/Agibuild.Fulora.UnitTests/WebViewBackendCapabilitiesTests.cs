using Agibuild.Fulora.Adapters.Abstractions;
using Agibuild.Fulora.Adapters.Gtk;
using Agibuild.Fulora.Adapters.MacOS;
using Agibuild.Fulora.Adapters.Windows;
using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class WebViewBackendCapabilitiesTests
{
    [Fact]
    public async Task Snapshots_are_immutable_and_comparable()
    {
        var none = WebViewBackendCapabilities.None;
        var dragOnly = new WebViewBackendCapabilities(new ShapeOnlyDragDropAdapter(), null);
        var asyncOnly = new WebViewBackendCapabilities(null, MockWebViewAdapter.CreateWithPreload());
        var both = new WebViewBackendCapabilities(dragOnly.DragDrop, asyncOnly.AsyncPreloadScript);

        Assert.Null(none.DragDrop);
        Assert.Null(none.AsyncPreloadScript);
        Assert.NotNull(dragOnly.DragDrop);
        Assert.Null(dragOnly.AsyncPreloadScript);
        Assert.Null(asyncOnly.DragDrop);
        Assert.NotNull(asyncOnly.AsyncPreloadScript);
        Assert.NotNull(both.DragDrop);
        Assert.NotNull(both.AsyncPreloadScript);

        Assert.Equal(none, default(WebViewBackendCapabilities));
        Assert.Equal(dragOnly, dragOnly with { });
        Assert.NotEqual(dragOnly, asyncOnly);
    }

    [Fact]
    public async Task Explicit_null_declaration_wins_over_implemented_type_shape()
    {
        var adapter = new ShapeOnlyDragDropAdapter();
        Assert.IsAssignableFrom<IDragDropAdapter>(adapter);
        Assert.Null(adapter.BackendCapabilities.DragDrop);

        using var runtime = new WebViewCoreFeatureRuntime(WebViewCoreTestContext.Create(adapter));
        Assert.False(runtime.HasDragDropSupport);
    }

    [Fact]
    public async Task Declared_drag_drop_facet_is_the_instance_wired_by_runtime()
    {
        var adapter = MockWebViewAdapter.CreateWithDragDrop();
        using var runtime = new WebViewCoreFeatureRuntime(WebViewCoreTestContext.Create(adapter));

        Assert.True(runtime.HasDragDropSupport);
        Assert.Same(adapter, adapter.BackendCapabilities.DragDrop);
        Assert.Same(adapter, WebViewCoreTestContext.Create(adapter).Capabilities.DragDrop);
    }

    [Fact]
    public async Task Declared_async_preload_facet_is_invoked_by_runtime()
    {
        var adapter = MockWebViewAdapter.CreateWithPreload();
        using var runtime = new WebViewCoreFeatureRuntime(WebViewCoreTestContext.Create(adapter));

        await runtime.AddPreloadScriptAsync("console.log(1)");

        Assert.Empty(adapter.SyncAddedScripts);
        Assert.Equal(["console.log(1)"], adapter.AsyncAddedScripts);
    }

    [Fact]
    public void BackendCapabilities_is_read_once_per_WebViewCore()
    {
        var adapter = new CountingCapabilitiesAdapter();
        using var core = new WebViewCore(adapter, new TestDispatcher());

        Assert.Equal(1, adapter.ReadCount);
        _ = adapter.BackendCapabilities;
        Assert.Equal(2, adapter.ReadCount);
    }

    [Fact]
    public void Platform_adapters_declare_verified_optional_facets()
    {
        IWebViewAdapter gtk = new GtkWebViewAdapter();
        Assert.Same(gtk, gtk.BackendCapabilities.DragDrop);
        Assert.Null(gtk.BackendCapabilities.AsyncPreloadScript);

        if (OperatingSystem.IsWindows())
        {
            IWebViewAdapter windows = new WindowsWebViewAdapter();
            Assert.Same(windows, windows.BackendCapabilities.DragDrop);
            Assert.Same(windows, windows.BackendCapabilities.AsyncPreloadScript);
        }

        if (OperatingSystem.IsMacOS())
        {
            IWebViewAdapter mac = new MacOSWebViewAdapter();
            Assert.Same(mac, mac.BackendCapabilities.DragDrop);
            Assert.Null(mac.BackendCapabilities.AsyncPreloadScript);
        }
    }

    private sealed class ShapeOnlyDragDropAdapter : StubWebViewAdapter, IDragDropAdapter
    {
        public override WebViewBackendCapabilities BackendCapabilities => WebViewBackendCapabilities.None;

        public event EventHandler<DragEventArgs>? DragEntered
        {
            add { }
            remove { }
        }

        public event EventHandler<DragEventArgs>? DragOver
        {
            add { }
            remove { }
        }

        public event EventHandler<EventArgs>? DragLeft
        {
            add { }
            remove { }
        }

        public event EventHandler<DropEventArgs>? DropCompleted
        {
            add { }
            remove { }
        }
    }

    private sealed class CountingCapabilitiesAdapter : StubWebViewAdapter
    {
        public int ReadCount { get; private set; }

        public override WebViewBackendCapabilities BackendCapabilities
        {
            get
            {
                ReadCount++;
                return WebViewBackendCapabilities.None;
            }
        }
    }
}
