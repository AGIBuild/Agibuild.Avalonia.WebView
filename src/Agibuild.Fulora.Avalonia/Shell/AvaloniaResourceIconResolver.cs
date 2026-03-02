using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Agibuild.Fulora.Shell;

/// <summary>
/// Resolves <c>avares://</c> URIs to Avalonia <see cref="WindowIcon"/> instances.
/// </summary>
internal sealed class AvaloniaResourceIconResolver : ITrayIconResolver
{
    private const string AvaresScheme = "avares://";

    public WindowIcon? Resolve(string? iconPath)
    {
        if (string.IsNullOrWhiteSpace(iconPath) ||
            !iconPath.StartsWith(AvaresScheme, StringComparison.OrdinalIgnoreCase))
            return null;

        try
        {
            var uri = new Uri(iconPath);
            using var stream = Avalonia.Platform.AssetLoader.Open(uri);
            return new WindowIcon(stream);
        }
        catch
        {
            return null;
        }
    }
}
