using System.Reflection;
using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Testing;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Automation;

public sealed class WebViewControlEventWiringIntegrationTests
{
    [AvaloniaFact]
    public void ContextMenuRequested_subscribe_before_core_attach_is_replayed()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.CreateWithContextMenu();
        using var core = new WebViewCore(adapter, dispatcher);
        var webView = new WebView();

        var fired = false;
        webView.ContextMenuRequested += (_, _) => fired = true;

        SetPrivateField(webView, "_core", core);
        InvokePrivate(webView, "SubscribeCoreEvents");

        ((MockWebViewAdapterWithContextMenu)adapter).RaiseContextMenu(new ContextMenuRequestedEventArgs { X = 1, Y = 2 });

        Assert.True(fired);
    }

    [AvaloniaFact]
    public void ContextMenuRequested_unsubscribe_before_core_attach_is_honored()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.CreateWithContextMenu();
        using var core = new WebViewCore(adapter, dispatcher);
        var webView = new WebView();

        var fired = false;
        EventHandler<ContextMenuRequestedEventArgs> handler = (_, _) => fired = true;
        webView.ContextMenuRequested += handler;
        webView.ContextMenuRequested -= handler;

        SetPrivateField(webView, "_core", core);
        InvokePrivate(webView, "SubscribeCoreEvents");

        ((MockWebViewAdapterWithContextMenu)adapter).RaiseContextMenu(new ContextMenuRequestedEventArgs { X = 1, Y = 2 });

        Assert.False(fired);
    }

    [AvaloniaFact]
    public void ContextMenuRequested_unsubscribe_after_core_attach_is_honored()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.CreateWithContextMenu();
        using var core = new WebViewCore(adapter, dispatcher);
        var webView = new WebView();

        var fired = false;
        EventHandler<ContextMenuRequestedEventArgs> handler = (_, _) => fired = true;
        webView.ContextMenuRequested += handler;

        SetPrivateField(webView, "_core", core);
        InvokePrivate(webView, "SubscribeCoreEvents");

        webView.ContextMenuRequested -= handler;
        ((MockWebViewAdapterWithContextMenu)adapter).RaiseContextMenu(new ContextMenuRequestedEventArgs { X = 11, Y = 22 });

        Assert.False(fired);
    }

    [AvaloniaFact]
    public void ContextMenuRequested_rebinds_to_new_core_after_reattach()
    {
        var webView = new WebView();
        var firedCount = 0;
        webView.ContextMenuRequested += (_, _) => firedCount++;

        var dispatcher1 = new TestDispatcher();
        var adapter1 = MockWebViewAdapter.CreateWithContextMenu();
        using var core1 = new WebViewCore(adapter1, dispatcher1);

        SetPrivateField(webView, "_core", core1);
        InvokePrivate(webView, "SubscribeCoreEvents");
        ((MockWebViewAdapterWithContextMenu)adapter1).RaiseContextMenu(new ContextMenuRequestedEventArgs { X = 1, Y = 1 });

        InvokePrivate(webView, "UnsubscribeCoreEvents");

        var dispatcher2 = new TestDispatcher();
        var adapter2 = MockWebViewAdapter.CreateWithContextMenu();
        using var core2 = new WebViewCore(adapter2, dispatcher2);

        SetPrivateField(webView, "_core", core2);
        InvokePrivate(webView, "SubscribeCoreEvents");

        ((MockWebViewAdapterWithContextMenu)adapter1).RaiseContextMenu(new ContextMenuRequestedEventArgs { X = 2, Y = 2 });
        ((MockWebViewAdapterWithContextMenu)adapter2).RaiseContextMenu(new ContextMenuRequestedEventArgs { X = 3, Y = 3 });

        Assert.Equal(2, firedCount);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(target, value);
    }

    private static void InvokePrivate(object target, string methodName)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(target, null);
    }
}
