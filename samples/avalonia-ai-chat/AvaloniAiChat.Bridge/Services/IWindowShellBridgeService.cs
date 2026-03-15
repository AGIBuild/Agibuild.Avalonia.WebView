using Agibuild.Fulora;

namespace AvaloniAiChat.Bridge.Services;

/// <summary>
/// Bridge contract for desktop window shell operations.
/// </summary>
[JsExport]
public interface IWindowShellBridgeService
{
    /// <summary>
    /// Gets current window shell state.
    /// </summary>
    Task<WindowShellState> GetWindowShellState();

    /// <summary>
    /// Updates window shell settings and returns updated state.
    /// </summary>
    /// <param name="settings">Requested shell settings.</param>
    Task<WindowShellState> UpdateWindowShellSettings(WindowShellSettings settings);

    /// <summary>
    /// Streams window shell state updates.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Window shell state stream.</returns>
    IAsyncEnumerable<WindowShellState> StreamWindowShellState(CancellationToken cancellationToken = default);
}
