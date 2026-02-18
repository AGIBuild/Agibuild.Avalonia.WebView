using System;
using System.Collections.Generic;

namespace Agibuild.Avalonia.WebView.Shell;

/// <summary>
/// Typed host capability operations available through the shell bridge.
/// </summary>
public enum WebViewHostCapabilityOperation
{
    ClipboardReadText = 0,
    ClipboardWriteText = 1,
    FileDialogOpen = 2,
    FileDialogSave = 3,
    ExternalOpen = 4,
    NotificationShow = 5
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
    Allow = 0,
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
}

/// <summary>
/// Typed result envelope for host capability calls.
/// </summary>
public sealed class WebViewHostCapabilityCallResult<T>
{
    private WebViewHostCapabilityCallResult(
        bool isAllowed,
        bool isSuccess,
        T? value,
        string? denyReason,
        Exception? error)
    {
        IsAllowed = isAllowed;
        IsSuccess = isSuccess;
        Value = value;
        DenyReason = denyReason;
        Error = error;
    }

    /// <summary>Whether the capability request was authorized.</summary>
    public bool IsAllowed { get; }
    /// <summary>Whether the authorized operation completed successfully.</summary>
    public bool IsSuccess { get; }
    /// <summary>Typed return value.</summary>
    public T? Value { get; }
    /// <summary>Deny reason when request is denied.</summary>
    public string? DenyReason { get; }
    /// <summary>Operation error when execution fails.</summary>
    public Exception? Error { get; }

    internal static WebViewHostCapabilityCallResult<T> Success(T? value)
        => new(isAllowed: true, isSuccess: true, value, denyReason: null, error: null);

    internal static WebViewHostCapabilityCallResult<T> Denied(string? reason)
        => new(isAllowed: false, isSuccess: false, value: default, denyReason: reason, error: null);

    internal static WebViewHostCapabilityCallResult<T> Failure(Exception error)
        => new(isAllowed: true, isSuccess: false, value: default, denyReason: null, error);
}

/// <summary>
/// Runtime host capability bridge with policy-first deterministic execution semantics.
/// </summary>
public sealed class WebViewHostCapabilityBridge
{
    private readonly IWebViewHostCapabilityProvider _provider;
    private readonly IWebViewHostCapabilityPolicy? _policy;

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

    private WebViewHostCapabilityCallResult<T> Execute<T>(
        in WebViewHostCapabilityRequestContext context,
        Func<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var decision = EvaluatePolicy(context);
        if (!decision.IsAllowed)
            return WebViewHostCapabilityCallResult<T>.Denied(decision.Reason);

        try
        {
            var result = action();
            return WebViewHostCapabilityCallResult<T>.Success(result);
        }
        catch (Exception ex)
        {
            if (!WebViewOperationFailure.TryGetCategory(ex, out _))
                WebViewOperationFailure.SetCategory(ex, WebViewOperationFailureCategory.AdapterFailed);
            return WebViewHostCapabilityCallResult<T>.Failure(ex);
        }
    }

    private WebViewHostCapabilityDecision EvaluatePolicy(in WebViewHostCapabilityRequestContext context)
    {
        if (_policy is null)
            return WebViewHostCapabilityDecision.Allow();

        try
        {
            return _policy.Evaluate(context);
        }
        catch (Exception ex)
        {
            if (!WebViewOperationFailure.TryGetCategory(ex, out _))
                WebViewOperationFailure.SetCategory(ex, WebViewOperationFailureCategory.AdapterFailed);
            return WebViewHostCapabilityDecision.Deny(ex.Message);
        }
    }
}
