using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Agibuild.Avalonia.WebView;

/// <summary>
/// Utility for normalizing JavaScript execution results returned by browser engines.
/// WebView2's <c>ExecuteScriptAsync</c> returns JSON-encoded strings
/// (e.g. <c>"null"</c> for null/undefined, <c>"\"hello\""</c> for <c>"hello"</c>).
/// This helper decodes such values to raw strings expected by the V1 adapter contract.
/// </summary>
public static class ScriptResultHelper
{
    /// <summary>
    /// Normalizes a JSON-encoded script result to a raw string value.
    /// </summary>
    /// <param name="jsonResult">The JSON-encoded result from the browser engine, or <c>null</c>.</param>
    /// <returns>
    /// <c>null</c> when the result represents JavaScript null/undefined;
    /// otherwise the decoded raw string value.
    /// </returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Deserializing to System.String is always safe for trimming.")]
    public static string? NormalizeJsonResult(string? jsonResult)
    {
        if (jsonResult is null || string.Equals(jsonResult, "null", StringComparison.Ordinal))
        {
            return null;
        }

        // JSON-encoded strings are surrounded by double quotes and may contain escape sequences.
        // Deserialize to get the raw string value.
        if (jsonResult.Length >= 2 && jsonResult[0] == '"' && jsonResult[^1] == '"')
        {
            return JsonSerializer.Deserialize<string>(jsonResult);
        }

        // Numbers, booleans, etc. — return as-is.
        return jsonResult;
    }
}
