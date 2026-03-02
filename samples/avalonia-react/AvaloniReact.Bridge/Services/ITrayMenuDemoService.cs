using Agibuild.Fulora;

namespace AvaloniReact.Bridge.Services;

/// <summary>
/// Demonstrates system tray and native menu integration via the Fulora bridge.
/// Exposed as a [JsExport] service so the React frontend can update tray/menu from JS.
/// </summary>
[JsExport]
public interface ITrayMenuDemoService
{
    /// <summary>
    /// Updates the system tray icon state (show/hide, tooltip, icon).
    /// </summary>
    Task<TrayUpdateResult> UpdateTray(TrayStateRequest request);

    /// <summary>
    /// Applies a native menu model (flat or nested items).
    /// </summary>
    Task<MenuApplyResult> ApplyMenu(MenuModelRequest request);
}

public sealed class TrayStateRequest
{
    public bool IsVisible { get; init; }
    public string? Tooltip { get; init; }
    public string? IconPath { get; init; }
}

public sealed class TrayUpdateResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
}

public sealed class MenuModelRequest
{
    public required MenuItemModel[] Items { get; init; }
}

public sealed class MenuItemModel
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public bool IsEnabled { get; init; } = true;
    public MenuItemModel[] Children { get; init; } = [];
}

public sealed class MenuApplyResult
{
    public bool Success { get; init; }
    public int AppliedItemCount { get; init; }
    public string? Error { get; init; }
}
