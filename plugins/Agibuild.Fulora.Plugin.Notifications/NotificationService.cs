namespace Agibuild.Fulora.Plugin.Notifications;

/// <summary>
/// Implementation of <see cref="INotificationService"/> that delegates to an <see cref="INativeNotificationProvider"/>.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly INativeNotificationProvider _provider;

    /// <summary>Initializes a new instance with the specified provider.</summary>
    /// <param name="provider">The native notification provider to delegate to.</param>
    public NotificationService(INativeNotificationProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>Shows a notification and returns its assigned ID.</summary>
    public Task<string> Show(string title, string body, NotificationOptions? options = null) =>
        _provider.ShowNotification(title, body, options);

    /// <summary>Requests permission to show notifications.</summary>
    public Task<bool> RequestPermission() =>
        _provider.RequestPermission();

    /// <summary>Clears all notifications.</summary>
    public Task ClearAll() =>
        _provider.ClearAll();

    /// <summary>Clears the notification with the specified ID.</summary>
    public Task Clear(string notificationId) =>
        _provider.Clear(notificationId);
}
