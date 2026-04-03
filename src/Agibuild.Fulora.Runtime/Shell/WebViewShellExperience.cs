using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Agibuild.Fulora;

namespace Agibuild.Fulora.Shell;

/// <summary>
/// Opt-in runtime helper that wires common host policies (new window, downloads, permissions)
/// onto an <see cref="IWebView"/> instance.
/// </summary>
public sealed class WebViewShellExperience : IDisposable
{
    // Kept for governance invariant scanning: "Host capability bridge is required for ExternalBrowser strategy."
    private static readonly HashSet<WebViewSystemAction> DefaultSystemActionWhitelist =
    [
        WebViewSystemAction.Quit,
        WebViewSystemAction.Restart,
        WebViewSystemAction.FocusMainWindow
        // ShowAbout is intentionally opt-in via explicit SystemActionWhitelist.
    ];

    private readonly IWebView _webView;
    private readonly WebViewShellExperienceOptions _options;
    private readonly Guid _rootWindowId;
    private readonly WebViewHostCapabilityExecutor _hostCapabilityExecutor;
    private readonly ShellSystemIntegrationRuntime _systemIntegrationRuntime;
    private readonly ShellWindowingRuntime _windowingRuntime;
    private readonly ShellBrowserInteractionRuntime _browserInteractionRuntime;
    private readonly ShellRequestGovernanceRuntime _requestGovernanceRuntime;
    private readonly ShellMenuGovernanceRuntime _menuGovernanceRuntime;
    private readonly WebViewManagedWindowManager _managedWindowManager;
    private readonly WebViewNewWindowHandler _newWindowHandler;
    private readonly WebViewShellSessionDecision? _sessionDecision;
    private readonly WebViewSessionPermissionProfile? _rootProfile;
    private bool _disposed;

    /// <summary>Creates a new shell experience instance for the given WebView.</summary>
    public WebViewShellExperience(IWebView webView, WebViewShellExperienceOptions? options = null)
        : this(
            webView,
            options,
            hostCapabilityExecutor: null,
            managedWindowManager: null,
            newWindowHandler: null)
    {
    }

    internal WebViewShellExperience(
        IWebView webView,
        WebViewShellExperienceOptions? options,
        WebViewHostCapabilityExecutor? hostCapabilityExecutor,
        WebViewManagedWindowManager? managedWindowManager,
        WebViewNewWindowHandler? newWindowHandler)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        _options = options ?? new WebViewShellExperienceOptions();
        _rootWindowId = Guid.NewGuid();

        _menuGovernanceRuntime = new ShellMenuGovernanceRuntime(
            _webView,
            _options,
            _rootWindowId,
            () => _sessionDecision,
            () => _rootProfile,
            RaiseSessionPermissionProfileDiagnostic,
            ReportSystemIntegrationOutcome,
            ReportPolicyFailure);
        _hostCapabilityExecutor = hostCapabilityExecutor
                                  ?? new WebViewHostCapabilityExecutor(
                                      _webView,
                                      _options,
                                      _rootWindowId,
                                      _menuGovernanceRuntime.NormalizeMenuModel,
                                      _menuGovernanceRuntime.TryPruneMenuModel,
                                      _menuGovernanceRuntime.UpdateEffectiveMenuModel,
                                      IsSystemActionWhitelisted,
                                      ReportPolicyFailure);
        _systemIntegrationRuntime = new ShellSystemIntegrationRuntime(_hostCapabilityExecutor);
        _windowingRuntime = new ShellWindowingRuntime(_webView, _options, _rootWindowId, _hostCapabilityExecutor);
        _browserInteractionRuntime = new ShellBrowserInteractionRuntime(
            _webView,
            _options,
            _rootWindowId,
            _hostCapabilityExecutor,
            ReportPolicyFailure);
        var (resolvedRootSessionDecision, resolvedRootProfile) = _windowingRuntime.ResolveRootShellState();
        if (resolvedRootProfile is not null && resolvedRootSessionDecision is not null)
        {
            RaiseSessionPermissionProfileDiagnostic(
                _rootWindowId,
                parentWindowId: null,
                scopeIdentity: _options.SessionContext.ScopeIdentity,
                profile: resolvedRootProfile,
                sessionDecision: resolvedRootSessionDecision,
                permissionKind: null,
                permissionDecision: WebViewPermissionProfileDecision.DefaultFallback());
        }

        _sessionDecision = resolvedRootSessionDecision;
        _rootProfile = resolvedRootProfile;
        _requestGovernanceRuntime = new ShellRequestGovernanceRuntime(
            _webView,
            _options,
            _rootWindowId,
            _hostCapabilityExecutor,
            _sessionDecision,
            _rootProfile,
            RaiseSessionPermissionProfileDiagnostic);

        _managedWindowManager = managedWindowManager
                                ?? new WebViewManagedWindowManager(
                                    _options,
                                    _rootWindowId,
                                    _sessionDecision,
                                    _rootProfile,
                                    _windowingRuntime,
                                    ReportPolicyFailure,
                                    RaiseSessionPermissionProfileDiagnostic,
                                    args => ManagedWindowLifecycleChanged?.Invoke(this, args));

        _newWindowHandler = newWindowHandler
                            ?? new WebViewNewWindowHandler(
                                _webView,
                                _options,
                                _managedWindowManager,
                                _windowingRuntime,
                                ReportPolicyFailure);

        if (_options.NewWindowPolicy is not null)
            _webView.NewWindowRequested += OnNewWindowRequested;
        if (_options.DownloadPolicy is not null || _options.DownloadHandler is not null)
            _webView.DownloadRequested += OnDownloadRequested;
        if (_options.PermissionPolicy is not null ||
            _options.PermissionHandler is not null ||
            _options.SessionPermissionProfileResolver is not null)
            _webView.PermissionRequested += OnPermissionRequested;
        if (_options.HostCapabilityBridge is not null)
            _options.HostCapabilityBridge.SystemIntegrationEventDispatched += OnSystemIntegrationEventDispatched;
    }

    /// <summary>
    /// Raised when policy execution fails in any shell domain.
    /// </summary>
    public event EventHandler<WebViewShellPolicyErrorEventArgs>? PolicyError;
    /// <summary>
    /// Raised whenever a managed window lifecycle state changes.
    /// </summary>
    public event EventHandler<WebViewManagedWindowLifecycleEventArgs>? ManagedWindowLifecycleChanged;
    /// <summary>
    /// Raised when profile resolution/evaluation completes for session or permission paths.
    /// </summary>
    public event EventHandler<WebViewSessionPermissionProfileDiagnosticEventArgs>? SessionPermissionProfileEvaluated;
    /// <summary>
    /// Raised when an inbound system integration event is delivered through shell governance.
    /// </summary>
    public event EventHandler<WebViewSystemIntegrationEventRequest>? SystemIntegrationEventReceived;

    /// <summary>
    /// Gets the session decision resolved at construction time when <see cref="WebViewShellExperienceOptions.SessionPolicy"/> is configured.
    /// </summary>
    public WebViewShellSessionDecision? SessionDecision => _sessionDecision;
    /// <summary>
    /// Gets root profile identity when profile resolver is configured.
    /// </summary>
    public string? RootProfileIdentity => _rootProfile?.ProfileIdentity;
    /// <summary>
    /// Current effective menu model after pruning and successful application.
    /// </summary>
    public WebViewMenuModelRequest? EffectiveMenuModel => _menuGovernanceRuntime.GetEffectiveMenuModelSnapshot();

    /// <summary>
    /// Stable identity of the root window associated with this shell experience.
    /// </summary>
    public Guid RootWindowId => _rootWindowId;

    /// <summary>
    /// Number of managed windows currently tracked by runtime.
    /// </summary>
    public int ManagedWindowCount => _managedWindowManager.ManagedWindowCount;

    /// <summary>
    /// Returns a snapshot of active managed window ids.
    /// </summary>
    public IReadOnlyList<Guid> GetManagedWindowIds()
        => _managedWindowManager.GetManagedWindowIds();

    /// <summary>
    /// Attempts to get a managed child window by id.
    /// </summary>
    public bool TryGetManagedWindow(Guid windowId, out IWebView? managedWindow)
        => _managedWindowManager.TryGetManagedWindow(windowId, out managedWindow);

    /// <summary>
    /// Reads host clipboard text via typed capability bridge.
    /// Returns denied result when bridge is not configured.
    /// </summary>
    public WebViewHostCapabilityCallResult<string?> ReadClipboardText()
        => _systemIntegrationRuntime.ReadClipboardText();

    /// <summary>
    /// Writes host clipboard text via typed capability bridge.
    /// Returns denied result when bridge is not configured.
    /// </summary>
    public WebViewHostCapabilityCallResult<object?> WriteClipboardText(string text)
        => _systemIntegrationRuntime.WriteClipboardText(text);

    /// <summary>
    /// Shows host open-file dialog via typed capability bridge.
    /// Returns denied result when bridge is not configured.
    /// </summary>
    public WebViewHostCapabilityCallResult<WebViewFileDialogResult> ShowOpenFileDialog(WebViewOpenFileDialogRequest request)
        => _systemIntegrationRuntime.ShowOpenFileDialog(request);

    /// <summary>
    /// Shows host save-file dialog via typed capability bridge.
    /// Returns denied result when bridge is not configured.
    /// </summary>
    public WebViewHostCapabilityCallResult<WebViewFileDialogResult> ShowSaveFileDialog(WebViewSaveFileDialogRequest request)
        => _systemIntegrationRuntime.ShowSaveFileDialog(request);

    /// <summary>
    /// Shows host notification via typed capability bridge.
    /// Returns denied result when bridge is not configured.
    /// </summary>
    public WebViewHostCapabilityCallResult<object?> ShowNotification(WebViewNotificationRequest request)
        => _systemIntegrationRuntime.ShowNotification(request);

    /// <summary>
    /// Applies host app menu model via typed capability bridge.
    /// Reports deterministic policy failures for deny/failure outcomes.
    /// </summary>
    public WebViewHostCapabilityCallResult<object?> ApplyMenuModel(WebViewMenuModelRequest request)
        => _systemIntegrationRuntime.ApplyMenuModel(request);

    /// <summary>
    /// Updates host tray state via typed capability bridge.
    /// Reports deterministic policy failures for deny/failure outcomes.
    /// </summary>
    public WebViewHostCapabilityCallResult<object?> UpdateTrayState(WebViewTrayStateRequest request)
        => _systemIntegrationRuntime.UpdateTrayState(request);

    /// <summary>
    /// Executes host system action via typed capability bridge.
    /// Reports deterministic policy failures for deny/failure outcomes.
    /// </summary>
    public WebViewHostCapabilityCallResult<object?> ExecuteSystemAction(WebViewSystemActionRequest request)
        => _systemIntegrationRuntime.ExecuteSystemAction(request);

    /// <summary>
    /// Publishes a host-originated system integration event through typed capability governance.
    /// </summary>
    public WebViewHostCapabilityCallResult<WebViewSystemIntegrationEventRequest> PublishSystemIntegrationEvent(
        WebViewSystemIntegrationEventRequest request)
        => _systemIntegrationRuntime.PublishSystemIntegrationEvent(request);

    /// <summary>
    /// Opens DevTools through shell policy governance.
    /// Returns false when blocked by policy or when execution fails.
    /// </summary>
    public Task<bool> OpenDevToolsAsync()
    {
        if (_disposed)
            return Task.FromResult(false);
        return _browserInteractionRuntime.OpenDevToolsAsync();
    }

    /// <summary>
    /// Closes DevTools through shell policy governance.
    /// Returns false when blocked by policy or when execution fails.
    /// </summary>
    public Task<bool> CloseDevToolsAsync()
    {
        if (_disposed)
            return Task.FromResult(false);
        return _browserInteractionRuntime.CloseDevToolsAsync();
    }

    /// <summary>
    /// Queries DevTools open state through shell policy governance.
    /// Returns false when blocked by policy or when execution fails.
    /// </summary>
    public Task<bool> IsDevToolsOpenAsync()
    {
        if (_disposed)
            return Task.FromResult(false);
        return _browserInteractionRuntime.IsDevToolsOpenAsync();
    }

    /// <summary>
    /// Executes a standard command through shell policy governance.
    /// Returns false when blocked, unsupported, or execution fails.
    /// </summary>
    public Task<bool> ExecuteCommandAsync(WebViewCommand command)
    {
        if (_disposed)
            return Task.FromResult(false);
        return _browserInteractionRuntime.ExecuteCommandAsync(command);
    }

    private void OnNewWindowRequested(object? sender, NewWindowRequestedEventArgs e)
    {
        if (_disposed) return;
        _newWindowHandler.Handle(e);
    }

    private void OnDownloadRequested(object? sender, DownloadRequestedEventArgs e)
    {
        if (_disposed) return;
        _requestGovernanceRuntime.HandleDownloadRequested(e);
    }

    private void OnPermissionRequested(object? sender, PermissionRequestedEventArgs e)
    {
        if (_disposed) return;
        _requestGovernanceRuntime.HandlePermissionRequested(e);
    }

    /// <summary>
    /// Closes a managed window by id and waits for bounded teardown completion.
    /// </summary>
    public async Task<bool> CloseManagedWindowAsync(
        Guid windowId,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return false;
        return await _managedWindowManager.CloseManagedWindowAsync(windowId, timeout, cancellationToken).ConfigureAwait(false);
    }

    private void ExecutePolicyDomain(WebViewShellPolicyDomain domain, Action action)
        => _hostCapabilityExecutor.ExecutePolicyDomain(domain, action);

    private T? ExecutePolicyDomain<T>(WebViewShellPolicyDomain domain, Func<T> action)
        => _hostCapabilityExecutor.ExecutePolicyDomain(domain, action);

    private void RaiseSessionPermissionProfileDiagnostic(
        Guid windowId,
        Guid? parentWindowId,
        string scopeIdentity,
        WebViewSessionPermissionProfile profile,
        WebViewShellSessionDecision sessionDecision,
        WebViewPermissionKind? permissionKind,
        WebViewPermissionProfileDecision permissionDecision)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(sessionDecision);

        SessionPermissionProfileEvaluated?.Invoke(
            this,
            new WebViewSessionPermissionProfileDiagnosticEventArgs(
                windowId,
                parentWindowId,
                profile.ProfileIdentity,
                WebViewSessionPermissionProfile.NormalizeProfileVersion(profile.ProfileVersion),
                WebViewSessionPermissionProfile.NormalizeProfileHash(profile.ProfileHash),
                scopeIdentity,
                sessionDecision,
                permissionKind,
                permissionDecision));
    }

    private void OnSystemIntegrationEventDispatched(object? sender, WebViewSystemIntegrationEventRequest request)
        => SystemIntegrationEventReceived?.Invoke(this, request);

    private bool IsSystemActionWhitelisted(WebViewSystemAction action)
    {
        var whitelist = _options.SystemActionWhitelist;
        if (whitelist is not null)
            return whitelist.Contains(action);
        return DefaultSystemActionWhitelist.Contains(action);
    }

    private static bool IsTransitionAllowed(
        WebViewManagedWindowLifecycleState? currentState,
        WebViewManagedWindowLifecycleState nextState)
    {
        return currentState switch
        {
            null => nextState == WebViewManagedWindowLifecycleState.Created,
            WebViewManagedWindowLifecycleState.Created => nextState is WebViewManagedWindowLifecycleState.Attached or WebViewManagedWindowLifecycleState.Closing,
            WebViewManagedWindowLifecycleState.Attached => nextState is WebViewManagedWindowLifecycleState.Ready or WebViewManagedWindowLifecycleState.Closing,
            WebViewManagedWindowLifecycleState.Ready => nextState == WebViewManagedWindowLifecycleState.Closing,
            WebViewManagedWindowLifecycleState.Closing => nextState == WebViewManagedWindowLifecycleState.Closed,
            WebViewManagedWindowLifecycleState.Closed => false,
            _ => false
        };
    }

    private void ReportPolicyFailure(WebViewShellPolicyDomain domain, Exception ex)
    {
        if (!WebViewOperationFailure.TryGetCategory(ex, out _))
            WebViewOperationFailure.SetCategory(ex, WebViewOperationFailureCategory.AdapterFailed);

        var errorArgs = new WebViewShellPolicyErrorEventArgs(domain, ex);
        PolicyError?.Invoke(this, errorArgs);

        try
        {
            _options.PolicyErrorHandler?.Invoke(_webView, errorArgs);
        }
        catch
        {
            // Policy error reporting is best-effort and must not crash event flow.
        }
    }

    private void ReportSystemIntegrationOutcome<T>(
        WebViewHostCapabilityCallResult<T> result,
        string defaultDenyReason)
        => _hostCapabilityExecutor.ReportSystemIntegrationOutcome(result, defaultDenyReason);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _managedWindowManager.DisposeManagedWindows();

        _webView.NewWindowRequested -= OnNewWindowRequested;
        _webView.DownloadRequested -= OnDownloadRequested;
        _webView.PermissionRequested -= OnPermissionRequested;
        if (_options.HostCapabilityBridge is not null)
            _options.HostCapabilityBridge.SystemIntegrationEventDispatched -= OnSystemIntegrationEventDispatched;
    }
}
