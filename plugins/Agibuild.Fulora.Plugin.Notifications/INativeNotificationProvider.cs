namespace Agibuild.Fulora.Plugin.Notifications;

/// <summary>
/// Platform abstraction for showing native notifications.
/// Implementations can use OS-specific APIs (e.g., Windows Toast, macOS NSUserNotification).
/// </summary>
public interface INativeNotificationProvider
{
    /// <summary>Shows a notification and returns its assigned ID.</summary>
    Task<string> ShowNotification(string title, string body, NotificationOptions? options);
    /// <summary>Requests permission to show notifications.</summary>
    Task<bool> RequestPermission();
    /// <summary>Clears all notifications.</summary>
    Task ClearAll();
    /// <summary>Clears the notification with the specified ID.</summary>
    Task Clear(string notificationId);
}
