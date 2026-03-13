namespace Agibuild.Fulora.NativeOverlay;

/// <summary>
/// Platform-specific provider for creating and managing native overlay windows above WebView.
/// </summary>
public interface INativeOverlayProvider : IDisposable
{
    /// <summary>Creates the native overlay window as a child of the specified parent handle.</summary>
    void CreateOverlay(IntPtr parentHandle);

    /// <summary>Destroys the native overlay window and releases platform resources.</summary>
    void DestroyOverlay();

    /// <summary>Repositions and resizes the overlay to the given logical bounds, adjusted for DPI.</summary>
    void UpdateBounds(double x, double y, double width, double height, double dpiScale);

    /// <summary>Makes the overlay visible.</summary>
    void Show();

    /// <summary>Hides the overlay without destroying it.</summary>
    void Hide();

    /// <summary>Indicates whether the overlay is currently visible.</summary>
    bool IsVisible { get; }

    /// <summary>Platform handle of the overlay window.</summary>
    IntPtr OverlayHandle { get; }
}
