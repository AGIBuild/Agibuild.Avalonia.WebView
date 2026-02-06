using Avalonia.Platform;
using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Adapters.Abstractions;

namespace Agibuild.Avalonia.WebView.Adapters.Windows;

internal sealed class WindowsWebViewAdapter : IWebViewAdapter
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

    public void Initialize(IWebViewAdapterHost host) => throw new PlatformNotSupportedException("WebView is not yet supported on Windows.");
    public void Attach(IPlatformHandle parentHandle) => throw new PlatformNotSupportedException("WebView is not yet supported on Windows.");
    public void Detach() => throw new PlatformNotSupportedException("WebView is not yet supported on Windows.");

    public Task NavigateAsync(Guid navigationId, Uri uri) => throw new PlatformNotSupportedException("WebView is not yet supported on Windows.");
    public Task NavigateToStringAsync(Guid navigationId, string html) => throw new PlatformNotSupportedException("WebView is not yet supported on Windows.");
    public Task<string?> InvokeScriptAsync(string script) => throw new PlatformNotSupportedException("WebView is not yet supported on Windows.");

    public bool GoBack(Guid navigationId) => throw new PlatformNotSupportedException("WebView is not yet supported on Windows.");
    public bool GoForward(Guid navigationId) => throw new PlatformNotSupportedException("WebView is not yet supported on Windows.");
    public bool Refresh(Guid navigationId) => throw new PlatformNotSupportedException("WebView is not yet supported on Windows.");
    public bool Stop() => throw new PlatformNotSupportedException("WebView is not yet supported on Windows.");
}
