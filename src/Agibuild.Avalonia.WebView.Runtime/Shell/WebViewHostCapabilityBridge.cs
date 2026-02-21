using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Agibuild.Avalonia.WebView.Shell;

/// <summary>
/// Typed host capability operations available through the shell bridge.
/// </summary>
public enum WebViewHostCapabilityOperation
{
    /// <summary>Read text from host clipboard.</summary>
    ClipboardReadText = 0,
    /// <summary>Write text to host clipboard.</summary>
    ClipboardWriteText = 1,
    /// <summary>Open-file picker operation.</summary>
    FileDialogOpen = 2,
    /// <summary>Save-file picker operation.</summary>
    FileDialogSave = 3,
    /// <summary>Open URI in external application/browser.</summary>
    ExternalOpen = 4,
    /// <summary>Show a host notification.</summary>
    NotificationShow = 5,
    /// <summary>Apply host app menu model.</summary>
    MenuApplyModel = 6,
    /// <summary>Update host tray state.</summary>
    TrayUpdateState = 7,
    /// <summary>Execute host system action.</summary>
    SystemActionExecute = 8,
    /// <summary>Dispatch tray interaction event to web pipeline.</summary>
    TrayInteractionEventDispatch = 9,
    /// <summary>Dispatch menu interaction event to web pipeline.</summary>
    MenuInteractionEventDispatch = 10
}

/// <summary>
/// Request context for host capability authorization.
/// </summary>
public readonly record struct WebViewHostCapabilityRequestContext(
    Guid RootWindowId,
    Guid? ParentWindowId,
    Guid? TargetWindowId,
    WebViewHostCapabilityOperation Operation,
    Uri? RequestUri = null);

/// <summary>
/// Authorization decision kind for a capability request.
/// </summary>
public enum WebViewHostCapabilityDecisionKind
{
    /// <summary>Request is allowed.</summary>
    Allow = 0,
    /// <summary>Request is denied.</summary>
    Deny = 1
}

/// <summary>
/// Authorization decision for a capability request.
/// </summary>
public readonly record struct WebViewHostCapabilityDecision(
    WebViewHostCapabilityDecisionKind Kind,
    string? Reason = null)
{
    /// <summary>Create an allow decision.</summary>
    public static WebViewHostCapabilityDecision Allow()
        => new(WebViewHostCapabilityDecisionKind.Allow);

    /// <summary>Create a deny decision.</summary>
    public static WebViewHostCapabilityDecision Deny(string? reason = null)
        => new(WebViewHostCapabilityDecisionKind.Deny, reason);

    /// <summary>
    /// True when this request is allowed.
    /// </summary>
    public bool IsAllowed => Kind == WebViewHostCapabilityDecisionKind.Allow;
}

/// <summary>
/// Deterministic outcome model for host capability calls.
/// </summary>
public enum WebViewHostCapabilityCallOutcome
{
    /// <summary>Capability is authorized and executed successfully.</summary>
    Allow = 0,
    /// <summary>Capability is denied by policy before provider execution.</summary>
    Deny = 1,
    /// <summary>Capability flow failed deterministically (policy/provider error).</summary>
    Failure = 2
}

/// <summary>
/// Host capability authorization policy.
/// </summary>
public interface IWebViewHostCapabilityPolicy
{
    /// <summary>Evaluates a capability request context.</summary>
    WebViewHostCapabilityDecision Evaluate(in WebViewHostCapabilityRequestContext context);
}

/// <summary>
/// Request payload for open-file dialog.
/// </summary>
public sealed class WebViewOpenFileDialogRequest
{
    /// <summary>Dialog title.</summary>
    public string? Title { get; init; }
    /// <summary>Whether multiple selection is allowed.</summary>
    public bool AllowMultiple { get; init; }
}

/// <summary>
/// Request payload for save-file dialog.
/// </summary>
public sealed class WebViewSaveFileDialogRequest
{
    /// <summary>Dialog title.</summary>
    public string? Title { get; init; }
    /// <summary>Suggested initial file name.</summary>
    public string? SuggestedFileName { get; init; }
}

/// <summary>
/// Request payload for host notification.
/// </summary>
public sealed class WebViewNotificationRequest
{
    /// <summary>Notification title.</summary>
    public required string Title { get; init; }
    /// <summary>Notification body.</summary>
    public required string Message { get; init; }
}

/// <summary>
/// Typed menu model payload for host app-shell integration.
/// </summary>
public sealed class WebViewMenuModelRequest
{
    /// <summary>Top-level menu items.</summary>
    public IReadOnlyList<WebViewMenuItemModel> Items { get; init; } = [];
}

/// <summary>
/// Typed menu item model.
/// </summary>
public sealed class WebViewMenuItemModel
{
    /// <summary>Stable item id.</summary>
    public required string Id { get; init; }
    /// <summary>User-visible label.</summary>
    public required string Label { get; init; }
    /// <summary>Whether this item is enabled.</summary>
    public bool IsEnabled { get; init; } = true;
    /// <summary>Nested child menu items.</summary>
    public IReadOnlyList<WebViewMenuItemModel> Children { get; init; } = [];
}

/// <summary>
/// Typed tray state payload for host app-shell integration.
/// </summary>
public sealed class WebViewTrayStateRequest
{
    /// <summary>Whether tray entry should be visible.</summary>
    public bool IsVisible { get; init; }
    /// <summary>Optional tray tooltip text.</summary>
    public string? Tooltip { get; init; }
    /// <summary>Optional tray icon path.</summary>
    public string? IconPath { get; init; }
}

/// <summary>
/// Supported typed system actions.
/// </summary>
public enum WebViewSystemAction
{
    /// <summary>Quit application.</summary>
    Quit = 0,
    /// <summary>Restart application.</summary>
    Restart = 1,
    /// <summary>Focus main window.</summary>
    FocusMainWindow = 2
}

/// <summary>
/// Typed system integration event kinds emitted from host to web pipeline.
/// </summary>
public enum WebViewSystemIntegrationEventKind
{
    /// <summary>Tray icon was interacted with.</summary>
    TrayInteracted = 0,
    /// <summary>Menu item was invoked.</summary>
    MenuItemInvoked = 1
}

/// <summary>
/// Typed inbound system integration event payload.
/// </summary>
public sealed class WebViewSystemIntegrationEventRequest
{
    /// <summary>Event kind.</summary>
    public WebViewSystemIntegrationEventKind Kind { get; init; }
    /// <summary>Optional item id for menu/tray events.</summary>
    public string? ItemId { get; init; }
    /// <summary>Optional context payload for diagnostics and UI reaction.</summary>
    public string? Context { get; init; }
}

/// <summary>
/// Typed request payload for host system action execution.
/// </summary>
public sealed class WebViewSystemActionRequest
{
    /// <summary>Action to execute.</summary>
    public WebViewSystemAction Action { get; init; }
}

/// <summary>
/// Result of file dialog operations.
/// </summary>
public sealed class WebViewFileDialogResult
{
    /// <summary>Whether user canceled the dialog.</summary>
    public bool IsCanceled { get; init; }
    /// <summary>Selected file paths.</summary>
    public IReadOnlyList<string> Paths { get; init; } = [];
}

/// <summary>
/// Host capability provider implementation.
/// </summary>
public interface IWebViewHostCapabilityProvider
{
    /// <summary>Reads text from host clipboard.</summary>
    string? ReadClipboardText();
    /// <summary>Writes text to host clipboard.</summary>
    void WriteClipboardText(string text);
    /// <summary>Shows open-file dialog.</summary>
    WebViewFileDialogResult ShowOpenFileDialog(WebViewOpenFileDialogRequest request);
    /// <summary>Shows save-file dialog.</summary>
    WebViewFileDialogResult ShowSaveFileDialog(WebViewSaveFileDialogRequest request);
    /// <summary>Opens URI using external host app/browser.</summary>
    void OpenExternal(Uri uri);
    /// <summary>Shows host notification.</summary>
    void ShowNotification(WebViewNotificationRequest request);
    /// <summary>Applies host app menu model.</summary>
    void ApplyMenuModel(WebViewMenuModelRequest request);
    /// <summary>Updates host tray state.</summary>
    void UpdateTrayState(WebViewTrayStateRequest request);
    /// <summary>Executes host system action.</summary>
    void ExecuteSystemAction(WebViewSystemActionRequest request);
}

/// <summary>
/// Typed result envelope for host capability calls.
/// </summary>
public sealed class WebViewHostCapabilityCallResult<T>
{
    private WebViewHostCapabilityCallResult(
        WebViewHostCapabilityCallOutcome outcome,
        bool wasAuthorized,
        T? value,
        string? denyReason,
        Exception? error)
    {
        Outcome = outcome;
        WasAuthorized = wasAuthorized;
        Value = value;
        DenyReason = denyReason;
        Error = error;
    }

    /// <summary>Deterministic capability outcome.</summary>
    public WebViewHostCapabilityCallOutcome Outcome { get; }
    /// <summary>Whether policy authorization succeeded before execution.</summary>
    public bool WasAuthorized { get; }
    /// <summary>Whether the capability request was authorized.</summary>
    public bool IsAllowed => WasAuthorized;
    /// <summary>Whether the authorized operation completed successfully.</summary>
    public bool IsSuccess => Outcome == WebViewHostCapabilityCallOutcome.Allow;
    /// <summary>Typed return value.</summary>
    public T? Value { get; }
    /// <summary>Deny reason when request is denied.</summary>
    public string? DenyReason { get; }
    /// <summary>Operation error when execution fails.</summary>
    public Exception? Error { get; }

    internal static WebViewHostCapabilityCallResult<T> Success(T? value)
        => new(WebViewHostCapabilityCallOutcome.Allow, wasAuthorized: true, value, denyReason: null, error: null);

    internal static WebViewHostCapabilityCallResult<T> Denied(string? reason)
        => new(WebViewHostCapabilityCallOutcome.Deny, wasAuthorized: false, value: default, denyReason: reason, error: null);

    internal static WebViewHostCapabilityCallResult<T> Failure(Exception error, bool wasAuthorized)
        => new(WebViewHostCapabilityCallOutcome.Failure, wasAuthorized, value: default, denyReason: null, error);
}

/// <summary>
/// Structured diagnostic payload for a completed host capability call.
/// </summary>
public sealed class WebViewHostCapabilityDiagnosticEventArgs : EventArgs
{
    /// <summary>Create diagnostic payload.</summary>
    public WebViewHostCapabilityDiagnosticEventArgs(
        Guid correlationId,
        Guid rootWindowId,
        Guid? parentWindowId,
        Guid? targetWindowId,
        WebViewHostCapabilityOperation operation,
        Uri? requestUri,
        WebViewHostCapabilityCallOutcome outcome,
        bool wasAuthorized,
        string? denyReason,
        WebViewOperationFailureCategory? failureCategory,
        long durationMilliseconds)
    {
        CorrelationId = correlationId;
        RootWindowId = rootWindowId;
        ParentWindowId = parentWindowId;
        TargetWindowId = targetWindowId;
        Operation = operation;
        RequestUri = requestUri;
        Outcome = outcome;
        WasAuthorized = wasAuthorized;
        DenyReason = denyReason;
        FailureCategory = failureCategory;
        DurationMilliseconds = durationMilliseconds;
    }

    /// <summary>Stable call correlation id.</summary>
    public Guid CorrelationId { get; }
    /// <summary>Root shell window id.</summary>
    public Guid RootWindowId { get; }
    /// <summary>Optional parent window id.</summary>
    public Guid? ParentWindowId { get; }
    /// <summary>Optional target window id.</summary>
    public Guid? TargetWindowId { get; }
    /// <summary>Capability operation.</summary>
    public WebViewHostCapabilityOperation Operation { get; }
    /// <summary>Optional request URI for URI-bound operations.</summary>
    public Uri? RequestUri { get; }
    /// <summary>Deterministic capability outcome.</summary>
    public WebViewHostCapabilityCallOutcome Outcome { get; }
    /// <summary>Whether policy authorization succeeded before execution.</summary>
    public bool WasAuthorized { get; }
    /// <summary>Deny reason when outcome is deny.</summary>
    public string? DenyReason { get; }
    /// <summary>Failure category when outcome is failure.</summary>
    public WebViewOperationFailureCategory? FailureCategory { get; }
    /// <summary>Elapsed duration in milliseconds.</summary>
    public long DurationMilliseconds { get; }
}

/// <summary>
/// Runtime host capability bridge with policy-first deterministic execution semantics.
/// </summary>
public sealed class WebViewHostCapabilityBridge
{
    private readonly IWebViewHostCapabilityProvider _provider;
    private readonly IWebViewHostCapabilityPolicy? _policy;

    /// <summary>
    /// Raised when a typed capability call is completed with deterministic outcome metadata.
    /// </summary>
    public event EventHandler<WebViewHostCapabilityDiagnosticEventArgs>? CapabilityCallCompleted;
    /// <summary>
    /// Raised when a typed inbound system integration event is allowed and dispatched.
    /// </summary>
    public event EventHandler<WebViewSystemIntegrationEventRequest>? SystemIntegrationEventDispatched;

    /// <summary>Create bridge with provider and optional authorization policy.</summary>
    public WebViewHostCapabilityBridge(IWebViewHostCapabilityProvider provider, IWebViewHostCapabilityPolicy? policy = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _policy = policy;
    }

    /// <summary>Reads text from clipboard.</summary>
    public WebViewHostCapabilityCallResult<string?> ReadClipboardText(
        Guid rootWindowId,
        Guid? parentWindowId = null,
        Guid? targetWindowId = null)
        => Execute(
            new WebViewHostCapabilityRequestContext(
                rootWindowId,
                parentWindowId,
                targetWindowId,
                WebViewHostCapabilityOperation.ClipboardReadText),
            () => _provider.ReadClipboardText());

    /// <summary>Writes text to clipboard.</summary>
    public WebViewHostCapabilityCallResult<object?> WriteClipboardText(
        string text,
        Guid rootWindowId,
        Guid? parentWindowId = null,
        Guid? targetWindowId = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Execute<object?>(
            new WebViewHostCapabilityRequestContext(
                rootWindowId,
                parentWindowId,
                targetWindowId,
                WebViewHostCapabilityOperation.ClipboardWriteText),
            () =>
            {
                _provider.WriteClipboardText(text);
                return null;
            });
    }

    /// <summary>Shows open-file dialog.</summary>
    public WebViewHostCapabilityCallResult<WebViewFileDialogResult> ShowOpenFileDialog(
        WebViewOpenFileDialogRequest request,
        Guid rootWindowId,
        Guid? parentWindowId = null,
        Guid? targetWindowId = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Execute(
            new WebViewHostCapabilityRequestContext(
                rootWindowId,
                parentWindowId,
                targetWindowId,
                WebViewHostCapabilityOperation.FileDialogOpen),
            () => _provider.ShowOpenFileDialog(request));
    }

    /// <summary>Shows save-file dialog.</summary>
    public WebViewHostCapabilityCallResult<WebViewFileDialogResult> ShowSaveFileDialog(
        WebViewSaveFileDialogRequest request,
        Guid rootWindowId,
        Guid? parentWindowId = null,
        Guid? targetWindowId = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Execute(
            new WebViewHostCapabilityRequestContext(
                rootWindowId,
                parentWindowId,
                targetWindowId,
                WebViewHostCapabilityOperation.FileDialogSave),
            () => _provider.ShowSaveFileDialog(request));
    }

    /// <summary>Opens URI using external host app/browser.</summary>
    public WebViewHostCapabilityCallResult<object?> OpenExternal(
        Uri uri,
        Guid rootWindowId,
        Guid? parentWindowId = null,
        Guid? targetWindowId = null)
    {
        ArgumentNullException.ThrowIfNull(uri);
        return Execute<object?>(
            new WebViewHostCapabilityRequestContext(
                rootWindowId,
                parentWindowId,
                targetWindowId,
                WebViewHostCapabilityOperation.ExternalOpen,
                RequestUri: uri),
            () =>
            {
                _provider.OpenExternal(uri);
                return null;
            });
    }

    /// <summary>Shows host notification.</summary>
    public WebViewHostCapabilityCallResult<object?> ShowNotification(
        WebViewNotificationRequest request,
        Guid rootWindowId,
        Guid? parentWindowId = null,
        Guid? targetWindowId = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Execute<object?>(
            new WebViewHostCapabilityRequestContext(
                rootWindowId,
                parentWindowId,
                targetWindowId,
                WebViewHostCapabilityOperation.NotificationShow),
            () =>
            {
                _provider.ShowNotification(request);
                return null;
            });
    }

    /// <summary>Applies host app menu model.</summary>
    public WebViewHostCapabilityCallResult<object?> ApplyMenuModel(
        WebViewMenuModelRequest request,
        Guid rootWindowId,
        Guid? parentWindowId = null,
        Guid? targetWindowId = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Execute<object?>(
            new WebViewHostCapabilityRequestContext(
                rootWindowId,
                parentWindowId,
                targetWindowId,
                WebViewHostCapabilityOperation.MenuApplyModel),
            () =>
            {
                _provider.ApplyMenuModel(request);
                return null;
            });
    }

    /// <summary>Updates host tray state.</summary>
    public WebViewHostCapabilityCallResult<object?> UpdateTrayState(
        WebViewTrayStateRequest request,
        Guid rootWindowId,
        Guid? parentWindowId = null,
        Guid? targetWindowId = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Execute<object?>(
            new WebViewHostCapabilityRequestContext(
                rootWindowId,
                parentWindowId,
                targetWindowId,
                WebViewHostCapabilityOperation.TrayUpdateState),
            () =>
            {
                _provider.UpdateTrayState(request);
                return null;
            });
    }

    /// <summary>Executes host system action.</summary>
    public WebViewHostCapabilityCallResult<object?> ExecuteSystemAction(
        WebViewSystemActionRequest request,
        Guid rootWindowId,
        Guid? parentWindowId = null,
        Guid? targetWindowId = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Execute<object?>(
            new WebViewHostCapabilityRequestContext(
                rootWindowId,
                parentWindowId,
                targetWindowId,
                WebViewHostCapabilityOperation.SystemActionExecute),
            () =>
            {
                _provider.ExecuteSystemAction(request);
                return null;
            });
    }

    /// <summary>
    /// Dispatches a typed inbound system integration event through policy-first capability flow.
    /// </summary>
    public WebViewHostCapabilityCallResult<WebViewSystemIntegrationEventRequest> DispatchSystemIntegrationEvent(
        WebViewSystemIntegrationEventRequest request,
        Guid rootWindowId,
        Guid? parentWindowId = null,
        Guid? targetWindowId = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        var operation = request.Kind switch
        {
            WebViewSystemIntegrationEventKind.TrayInteracted => WebViewHostCapabilityOperation.TrayInteractionEventDispatch,
            WebViewSystemIntegrationEventKind.MenuItemInvoked => WebViewHostCapabilityOperation.MenuInteractionEventDispatch,
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Kind, "Unsupported system integration event kind.")
        };

        return Execute(
            new WebViewHostCapabilityRequestContext(
                rootWindowId,
                parentWindowId,
                targetWindowId,
                operation),
            () =>
            {
                SystemIntegrationEventDispatched?.Invoke(this, request);
                return request;
            });
    }

    private WebViewHostCapabilityCallResult<T> Execute<T>(
        in WebViewHostCapabilityRequestContext context,
        Func<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var correlationId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();
        WebViewHostCapabilityCallResult<T> result;

        if (!TryEvaluatePolicy(context, out var decision, out var policyFailure))
        {
            result = WebViewHostCapabilityCallResult<T>.Failure(policyFailure!, wasAuthorized: false);
        }
        else if (!decision.IsAllowed)
        {
            result = WebViewHostCapabilityCallResult<T>.Denied(decision.Reason);
        }
        else
        {
            try
            {
                var value = action();
                result = WebViewHostCapabilityCallResult<T>.Success(value);
            }
            catch (Exception ex)
            {
                if (!WebViewOperationFailure.TryGetCategory(ex, out _))
                    WebViewOperationFailure.SetCategory(ex, WebViewOperationFailureCategory.AdapterFailed);
                result = WebViewHostCapabilityCallResult<T>.Failure(ex, wasAuthorized: true);
            }
        }

        stopwatch.Stop();
        EmitCapabilityDiagnostic(correlationId, context, result, stopwatch.ElapsedMilliseconds);
        return result;
    }

    private bool TryEvaluatePolicy(
        in WebViewHostCapabilityRequestContext context,
        out WebViewHostCapabilityDecision decision,
        out Exception? failure)
    {
        if (_policy is null)
        {
            decision = WebViewHostCapabilityDecision.Allow();
            failure = null;
            return true;
        }

        try
        {
            decision = _policy.Evaluate(context);
            failure = null;
            return true;
        }
        catch (Exception ex)
        {
            if (!WebViewOperationFailure.TryGetCategory(ex, out _))
                WebViewOperationFailure.SetCategory(ex, WebViewOperationFailureCategory.AdapterFailed);
            decision = default;
            failure = ex;
            return false;
        }
    }

    private void EmitCapabilityDiagnostic<T>(
        Guid correlationId,
        in WebViewHostCapabilityRequestContext context,
        WebViewHostCapabilityCallResult<T> result,
        long durationMilliseconds)
    {
        WebViewOperationFailureCategory? failureCategory = null;
        if (result.Error is not null && WebViewOperationFailure.TryGetCategory(result.Error, out var category))
            failureCategory = category;

        CapabilityCallCompleted?.Invoke(
            this,
            new WebViewHostCapabilityDiagnosticEventArgs(
                correlationId,
                context.RootWindowId,
                context.ParentWindowId,
                context.TargetWindowId,
                context.Operation,
                context.RequestUri,
                result.Outcome,
                result.WasAuthorized,
                result.DenyReason,
                failureCategory,
                durationMilliseconds));
    }
}
