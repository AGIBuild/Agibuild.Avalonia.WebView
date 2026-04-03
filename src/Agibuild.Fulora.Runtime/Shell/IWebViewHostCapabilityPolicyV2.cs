using System;
using System.Collections.Generic;

namespace Agibuild.Fulora.Shell;

/// <summary>
/// Rich capability authorization context used by policy v2.
/// </summary>
public readonly record struct WebViewCapabilityAuthorizationContext(
    Guid RootWindowId,
    Guid? ParentWindowId,
    Guid? TargetWindowId,
    WebViewHostCapabilityOperation Operation,
    string CapabilityId,
    string SourceComponent,
    Uri? RequestUri = null,
    string? RequestedAction = null,
    IReadOnlyDictionary<string, string>? Attributes = null);

/// <summary>
/// Capability authorization policy with support for constrained allow decisions.
/// </summary>
public interface IWebViewHostCapabilityPolicyV2
{
    /// <summary>Evaluates a capability request context.</summary>
    WebViewCapabilityPolicyDecision Evaluate(in WebViewCapabilityAuthorizationContext context);
}
