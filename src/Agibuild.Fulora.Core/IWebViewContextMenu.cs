namespace Agibuild.Fulora;

/// <summary>
/// Capability: observe and optionally suppress the platform context menu
/// (right-click, long-press).
/// </summary>
public interface IWebViewContextMenu
{
    /// <summary>
    /// Raised when the user triggers a context menu. Setting
    /// <see cref="ContextMenuRequestedEventArgs.Handled"/> suppresses the default menu.
    /// </summary>
    event EventHandler<ContextMenuRequestedEventArgs>? ContextMenuRequested;
}
