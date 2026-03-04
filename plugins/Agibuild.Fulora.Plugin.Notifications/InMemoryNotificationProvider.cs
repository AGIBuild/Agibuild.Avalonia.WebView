namespace Agibuild.Fulora.Plugin.Notifications;

/// <summary>
/// In-memory notification provider for testing.
/// Tracks notifications by ID without showing them on the system.
/// </summary>
public sealed class InMemoryNotificationProvider : INativeNotificationProvider
{
    private readonly Dictionary<string, (string Title, string Body, NotificationOptions? Options)> _notifications = new();
    private int _idCounter;

    /// <summary>Shows a notification and returns its assigned ID.</summary>
    public Task<string> ShowNotification(string title, string body, NotificationOptions? options)
    {
        var id = $"notif-{Interlocked.Increment(ref _idCounter)}";
        _notifications[id] = (title, body, options);
        return Task.FromResult(id);
    }

    /// <summary>Requests notification permission; always returns true for in-memory provider.</summary>
    public Task<bool> RequestPermission()
    {
        return Task.FromResult(true);
    }

    /// <summary>Clears all tracked notifications.</summary>
    public Task ClearAll()
    {
        _notifications.Clear();
        return Task.CompletedTask;
    }

    /// <summary>Clears the notification with the specified ID.</summary>
    public Task Clear(string notificationId)
    {
        _notifications.Remove(notificationId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns the count of tracked notifications (for testing).
    /// </summary>
    public int Count => _notifications.Count;

    /// <summary>
    /// Returns whether a notification with the given ID exists (for testing).
    /// </summary>
    public bool HasNotification(string id) => _notifications.ContainsKey(id);
}
