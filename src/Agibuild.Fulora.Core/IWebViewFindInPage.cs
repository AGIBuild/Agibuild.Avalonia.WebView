namespace Agibuild.Fulora;

/// <summary>
/// Capability: incremental find-in-page search with highlight management.
/// </summary>
public interface IWebViewFindInPage
{
    /// <summary>
    /// Searches the current page for <paramref name="text"/>. Returns match count
    /// and active match index.
    /// </summary>
    Task<FindInPageEventArgs> FindInPageAsync(string text, FindInPageOptions? options = null);

    /// <summary>
    /// Clears find-in-page highlights and resets search state.
    /// </summary>
    Task StopFindInPageAsync(bool clearHighlights = true);
}
