using Avalonia.Controls;

namespace Agibuild.Fulora.Shell;

/// <summary>
/// Resolves a tray icon path to an Avalonia <see cref="WindowIcon"/>.
/// </summary>
public interface ITrayIconResolver
{
    /// <summary>
    /// Attempts to resolve the given icon path to a <see cref="WindowIcon"/>.
    /// Returns <c>null</c> when this resolver cannot handle the path.
    /// </summary>
    WindowIcon? Resolve(string? iconPath);
}
