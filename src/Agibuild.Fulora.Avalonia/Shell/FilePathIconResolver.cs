using System.IO;
using Avalonia.Controls;

namespace Agibuild.Fulora.Shell;

/// <summary>
/// Resolves local file system paths to Avalonia <see cref="WindowIcon"/> instances.
/// </summary>
internal sealed class FilePathIconResolver : ITrayIconResolver
{
    public WindowIcon? Resolve(string? iconPath)
    {
        if (string.IsNullOrWhiteSpace(iconPath))
            return null;

        if (!File.Exists(iconPath))
            return null;

        try
        {
            using var stream = File.OpenRead(iconPath);
            return new WindowIcon(stream);
        }
        catch
        {
            return null;
        }
    }
}
