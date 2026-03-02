## Why

The host capability bridge already defines typed contracts for system tray (`TrayUpdateState`) and native menu (`MenuApplyModel`) operations with full policy governance and diagnostic support. However, the `IWebViewHostCapabilityProvider` implementations in the template and samples are no-ops — no real Avalonia `TrayIcon` or `NativeMenu` binding exists. This means developers cannot use tray or menu capabilities without writing platform-specific code themselves, undermining the "web-first developer speed" promise.

**Goal IDs**: G1 (Type-Safe Bridge — extending typed capability surface), E1 (Project Template — out-of-box shell capabilities), G3 (Secure by Default — policy-governed tray/menu access)

**ROADMAP justification**: Post-Phase 9 (1.0 GA released). System tray and native menu are essential desktop application capabilities that differentiate Fulora from Electron by offering declarative, type-safe, policy-governed system integration vs. Electron's imperative `Tray`/`Menu` API.

## What Changes

- Implement an Avalonia-based `IWebViewHostCapabilityProvider` that binds `TrayUpdateState` to Avalonia `TrayIcon` (icon, tooltip, visibility, click events)
- Implement `MenuApplyModel` binding to Avalonia `NativeMenu` / `NativeMenuItem` tree with recursive model mapping
- Wire `TrayInteractionEventDispatch` and `MenuInteractionEventDispatch` inbound events when users click tray icon or select menu items
- Provide cross-platform icon resolution (embedded resource, file path, platform-adaptive)
- Add contract tests for the Avalonia provider bindings and integration tests for representative tray/menu flows

## Non-goals

- Custom tray popup menus (use native menu model instead)
- Animated tray icons or badge counts (future enhancement)
- Platform-specific menu behaviors beyond what Avalonia NativeMenu exposes

## Capabilities

### New Capabilities
- `avalonia-tray-menu-provider`: Avalonia-specific `IWebViewHostCapabilityProvider` implementation that binds system tray and native menu operations to Avalonia `TrayIcon` and `NativeMenu` controls with bidirectional event dispatch

### Modified Capabilities
- None (existing `webview-host-capability-bridge` contracts remain unchanged; this adds an implementation, not new requirements)

## Impact

- `src/Agibuild.Fulora.Avalonia/` — new provider class(es) for tray and menu bindings
- `src/Agibuild.Fulora.Core/` — possible icon resolution utilities
- `templates/agibuild-hybrid/` — wire tray/menu provider in app-shell preset
- `tests/` — contract tests for Avalonia provider, integration tests for tray/menu round-trip
- `samples/avalonia-react/` — add tray + menu demonstration
