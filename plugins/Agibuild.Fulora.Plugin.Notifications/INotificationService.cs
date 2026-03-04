using Agibuild.Fulora;

namespace Agibuild.Fulora.Plugin.Notifications;

/// <summary>
/// Bridge service for showing native/system notifications.
/// </summary>
[JsExport]
public interface INotificationService
{
    /// <summary>Shows a notification and returns its assigned ID.</summary>
    Task<string> Show(string title, string body, NotificationOptions? options = null);
    /// <summary>Requests permission to show notifications.</summary>
    Task<bool> RequestPermission();
    /// <summary>Clears all notifications.</summary>
    Task ClearAll();
    /// <summary>Clears the notification with the specified ID.</summary>
    Task Clear(string notificationId);
}
