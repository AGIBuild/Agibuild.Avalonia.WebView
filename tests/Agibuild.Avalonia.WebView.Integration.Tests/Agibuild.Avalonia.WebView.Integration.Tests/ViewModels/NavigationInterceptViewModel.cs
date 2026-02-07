using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Agibuild.Avalonia.WebView.Integration.Tests.ViewModels;

/// <summary>
/// Demonstrates navigation interception: blocks in-page link clicks,
/// shows an inline confirmation overlay, and lets the user Allow / Block / redirect to a safe page.
/// </summary>
public partial class NavigationInterceptViewModel : ViewModelBase
{
    // ── Safe fallback page ──────────────────────────
    private const string SafePageUrl = "https://www.example.com";

    // ── Observable state ────────────────────────────
    [ObservableProperty] private string _log = string.Empty;
    [ObservableProperty] private string _interceptedUrl = string.Empty;
    [ObservableProperty] private bool _isInterceptDialogVisible;
    [ObservableProperty] private bool _isInterceptionEnabled = true;
    [ObservableProperty] private string _status = "Ready. Click a link in the page to test interception.";

    // ── Tracking to avoid re-intercept loops ────────
    private Uri? _allowedNavigationUri;

    /// <summary>
    /// Raised when the user has made a decision and the view should navigate.
    /// The Uri is null when the user chose "Block" (no navigation needed).
    /// </summary>
    public event Action<Uri?>? NavigationDecisionMade;

    // ── Test page HTML with styled links ────────────

    public string TestPageHtml => """
        <!DOCTYPE html>
        <html lang="en">
        <head>
        <meta charset="utf-8"/>
        <meta name="viewport" content="width=device-width, initial-scale=1"/>
        <style>
          * { box-sizing: border-box; margin: 0; padding: 0; }
          body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh; padding: 24px; color: #1a1a2e;
          }
          .container { max-width: 480px; margin: 0 auto; }
          h1 { color: #fff; font-size: 22px; margin-bottom: 6px; }
          .subtitle { color: rgba(255,255,255,0.8); font-size: 13px; margin-bottom: 24px; }
          .card {
            background: #fff; border-radius: 12px; padding: 20px;
            margin-bottom: 16px;
          }
          .card h2 { font-size: 15px; color: #64748b; margin-bottom: 12px; text-transform: uppercase; letter-spacing: 0.5px; }
          a {
            display: block; padding: 14px 16px; margin: 6px 0;
            border-radius: 8px; text-decoration: none; font-size: 15px;
            font-weight: 500; transition: all 0.2s;
          }
          a:active { transform: scale(0.98); }
          .link-ext { background: #EFF6FF; color: #2563EB; border: 1px solid #BFDBFE; }
          .link-social { background: #F0FDF4; color: #16A34A; border: 1px solid #BBF7D0; }
          .link-danger { background: #FEF2F2; color: #DC2626; border: 1px solid #FECACA; }
          .arrow { float: right; opacity: 0.5; }
          .info {
            background: rgba(255,255,255,0.15); border-radius: 8px; padding: 12px 14px;
            color: #fff; font-size: 12px; line-height: 1.5; margin-top: 16px;
          }
        </style>
        </head>
        <body>
          <div class="container">
            <h1>Navigation Intercept Demo</h1>
            <p class="subtitle">Tap any link below — it will be intercepted before navigating.</p>

            <div class="card">
              <h2>Search Engines</h2>
              <a class="link-ext" href="https://www.bing.com">Bing <span class="arrow">→</span></a>
              <a class="link-ext" href="https://www.google.com">Google <span class="arrow">→</span></a>
              <a class="link-ext" href="https://duckduckgo.com">DuckDuckGo <span class="arrow">→</span></a>
            </div>

            <div class="card">
              <h2>Social & News</h2>
              <a class="link-social" href="https://github.com">GitHub <span class="arrow">→</span></a>
              <a class="link-social" href="https://news.ycombinator.com">Hacker News <span class="arrow">→</span></a>
            </div>

            <div class="card">
              <h2>Potentially Blocked</h2>
              <a class="link-danger" href="https://malicious-example.test">Suspicious Site <span class="arrow">→</span></a>
              <a class="link-danger" href="https://phishing-demo.test">Phishing Demo <span class="arrow">→</span></a>
            </div>

            <div class="info">
              ℹ️ Every navigation from this page is intercepted. You choose whether to Allow, Block, or redirect to a safe page.
            </div>
          </div>
        </body>
        </html>
        """;

    // ── Called by the view when NavigationStarted fires ──

    /// <summary>
    /// Returns true if this navigation should be cancelled (intercepted).
    /// </summary>
    public bool ShouldIntercept(Uri requestUri)
    {
        // Never intercept about:blank or data: URIs (initial loads / HTML injection)
        if (requestUri.Scheme is "about" or "data")
            return false;

        // If interception is disabled, allow everything
        if (!IsInterceptionEnabled)
        {
            LogLine($"[Pass] {requestUri.AbsoluteUri}  (interception disabled)");
            return false;
        }

        // If this is a re-navigation we explicitly allowed, let it through
        if (_allowedNavigationUri is not null
            && _allowedNavigationUri.AbsoluteUri == requestUri.AbsoluteUri)
        {
            LogLine($"[Allowed] {requestUri.AbsoluteUri}");
            _allowedNavigationUri = null;
            return false;
        }

        // Intercept!
        LogLine($"[Intercepted] {requestUri.AbsoluteUri}");
        InterceptedUrl = requestUri.AbsoluteUri;
        IsInterceptDialogVisible = true;
        Status = $"Intercepted: {requestUri.Host}";
        return true;
    }

    // ── User decision commands ──────────────────────

    [RelayCommand]
    private void AllowNavigation()
    {
        var uri = ParseInterceptedUri();
        if (uri is null) return;

        _allowedNavigationUri = uri;
        IsInterceptDialogVisible = false;
        Status = $"Allowed → {uri.Host}";
        LogLine($"[Decision] ALLOW → {uri.AbsoluteUri}");
        NavigationDecisionMade?.Invoke(uri);
    }

    [RelayCommand]
    private void BlockNavigation()
    {
        IsInterceptDialogVisible = false;
        Status = "Blocked. Staying on current page.";
        LogLine($"[Decision] BLOCK ← {InterceptedUrl}");
        InterceptedUrl = string.Empty;
        NavigationDecisionMade?.Invoke(null);
    }

    [RelayCommand]
    private void GoToSafePage()
    {
        var safeUri = new Uri(SafePageUrl);
        _allowedNavigationUri = safeUri;
        IsInterceptDialogVisible = false;
        Status = $"Redirected → {safeUri.Host}";
        LogLine($"[Decision] SAFE PAGE → {safeUri.AbsoluteUri}  (original: {InterceptedUrl})");
        InterceptedUrl = string.Empty;
        NavigationDecisionMade?.Invoke(safeUri);
    }

    [RelayCommand]
    private void ClearLog() => Log = string.Empty;

    [RelayCommand]
    private void ReloadTestPage()
    {
        // Signal the view to reload the test HTML
        _allowedNavigationUri = null;
        Status = "Reloaded test page.";
        LogLine("[Action] Reload test page");
        NavigationDecisionMade?.Invoke(new Uri("about:reload-test-page"));
    }

    // ── Helpers ─────────────────────────────────────

    private Uri? ParseInterceptedUri()
    {
        if (string.IsNullOrWhiteSpace(InterceptedUrl)) return null;
        return Uri.TryCreate(InterceptedUrl, UriKind.Absolute, out var uri) ? uri : null;
    }

    public void LogLine(string msg)
    {
        var ts = DateTime.Now.ToString("HH:mm:ss.fff");
        Log += $"[{ts}] {msg}\n";
    }
}
