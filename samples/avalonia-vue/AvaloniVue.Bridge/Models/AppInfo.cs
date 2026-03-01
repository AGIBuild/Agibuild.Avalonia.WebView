namespace AvaloniVue.Bridge.Models;

/// <summary>Application metadata exposed to the frontend.</summary>
public record AppInfo(
    string Name,
    string Version,
    string Description);
