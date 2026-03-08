using System;
using Agibuild.Fulora;

namespace Agibuild.Fulora.Shell;

internal sealed class WebViewNewWindowHandler
{
    private readonly IWebView _webView;
    private readonly WebViewShellExperienceOptions _options;
    private readonly Guid _rootWindowId;
    private readonly WebViewManagedWindowManager _managedWindowManager;
    private readonly WebViewHostCapabilityExecutor _policyExecutor;
    private readonly Action<WebViewShellPolicyDomain, Exception> _reportPolicyFailure;

    public WebViewNewWindowHandler(
        IWebView webView,
        WebViewShellExperienceOptions options,
        Guid rootWindowId,
        WebViewManagedWindowManager managedWindowManager,
        WebViewHostCapabilityExecutor policyExecutor,
        Action<WebViewShellPolicyDomain, Exception> reportPolicyFailure)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _rootWindowId = rootWindowId;
        _managedWindowManager = managedWindowManager ?? throw new ArgumentNullException(nameof(managedWindowManager));
        _policyExecutor = policyExecutor ?? throw new ArgumentNullException(nameof(policyExecutor));
        _reportPolicyFailure = reportPolicyFailure ?? throw new ArgumentNullException(nameof(reportPolicyFailure));
    }

    public void Handle(NewWindowRequestedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var candidateWindowId = Guid.NewGuid();
        var decision = ResolveNewWindowStrategy(candidateWindowId, args)
                       ?? WebViewNewWindowStrategyDecision.InPlace();
        ExecuteStrategyDecision(decision, candidateWindowId, args);
    }

    private WebViewNewWindowStrategyDecision? ResolveNewWindowStrategy(Guid candidateWindowId, NewWindowRequestedEventArgs args)
    {
        var policyContext = new WebViewNewWindowPolicyContext(
            SourceWindowId: _rootWindowId,
            CandidateWindowId: candidateWindowId,
            TargetUri: args.Uri,
            ScopeIdentity: _options.SessionContext.ScopeIdentity);

        return _policyExecutor.ExecutePolicyDomain(
            WebViewShellPolicyDomain.NewWindow,
            () => _options.NewWindowPolicy?.Decide(_webView, args, policyContext)
                  ?? WebViewNewWindowStrategyDecision.InPlace());
    }

    private void ExecuteStrategyDecision(WebViewNewWindowStrategyDecision decision, Guid candidateWindowId, NewWindowRequestedEventArgs args)
    {
        switch (decision.Strategy)
        {
            case WebViewNewWindowStrategy.InPlace:
                args.Handled = false;
                return;
            case WebViewNewWindowStrategy.ManagedWindow:
                HandleManagedWindowStrategy(decision, candidateWindowId, args);
                return;
            case WebViewNewWindowStrategy.ExternalBrowser:
                HandleExternalBrowserStrategy(args);
                return;
            case WebViewNewWindowStrategy.Delegate:
                args.Handled = decision.Handled;
                return;
            default:
                args.Handled = false;
                return;
        }
    }

    private void HandleManagedWindowStrategy(
        WebViewNewWindowStrategyDecision decision,
        Guid candidateWindowId,
        NewWindowRequestedEventArgs args)
    {
        var created = _managedWindowManager.TryCreateManagedWindow(candidateWindowId, args.Uri, decision.ScopeIdentityOverride);
        args.Handled = created;
        if (!created)
            args.Handled = false;
    }

    private void HandleExternalBrowserStrategy(NewWindowRequestedEventArgs args)
    {
        if (args.Uri is null)
        {
            args.Handled = true;
            _reportPolicyFailure(
                WebViewShellPolicyDomain.ExternalOpen,
                new InvalidOperationException("External open strategy requires a non-null target URI."));
            return;
        }

        if (_options.HostCapabilityBridge is null)
        {
            args.Handled = true;
            _reportPolicyFailure(
                WebViewShellPolicyDomain.ExternalOpen,
                new InvalidOperationException("Host capability bridge is required for ExternalBrowser strategy."));
            return;
        }

        var openResult = _options.HostCapabilityBridge.OpenExternal(
            args.Uri,
            _rootWindowId,
            parentWindowId: _rootWindowId,
            targetWindowId: null);

        args.Handled = true;
        if (openResult.Outcome == WebViewHostCapabilityCallOutcome.Deny)
        {
            _reportPolicyFailure(
                WebViewShellPolicyDomain.ExternalOpen,
                new UnauthorizedAccessException(openResult.DenyReason ?? "External open was denied by host capability policy."));
            return;
        }

        if (openResult.Outcome == WebViewHostCapabilityCallOutcome.Failure)
        {
            _reportPolicyFailure(
                WebViewShellPolicyDomain.ExternalOpen,
                openResult.Error ?? new InvalidOperationException("External open failed without an exception payload."));
        }
    }
}
