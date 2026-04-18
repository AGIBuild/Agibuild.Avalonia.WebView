namespace Agibuild.Fulora;

/// <summary>
/// Capability: capture a full-page PNG screenshot of the current WebView content.
/// </summary>
public interface IWebViewScreenshot
{
    /// <summary>Captures the current viewport as PNG bytes.</summary>
    Task<byte[]> CaptureScreenshotAsync();
}
