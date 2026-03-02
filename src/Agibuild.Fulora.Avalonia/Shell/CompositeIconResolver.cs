using System.Collections.Generic;
using Avalonia.Controls;

namespace Agibuild.Fulora.Shell;

/// <summary>
/// Chains multiple <see cref="ITrayIconResolver"/> instances and returns the first non-null result.
/// </summary>
internal sealed class CompositeIconResolver : ITrayIconResolver
{
    private readonly IReadOnlyList<ITrayIconResolver> _resolvers;

    public CompositeIconResolver(IReadOnlyList<ITrayIconResolver> resolvers)
    {
        _resolvers = resolvers;
    }

    /// <summary>
    /// Creates a default composite resolver with Avalonia resource and file path resolvers.
    /// </summary>
    public static CompositeIconResolver CreateDefault()
        => new([new AvaloniaResourceIconResolver(), new FilePathIconResolver()]);

    public WindowIcon? Resolve(string? iconPath)
    {
        foreach (var resolver in _resolvers)
        {
            var icon = resolver.Resolve(iconPath);
            if (icon is not null)
                return icon;
        }

        return null;
    }
}
