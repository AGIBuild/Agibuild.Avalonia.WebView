using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;

namespace Agibuild.Fulora;

/// <summary>
/// An Avalonia panel that hosts the Bridge DevTools overlay, displaying real-time
/// bridge call logs, payloads, latency metrics, and error details.
/// <para>
/// Place this in a <see cref="Grid"/> alongside the main <see cref="WebView"/>
/// and call <see cref="Attach"/> to wire it up:
/// <code>
/// &lt;Grid&gt;
///   &lt;agw:WebView x:Name="WebView" /&gt;
///   &lt;agw:BridgeDevToolsOverlay x:Name="DevToolsOverlay"
///     VerticalAlignment="Bottom" Height="300" IsVisible="False" /&gt;
/// &lt;/Grid&gt;
/// </code>
/// </para>
/// </summary>
public sealed class BridgeDevToolsOverlay : ContentControl, IDisposable
{
    /// <summary>
    /// Default keyboard gesture for toggling the overlay: F12.
    /// </summary>
    public static readonly KeyGesture DefaultToggleGesture = new(Key.D, KeyModifiers.Control | KeyModifiers.Shift);

    private BridgeDevToolsService? _service;
    private WebView? _overlayWebView;
    private bool _attached;
    private bool _disposed;

    /// <summary>
    /// Attaches the overlay to a <see cref="WebView"/>, wiring the devtools tracer
    /// into its bridge and loading the overlay HTML.
    /// </summary>
    /// <param name="targetWebView">
    /// The WebView whose bridge calls will be traced.
    /// Must have its bridge enabled before calling this.
    /// </param>
    /// <param name="existingTracer">Optional existing tracer to chain (events forwarded to both).</param>
    public void Attach(WebView targetWebView, IBridgeTracer? existingTracer = null)
    {
        if (_attached) return;
        _attached = true;

        _service = new BridgeDevToolsService(existingTracer);
        targetWebView.BridgeTracer = _service.Tracer;

        _overlayWebView = new WebView
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        Content = _overlayWebView;

        _overlayWebView.Loaded += async (_, _) =>
        {
            try
            {
                var html = BridgeDevToolsService.GetOverlayHtml();
                await _overlayWebView.NavigateToStringAsync(html);

                _service.StartPushing(script => _overlayWebView.InvokeScriptAsync(script));
            }
            catch
            {
                // Overlay is optional — don't crash the app
            }
        };
    }

    /// <summary>The underlying tracer. Wire this into your bridge service.</summary>
    public IBridgeTracer? Tracer => _service?.Tracer;

    /// <summary>The underlying event collector for direct access.</summary>
    public IBridgeEventCollector? Collector => _service?.Collector;

    /// <summary>Toggles the overlay visibility.</summary>
    public void Toggle()
    {
        IsVisible = !IsVisible;
    }

    /// <summary>
    /// Registers a keyboard shortcut on the given <paramref name="root"/> control to toggle
    /// this overlay. Defaults to <see cref="DefaultToggleGesture"/> (F12).
    /// </summary>
    /// <param name="root">
    /// The control to attach the key binding to (typically the top-level window or panel).
    /// </param>
    /// <param name="gesture">
    /// Keyboard gesture that triggers the toggle. Pass <c>null</c> to use <see cref="DefaultToggleGesture"/>.
    /// </param>
    public void RegisterToggleShortcut(Control root, KeyGesture? gesture = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        gesture ??= DefaultToggleGesture;

        root.KeyBindings.Add(new KeyBinding
        {
            Gesture = gesture,
            Command = new ActionCommand(Toggle),
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _service?.Dispose();
        _service = null;
    }

    /// <summary>Lightweight command wrapping an <see cref="Action"/>.</summary>
    private sealed class ActionCommand : System.Windows.Input.ICommand
    {
        private readonly Action _action;
        public ActionCommand(Action action) => _action = action;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _action();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
