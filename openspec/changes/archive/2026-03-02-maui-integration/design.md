## Context

Fulora's Avalonia host uses an adapter pattern: a host-neutral runtime (bridge, SPA hosting, contracts) is coupled to Avalonia's WebView control via an adapter. MAUI provides its own `WebView` control with different lifecycle, navigation, and scripting APIs. The bridge and SPA hosting logic are platform-agnostic; only the WebView binding layer is platform-specific.

**Gap**: No MAUI adapter exists. MAUI developers cannot use Fulora without reimplementing the integration.

## Goals / Non-Goals

**Goals:**
- Create MAUI adapter implementing host-neutral contracts (e.g., `IWebViewHost`, `IWebViewLifecycle`)
- Support bridge invocation and SPA hosting in MAUI apps
- Follow adapter pattern consistent with Avalonia host

**Non-Goals:**
- Full Avalonia feature parity (e.g., multi-window, platform-specific features) in v1
- Modifying core bridge for MAUI
- Supporting MAUI-specific WebView quirks beyond adapter scope

## Decisions

### D1: Package structure

**Choice**: New package `Agibuild.Fulora.Maui` referencing `Agibuild.Fulora` core and `Microsoft.Maui`. Multi-target for net8.0-android, net8.0-ios, net8.0-maccatalyst, net8.0-windows10.0.19041.

**Alternatives considered**:
- Single package with both Avalonia and MAUI: Increases package size, complicates dependencies
- MAUI as part of core: Violates separation of host adapters

**Rationale**: Separate package keeps dependencies clean; consumers choose only what they need.

### D2: Adapter abstraction

**Choice**: Define or reuse `IWebViewHost` (or equivalent) in core; MAUI adapter implements it by wrapping `Microsoft.Maui.Controls.WebView` and bridging to MAUI's `EvaluateJavaScriptAsync` / navigation events.

**Rationale**: Mirrors Avalonia adapter; core stays host-agnostic.

### D3: Platform support

**Choice**: Support Android, iOS, Windows, macOS (Mac Catalyst) where MAUI WebView is available. Document platform-specific behavior (e.g., Android WebView vs. WebView2 on Windows).

**Rationale**: MAUI's WebView support varies by platform; document rather than hide differences.

## Risks / Trade-offs

- **[Risk] MAUI WebView API differences** → Each platform uses different WebView implementations. Adapter must abstract navigation, script execution, and lifecycle consistently.
- **[Risk] SPA asset serving on mobile** → Embedded resources work; dev server proxying may need platform-specific handling (e.g., localhost on emulator).
- **[Trade-off] Delayed platform parity** → Start with Windows/macOS; mobile platforms can follow if API permits.
