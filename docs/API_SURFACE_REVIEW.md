# API Surface Review — Agibuild.Fulora

**Date**: 2026-02-17  
**Status**: Pre-1.0 Audit  
**Test Count**: 525 CTs passing

---

## Summary

The public API surface is clean and well-structured. A few consistency gaps exist between `IWebView` interface and concrete implementations.

## Key Findings

### 1. IWebView Interface Gap (Medium)

The following members exist on `WebViewCore` and `WebView` but are **not** on `IWebView`:

| Member | Category | Recommendation |
|--------|----------|----------------|
| `ZoomFactor` + `ZoomFactorChanged` | Feature | Promote to IWebView for 1.0 |
| `FindInPageAsync` + `StopFindInPage` | Feature | Promote to IWebView for 1.0 |
| `AddPreloadScript` + `RemovePreloadScript` | Feature | Promote to IWebView for 1.0 |
| `ContextMenuRequested` | Event | Promote to IWebView for 1.0 |
| `SetCustomUserAgent` | Config | Keep on concrete types (init-time) |
| `EnableWebMessageBridge` / `DisableWebMessageBridge` | Config | Keep on concrete types (advanced) |
| `EnableSpaHosting` | Config | Keep on concrete types (advanced) |

**Rationale**: Features like zoom, find, preload scripts are commonly needed by consumers. Config-level methods are setup-only and don't belong on the abstraction.

### 2. WebView Missing EnableSpaHosting (Medium)

`EnableSpaHosting` is only on `WebViewCore`, not on the `WebView` Avalonia control. Consumers using the control must access `_core` (internal). **Recommendation**: Add a convenience method or use WebViewEnvironmentOptions pattern.

### 3. Duplicate XML Doc (Fixed)

`ContextMenuMediaType` had two `<summary>` tags. **Fixed** in this audit.

### 4. Experimental APIs

| Type | Attribute | Status |
|------|-----------|--------|
| `ICookieManager` | `[Experimental("AGWV001")]` | Keep for 1.0 preview |
| `WebResourceRequestedEventArgs` | `[Experimental("AGWV004")]` | Remove for 1.0 (stable now) |
| `EnvironmentRequestedEventArgs` | `[Experimental("AGWV005")]` | Keep (still evolving) |

### 5. MockBridgeService.Dispose

`MockBridgeService` has `Dispose()` but `IBridgeService` does not extend `IDisposable`. This is fine for test convenience but document it.

### 6. Event Loss on WebView Control

`WebView` control events added before adapter attach are silently lost (no-op add/remove when `_core` is null). This is by design for Avalonia lifecycle but should be documented.

### 7. Type Naming Consistency ✅

All naming follows .NET conventions:
- `I*` for interfaces
- `*Attribute` for attributes
- `*EventArgs` for events
- PascalCase throughout

### 8. Experimental Attributes Summary

| Code | Description | Should Graduate? |
|------|-------------|-----------------|
| AGWV001 | Cookie Management | Not yet — platform gaps |
| AGWV004 | Web Resource Interception | Yes — stable and widely used |
| AGWV005 | Environment Requested | Not yet — placeholder |

---

## Action Items for 1.0

| # | Action | Priority | Breaking? |
|---|--------|----------|-----------|
| 1 | Promote Zoom/Find/Preload/ContextMenu to IWebView | High | Additive only |
| 2 | Expose EnableSpaHosting on WebView control | Medium | Additive |
| 3 | Graduate AGWV004 from Experimental | Low | Non-breaking |
| 4 | Document event lifecycle on WebView control | Low | N/A |
| 5 | Version as 1.0.0-preview.1 → 1.0.0 | — | — |

---

## Evidence Pointers (API → Executable)

- **Navigation cancel semantics**: `tests/Agibuild.Fulora.UnitTests/ContractSemanticsV1NavigationTests.cs`
- **Native navigation cancel**: `tests/Agibuild.Fulora.UnitTests/ContractSemanticsV1NativeNavigationTests.cs`
- **WebMessage bridge policy/drop**: `tests/Agibuild.Fulora.UnitTests/WebViewCoreHotspotCoverageTests.cs`
- **DevTools toggle (core)**: `tests/Agibuild.Fulora.UnitTests/RuntimeCoverageTests.cs`
- **DevTools toggle (GTK smoke)**: `tests/Agibuild.Fulora.Integration.Tests/Agibuild.Fulora.Integration.Tests/ViewModels/GtkWebViewSmokeViewModel.cs`

---

## Public API Inventory (generated)

Regenerate:
- `dotnet build -c Release`
- `dotnet run --project tools/ApiSurfaceInventory/ApiSurfaceInventory.csproj -c Release -- --config=Release`

<details><summary>Inventory (Release)</summary>

```text
See `docs/API_SURFACE_INVENTORY.release.txt`.
```

</details>
