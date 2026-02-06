using Avalonia.Platform;
using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Adapters.Abstractions;

namespace Agibuild.Avalonia.WebView.Adapters.Gtk;

internal sealed class GtkWebViewAdapter : IWebViewAdapter
{
    public bool CanGoBack => false;
    public bool CanGoForward => false;

    public event EventHandler<NavigationCompletedEventArgs>? NavigationCompleted
    {
        add { }
        remove { }
    }

    public event EventHandler<NewWindowRequestedEventArgs>? NewWindowRequested
    {
        add { }
        remove { }
    }

    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived
    {
        add { }
        remove { }
    }

    public event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested
    {
        add { }
        remove { }
    }

    public event EventHandler<EnvironmentRequestedEventArgs>? EnvironmentRequested
    {
        add { }
        remove { }
    }

    public void Initialize(IWebViewAdapterHost host) => throw new PlatformNotSupportedException("WebView is not yet supported on Linux/GTK.");
    public void Attach(IPlatformHandle parentHandle) => throw new PlatformNotSupportedException("WebView is not yet supported on Linux/GTK.");
    public void Detach() => throw new PlatformNotSupportedException("WebView is not yet supported on Linux/GTK.");

    public Task NavigateAsync(Guid navigationId, Uri uri) => throw new PlatformNotSupportedException("WebView is not yet supported on Linux/GTK.");
    public Task NavigateToStringAsync(Guid navigationId, string html) => throw new PlatformNotSupportedException("WebView is not yet supported on Linux/GTK.");
    public Task<string?> InvokeScriptAsync(string script) => throw new PlatformNotSupportedException("WebView is not yet supported on Linux/GTK.");

    public bool GoBack(Guid navigationId) => throw new PlatformNotSupportedException("WebView is not yet supported on Linux/GTK.");
    public bool GoForward(Guid navigationId) => throw new PlatformNotSupportedException("WebView is not yet supported on Linux/GTK.");
    public bool Refresh(Guid navigationId) => throw new PlatformNotSupportedException("WebView is not yet supported on Linux/GTK.");
    public bool Stop() => throw new PlatformNotSupportedException("WebView is not yet supported on Linux/GTK.");
}
