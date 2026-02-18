using System;
using Agibuild.Avalonia.WebView;

namespace Agibuild.Avalonia.WebView.Shell;

/// <summary>
/// Options for <see cref="WebViewShellExperience"/>. All features are opt-in.
/// </summary>
public sealed class WebViewShellExperienceOptions
{
    /// <summary>Optional policy for handling <see cref="IWebView.NewWindowRequested"/>.</summary>
    public IWebViewNewWindowPolicy? NewWindowPolicy { get; init; }
    /// <summary>Optional handler for download requests.</summary>
    public Action<IWebView, DownloadRequestedEventArgs>? DownloadHandler { get; init; }
    /// <summary>Optional handler for permission requests.</summary>
    public Action<IWebView, PermissionRequestedEventArgs>? PermissionHandler { get; init; }
}

/// <summary>
/// Policy for handling <see cref="IWebView.NewWindowRequested"/> in a host-controlled way.
/// </summary>
public interface IWebViewNewWindowPolicy
{
    /// <summary>Handles the new-window request.</summary>
    void Handle(IWebView webView, NewWindowRequestedEventArgs e);
}

/// <summary>
/// New-window policy that preserves the v1 fallback behavior (navigate in-place when unhandled).
/// </summary>
public sealed class NavigateInPlaceNewWindowPolicy : IWebViewNewWindowPolicy
{
    public void Handle(IWebView webView, NewWindowRequestedEventArgs e)
    {
        // Intentionally rely on v1 contract fallback: when Handled == false, WebView navigates in-place.
        // This avoids async navigation work inside an event handler.
        e.Handled = false;
    }
}

/// <summary>
/// New-window policy that delegates handling to a host-provided callback.
/// </summary>
public sealed class DelegateNewWindowPolicy : IWebViewNewWindowPolicy
{
    private readonly Action<IWebView, NewWindowRequestedEventArgs> _handler;

    /// <summary>Creates a delegating policy.</summary>
    public DelegateNewWindowPolicy(Action<IWebView, NewWindowRequestedEventArgs> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public void Handle(IWebView webView, NewWindowRequestedEventArgs e)
        => _handler(webView, e);
}

/// <summary>
/// Opt-in runtime helper that wires common host policies (new window, downloads, permissions)
/// onto an <see cref="IWebView"/> instance.
/// </summary>
public sealed class WebViewShellExperience : IDisposable
{
    private readonly IWebView _webView;
    private readonly WebViewShellExperienceOptions _options;
    private bool _disposed;

    /// <summary>Creates a new shell experience instance for the given WebView.</summary>
    public WebViewShellExperience(IWebView webView, WebViewShellExperienceOptions? options = null)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        _options = options ?? new WebViewShellExperienceOptions();

        if (_options.NewWindowPolicy is not null)
            _webView.NewWindowRequested += OnNewWindowRequested;
        if (_options.DownloadHandler is not null)
            _webView.DownloadRequested += OnDownloadRequested;
        if (_options.PermissionHandler is not null)
            _webView.PermissionRequested += OnPermissionRequested;
    }

    private void OnNewWindowRequested(object? sender, NewWindowRequestedEventArgs e)
    {
        if (_disposed) return;
        _options.NewWindowPolicy?.Handle(_webView, e);
    }

    private void OnDownloadRequested(object? sender, DownloadRequestedEventArgs e)
    {
        if (_disposed) return;
        _options.DownloadHandler?.Invoke(_webView, e);
    }

    private void OnPermissionRequested(object? sender, PermissionRequestedEventArgs e)
    {
        if (_disposed) return;
        _options.PermissionHandler?.Invoke(_webView, e);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _webView.NewWindowRequested -= OnNewWindowRequested;
        _webView.DownloadRequested -= OnDownloadRequested;
        _webView.PermissionRequested -= OnPermissionRequested;
    }
}

