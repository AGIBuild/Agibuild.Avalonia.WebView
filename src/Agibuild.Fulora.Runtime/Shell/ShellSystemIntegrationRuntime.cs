namespace Agibuild.Fulora.Shell;

/// <summary>
/// Thin runtime façade for shell system-integration capabilities.
/// Keeps host capability execution out of <see cref="WebViewShellExperience"/>.
/// </summary>
internal sealed class ShellSystemIntegrationRuntime
{
    private readonly WebViewHostCapabilityExecutor _executor;

    public ShellSystemIntegrationRuntime(WebViewHostCapabilityExecutor executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    public WebViewHostCapabilityCallResult<string?> ReadClipboardText()
        => _executor.ReadClipboardText();

    public WebViewHostCapabilityCallResult<object?> WriteClipboardText(string text)
        => _executor.WriteClipboardText(text);

    public WebViewHostCapabilityCallResult<WebViewFileDialogResult> ShowOpenFileDialog(WebViewOpenFileDialogRequest request)
        => _executor.ShowOpenFileDialog(request);

    public WebViewHostCapabilityCallResult<WebViewFileDialogResult> ShowSaveFileDialog(WebViewSaveFileDialogRequest request)
        => _executor.ShowSaveFileDialog(request);

    public WebViewHostCapabilityCallResult<object?> ShowNotification(WebViewNotificationRequest request)
        => _executor.ShowNotification(request);

    public WebViewHostCapabilityCallResult<object?> ApplyMenuModel(WebViewMenuModelRequest request)
        => _executor.ApplyMenuModel(request);

    public WebViewHostCapabilityCallResult<object?> UpdateTrayState(WebViewTrayStateRequest request)
        => _executor.UpdateTrayState(request);

    public WebViewHostCapabilityCallResult<object?> ExecuteSystemAction(WebViewSystemActionRequest request)
        => _executor.ExecuteSystemAction(request);

    public WebViewHostCapabilityCallResult<WebViewSystemIntegrationEventRequest> PublishSystemIntegrationEvent(
        WebViewSystemIntegrationEventRequest request)
        => _executor.PublishSystemIntegrationEvent(request);
}
