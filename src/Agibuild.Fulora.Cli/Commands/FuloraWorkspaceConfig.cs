using System.Text.Json.Serialization;

namespace Agibuild.Fulora.Cli.Commands;

internal sealed class FuloraWorkspaceConfig
{
    [JsonPropertyName("web")]
    public WebSection? Web { get; set; }

    [JsonPropertyName("bridge")]
    public BridgeSection? Bridge { get; set; }

    [JsonPropertyName("desktop")]
    public DesktopSection? Desktop { get; set; }

    internal sealed class WebSection
    {
        [JsonPropertyName("root")]
        public string? Root { get; set; }

        [JsonPropertyName("command")]
        public string? Command { get; set; }

        [JsonPropertyName("devServerUrl")]
        public string? DevServerUrl { get; set; }

        [JsonPropertyName("generatedDir")]
        public string? GeneratedDir { get; set; }
    }

    internal sealed class BridgeSection
    {
        [JsonPropertyName("project")]
        public string? Project { get; set; }
    }

    internal sealed class DesktopSection
    {
        [JsonPropertyName("project")]
        public string? Project { get; set; }
    }
}

internal sealed class LoadedFuloraWorkspaceConfig
{
    public LoadedFuloraWorkspaceConfig(string configPath, string workspaceRoot, FuloraWorkspaceConfig config)
    {
        ConfigPath = configPath;
        WorkspaceRoot = workspaceRoot;
        Config = config;
    }

    public string ConfigPath { get; }

    public string WorkspaceRoot { get; }

    public FuloraWorkspaceConfig Config { get; }

    public string? ResolveWebRoot()
        => FuloraWorkspaceConfigResolver.ResolvePath(WorkspaceRoot, Config.Web?.Root);

    public string? ResolveGeneratedDir()
        => FuloraWorkspaceConfigResolver.ResolvePath(WorkspaceRoot, Config.Web?.GeneratedDir);

    public string? ResolveBridgeProject()
        => FuloraWorkspaceConfigResolver.ResolvePath(WorkspaceRoot, Config.Bridge?.Project);

    public string? ResolveDesktopProject()
        => FuloraWorkspaceConfigResolver.ResolvePath(WorkspaceRoot, Config.Desktop?.Project);
}
