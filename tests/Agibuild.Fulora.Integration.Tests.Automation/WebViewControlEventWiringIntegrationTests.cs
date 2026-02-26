using Agibuild.Fulora;
using Agibuild.Fulora.Testing;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Agibuild.Fulora.Integration.Tests.Automation;

public sealed class WebViewControlEventWiringIntegrationTests
{
    [AvaloniaFact]
    public void ContextMenuRequested_subscribe_before_core_attach_is_replayed()
    {
        AvaloniaUiThreadRunner.Run(() =>
        {
            var dispatcher = new TestDispatcher();
            var adapter = MockWebViewAdapter.CreateWithContextMenu();
            using var core = new WebViewCore(adapter, dispatcher);
            var webView = new WebView();

            var fired = false;
            webView.ContextMenuRequested += (_, _) => fired = true;

            webView.TestOnlyAttachCore(core);
            webView.TestOnlySubscribeCoreEvents();

            ((MockWebViewAdapterWithContextMenu)adapter).RaiseContextMenu(new ContextMenuRequestedEventArgs { X = 1, Y = 2 });

            Assert.True(fired);
        });
    }

    [AvaloniaFact]
    public void ContextMenuRequested_unsubscribe_before_core_attach_is_honored()
    {
        AvaloniaUiThreadRunner.Run(() =>
        {
            var dispatcher = new TestDispatcher();
            var adapter = MockWebViewAdapter.CreateWithContextMenu();
            using var core = new WebViewCore(adapter, dispatcher);
            var webView = new WebView();

            var fired = false;
            EventHandler<ContextMenuRequestedEventArgs> handler = (_, _) => fired = true;
            webView.ContextMenuRequested += handler;
            webView.ContextMenuRequested -= handler;

            webView.TestOnlyAttachCore(core);
            webView.TestOnlySubscribeCoreEvents();

            ((MockWebViewAdapterWithContextMenu)adapter).RaiseContextMenu(new ContextMenuRequestedEventArgs { X = 1, Y = 2 });

            Assert.False(fired);
        });
    }

    [AvaloniaFact]
    public void ContextMenuRequested_unsubscribe_after_core_attach_is_honored()
    {
        AvaloniaUiThreadRunner.Run(() =>
        {
            var dispatcher = new TestDispatcher();
            var adapter = MockWebViewAdapter.CreateWithContextMenu();
            using var core = new WebViewCore(adapter, dispatcher);
            var webView = new WebView();

            var fired = false;
            EventHandler<ContextMenuRequestedEventArgs> handler = (_, _) => fired = true;
            webView.ContextMenuRequested += handler;

            webView.TestOnlyAttachCore(core);
            webView.TestOnlySubscribeCoreEvents();

            webView.ContextMenuRequested -= handler;
            ((MockWebViewAdapterWithContextMenu)adapter).RaiseContextMenu(new ContextMenuRequestedEventArgs { X = 11, Y = 22 });

            Assert.False(fired);
        });
    }

    [AvaloniaFact]
    public void ContextMenuRequested_rebinds_to_new_core_after_reattach()
    {
        AvaloniaUiThreadRunner.Run(() =>
        {
            var webView = new WebView();
            var firedCount = 0;
            webView.ContextMenuRequested += (_, _) => firedCount++;

            var dispatcher1 = new TestDispatcher();
            var adapter1 = MockWebViewAdapter.CreateWithContextMenu();
            using var core1 = new WebViewCore(adapter1, dispatcher1);

            webView.TestOnlyAttachCore(core1);
            webView.TestOnlySubscribeCoreEvents();
            ((MockWebViewAdapterWithContextMenu)adapter1).RaiseContextMenu(new ContextMenuRequestedEventArgs { X = 1, Y = 1 });

            webView.TestOnlyUnsubscribeCoreEvents();

            var dispatcher2 = new TestDispatcher();
            var adapter2 = MockWebViewAdapter.CreateWithContextMenu();
            using var core2 = new WebViewCore(adapter2, dispatcher2);

            webView.TestOnlyAttachCore(core2);
            webView.TestOnlySubscribeCoreEvents();

            ((MockWebViewAdapterWithContextMenu)adapter1).RaiseContextMenu(new ContextMenuRequestedEventArgs { X = 2, Y = 2 });
            ((MockWebViewAdapterWithContextMenu)adapter2).RaiseContextMenu(new ContextMenuRequestedEventArgs { X = 3, Y = 3 });

            Assert.Equal(2, firedCount);
        });
    }
}
