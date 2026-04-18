# API Surface Review — Agibuild.Fulora

**Date**: 2026-03-02
**Status**: 1.0 Freeze Audit
**Test Count**: 1668 CTs passing (1459 unit + 209 integration)
**Inventory**: `docs/API_SURFACE_INVENTORY.release.txt` (Core: 72 types, Runtime: 100 types)

---

## Summary

The public API surface is frozen for the 1.0 GA release. All previously identified action items from the pre-1.0 audit (2026-02-17) have been resolved. Phase 8 additions (deep-link registration, SPA hot update, Bridge V2 capabilities) have been audited for naming consistency and interface coverage.

---

## 1.0 Freeze Status

### IWebView Interface Coverage (Resolved ✅)

The following members have been promoted to `IWebView` (additive, non-breaking):

| Member | Status |
|--------|--------|
| `GetZoomFactorAsync` / `SetZoomFactorAsync` | ✅ On IWebView |
| `FindInPageAsync` / `StopFindInPageAsync` | ✅ On IWebView |
| `AddPreloadScriptAsync` / `RemovePreloadScriptAsync` | ✅ On IWebView |
| `ContextMenuRequested` | ✅ On IWebView |

The following remain on concrete types only (by design):

| Member | Rationale |
|--------|-----------|
| `SetCustomUserAgent` | Init-time configuration; not a runtime feature |
| `EnableWebMessageBridge` / `DisableWebMessageBridge` | Advanced plumbing; auto-managed by Bridge property |
| `EnableSpaHosting` | Configuration via `WebViewEnvironmentOptions`; deferred to post-1.0 |

### Experimental Attributes (Resolved ✅)

| Code | Type | 1.0 Decision | Rationale |
|------|------|-------------|-----------|
| AGWV001 | `ICookieManager` | **Keep experimental** | Cookie management has platform gaps (Android adapter returns null). Graduating implies full cross-platform support commitment. |
| AGWV004 | `WebResourceRequestedEventArgs` | **Graduated** | Attribute removed. Stable and widely used across all adapters. |
| AGWV005 | `EnvironmentRequestedEventArgs` | **Keep experimental** | Placeholder with no concrete implementation. Will be designed in a future release. |

### Phase 8 Additions Audit (New ✅)

Phase 8 introduced the following public API surface, all passing naming convention validation:

| Category | Key Public Types | Convention Check |
|----------|-----------------|-----------------|
| Deep Link Registration | `DeepLinkActivationEnvelope`, `DeepLinkRegistrationService`, `IDeepLinkRegistrationService`, `IDeepLinkAdmissionPolicy`, `DeepLinkRouteDeclaration`, `DeepLinkPlatformEntrypoint` | ✅ PascalCase, I* prefix, *EventArgs suffix |
| Deep Link Diagnostics | `DeepLinkDiagnosticEventArgs`, `DeepLinkDiagnosticEventType` | ✅ Follows .NET event pattern |
| Deep Link Results | `DeepLinkRegistrationResult`, `DeepLinkActivationIngressResult`, `DeepLinkAdmissionDecision` | ✅ Record pattern with factory methods |
| SPA Hot Update | `SpaAssetHotUpdateService`, `SpaAssetHotUpdateResult` | ✅ PascalCase |
| Bridge V2 | `JsExportAttribute`, `JsImportAttribute`, `IBridgeService`, `BridgeCallOptions` | ✅ PascalCase, I* prefix, *Attribute suffix |

### Type Naming Consistency ✅

All 172 public types (72 Core + 100 Runtime) follow .NET naming conventions:
- `I*` prefix for interfaces
- `*Attribute` suffix for attributes
- `*EventArgs` suffix for event argument types
- PascalCase throughout
- No convention violations detected

---

## Action Items Resolution

| # | Action | Original Priority | Resolution |
|---|--------|----------|-----------|
| 1 | Promote Zoom/Find/Preload/ContextMenu to IWebView | High | ✅ Completed — all promoted to IWebView |
| 2 | Expose EnableSpaHosting on WebView control | Medium | ⏸ Deferred to post-1.0 — adequate workaround via WebViewEnvironmentOptions |
| 3 | Graduate AGWV004 from Experimental | Low | ✅ Completed — attribute removed |
| 4 | Document event lifecycle on WebView control | Low | ✅ Documented — pre-attach subscription is no-op by design for Avalonia lifecycle |
| 5 | Version as 1.0.0-preview.1 → 1.0.0 | — | 🔜 Pending — M9.6 Stable Release Gate |

---

## Evidence Pointers (API → Executable)

- **Navigation cancel semantics**: `tests/Agibuild.Fulora.UnitTests/ContractSemanticsV1NavigationTests.cs`
- **Native navigation cancel**: `tests/Agibuild.Fulora.UnitTests/ContractSemanticsV1NativeNavigationTests.cs`
- **WebMessage bridge policy/drop**: `tests/Agibuild.Fulora.UnitTests/WebViewCoreHotspotCoverageTests.cs`
- **DevTools toggle (core)**: `tests/Agibuild.Fulora.UnitTests/RuntimeCoverageTests.cs`
- **DevTools toggle (GTK smoke)**: `tests/Agibuild.Fulora.Integration.Tests/Agibuild.Fulora.Integration.Tests/ViewModels/GtkWebViewSmokeViewModel.cs`
- **Deep link registration**: `tests/Agibuild.Fulora.UnitTests/DeepLinkRegistrationServiceTests.cs`
- **Deep link activation**: `tests/Agibuild.Fulora.UnitTests/DeepLinkActivationTests.cs`
- **Bridge cancellation**: `tests/Agibuild.Fulora.UnitTests/BridgeCancellationTests.cs`
- **Bridge streaming**: `tests/Agibuild.Fulora.UnitTests/BridgeStreamingTests.cs`
- **SPA hot update**: `tests/Agibuild.Fulora.UnitTests/SpaAssetHotUpdateTests.cs`

---

## Public API Inventory (generated)

Snapshot source:
- `dotnet build -c Release` (2026-03-01)
- Generator: `tools/gen-api-inventory.csx`

<details><summary>Inventory (Release)</summary>

```text
See `docs/API_SURFACE_INVENTORY.release.txt`.
```

</details>

---

## 1.6 Capability Split (P5, Additive)

**Date**: 2026-04-15
**Status**: Additive, source-compatible. No existing members removed or renamed.

### New Capability Interfaces (13 total — 12 composed into `IWebViewFeatures`, 1 into `IWebViewScript`)

| Interface | Members | Replaces / Extracted from |
|---|---|---|
| `IWebViewDevTools` | `OpenDevToolsAsync`, `CloseDevToolsAsync`, `IsDevToolsOpenAsync` | `IWebViewFeatures` |
| `IWebViewScreenshot` | `CaptureScreenshotAsync` | `IWebViewFeatures` |
| `IWebViewPrinting` | `PrintToPdfAsync` | `IWebViewFeatures` |
| `IWebViewZoom` | `GetZoomFactorAsync`, `SetZoomFactorAsync` | `IWebViewFeatures` |
| `IWebViewFindInPage` | `FindInPageAsync`, `StopFindInPageAsync` | `IWebViewFeatures` |
| `IWebViewPreloadScripts` | `AddPreloadScriptAsync`, `RemovePreloadScriptAsync` | `IWebViewScript` |
| `IWebViewNativeHandle` | `TryGetWebViewHandleAsync` | `IWebViewFeatures` |
| `IWebViewDownloads` | `DownloadRequested` event | `IWebViewFeatures` |
| `IWebViewPermissions` | `PermissionRequested` event | `IWebViewFeatures` |
| `IWebViewContextMenu` | `ContextMenuRequested` event | `IWebViewFeatures` |
| `IWebViewPopupWindows` | `NewWindowRequested` event | `IWebViewFeatures` |
| `IWebViewResourceInterception` | `WebResourceRequested`, `EnvironmentRequested` events | `IWebViewFeatures` |
| `IWebViewLifecycleEvents` | `AdapterCreated`, `AdapterDestroyed` events | `IWebViewFeatures` |

### Unchanged Surface

- `IWebView` still derives from `IWebViewNavigation + IWebViewScript + IWebViewBridge + IWebViewFeatures` — no member visible to consumers moves or changes.
- `IWebViewFeatures` is kept as an empty interface that inherits the 12 capability interfaces. Every member signature is identical.
- `IWebViewScript` keeps `InvokeScriptAsync` plus, via inheritance from `IWebViewPreloadScripts`, the two preload methods.
- `IWebViewBridge` is **not** modified in this change — its `TryGet*Manager` and `ChannelId` cleanup is tracked separately for v2.0.

### Migration

No action required. Existing code compiles unchanged. New code can now declare dependencies on a single capability (e.g. `ctor(IWebViewDevTools dt)`) instead of `IWebView`, enabling narrower unit-test doubles and cleaner Dependency Injection registrations.
