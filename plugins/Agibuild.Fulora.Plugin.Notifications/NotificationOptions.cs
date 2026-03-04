namespace Agibuild.Fulora.Plugin.Notifications;

/// <summary>
/// Options for notification display.
/// </summary>
public sealed class NotificationOptions
{
    /// <summary>Optional icon identifier for the notification.</summary>
    public string? Icon { get; init; }
    /// <summary>Optional tag for grouping or replacing notifications.</summary>
    public string? Tag { get; init; }
    /// <summary>When true, suppresses sound for the notification.</summary>
    public bool Silent { get; init; }
}
