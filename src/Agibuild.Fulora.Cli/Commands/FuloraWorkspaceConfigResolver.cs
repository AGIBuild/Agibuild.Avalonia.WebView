using System.Text.Json;

namespace Agibuild.Fulora.Cli.Commands;

internal static class FuloraWorkspaceConfigResolver
{
    internal const string FileName = "fulora.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = true,
    };

    internal static LoadedFuloraWorkspaceConfig? Load(string startDirectory, IFileSystem? fileSystem = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startDirectory);
        fileSystem ??= RealFileSystem.Instance;

        var configPath = FindConfigPath(startDirectory, fileSystem);
        if (configPath is null)
            return null;

        var json = fileSystem.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize<FuloraWorkspaceConfig>(json, SerializerOptions);
        if (config is null)
            return null;

        var workspaceRoot = Path.GetDirectoryName(configPath)
            ?? throw new InvalidOperationException($"Could not determine workspace root for {configPath}.");

        return new LoadedFuloraWorkspaceConfig(configPath, workspaceRoot, config);
    }

    internal static void Save(string workspaceRoot, FuloraWorkspaceConfig config, IFileSystem? fileSystem = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);
        ArgumentNullException.ThrowIfNull(config);
        fileSystem ??= RealFileSystem.Instance;

        fileSystem.CreateDirectory(workspaceRoot);
        var configPath = Path.Combine(workspaceRoot, FileName);
        var json = JsonSerializer.Serialize(config, SerializerOptions);
        fileSystem.WriteAllText(configPath, json + Environment.NewLine);
    }

    internal static string? FindConfigPath(string startDirectory, IFileSystem? fileSystem = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startDirectory);
        fileSystem ??= RealFileSystem.Instance;

        var current = Path.GetFullPath(startDirectory);
        if (fileSystem.FileExists(current))
        {
            current = Path.GetDirectoryName(current)
                ?? throw new InvalidOperationException($"Could not determine directory for {startDirectory}.");
        }

        while (true)
        {
            var candidate = Path.Combine(current, FileName);
            if (fileSystem.FileExists(candidate))
                return candidate;

            var parent = Path.GetDirectoryName(current);
            if (parent is null)
                return null;

            current = parent;
        }
    }

    internal static string? ResolvePath(string workspaceRoot, string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
            return null;

        return Path.GetFullPath(Path.Combine(workspaceRoot, configuredPath));
    }

    internal static string MakeRelativePath(string workspaceRoot, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var relative = Path.GetRelativePath(Path.GetFullPath(workspaceRoot), Path.GetFullPath(path))
            .Replace('\\', '/');

        if (relative is ".")
            return "./";

        return relative.StartsWith(".", StringComparison.Ordinal) ? relative : $"./{relative}";
    }
}
