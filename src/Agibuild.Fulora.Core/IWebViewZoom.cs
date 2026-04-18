namespace Agibuild.Fulora;

/// <summary>
/// Capability: read and adjust the page zoom factor. The zoom factor is clamped
/// to the range [0.25, 5.0]; values outside that range are silently adjusted.
/// </summary>
/// <remarks>
/// The existing <c>ZoomFactorChanged</c> event currently lives on the concrete
/// <c>WebViewCore</c> type rather than on the public interface; hoisting it is
/// a breaking change and is tracked for v2.0.
/// </remarks>
public interface IWebViewZoom
{
    /// <summary>Gets the current zoom factor (1.0 = 100%).</summary>
    Task<double> GetZoomFactorAsync();

    /// <summary>Sets the zoom factor (clamped to [0.25, 5.0]).</summary>
    Task SetZoomFactorAsync(double zoomFactor);
}
