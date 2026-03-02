## Context

The host capability bridge (`WebViewHostCapabilityBridge`) already defines typed operations for system tray (`TrayUpdateState`, `TrayInteractionEventDispatch`) and native menu (`MenuApplyModel`, `MenuInteractionEventDispatch`) with full policy governance, diagnostic emission, and metadata envelope validation. The `IWebViewHostCapabilityProvider` interface declares `UpdateTrayState(WebViewTrayStateRequest)` and `ApplyMenuModel(WebViewMenuModelRequest)` methods.

However, the template's `TemplateHostCapabilityProvider` implements these as no-ops. No Avalonia-specific binding exists to translate these abstract operations into real `TrayIcon` and `NativeMenu` instances.

The existing architecture already handles:
- Policy evaluation before provider execution
- Diagnostic emission for allow/deny/failure outcomes
- Inbound event dispatch (tray interaction → web content)
- Metadata envelope validation for inbound events

What's missing: a concrete Avalonia provider that bridges the gap between the abstract capability model and Avalonia's `TrayIcon`/`NativeMenu` APIs.

## Goals / Non-Goals

**Goals:**
- Provide a production-ready `AvaloniaHostCapabilityProvider` that implements `IWebViewHostCapabilityProvider` with real Avalonia `TrayIcon` and `NativeMenu` bindings
- Support declarative menu model → Avalonia `NativeMenu` tree mapping with recursive item construction
- Support tray icon management with visibility, tooltip, icon source, and click event handling
- Dispatch `TrayInteractionEventDispatch` and `MenuInteractionEventDispatch` inbound events through the existing capability bridge when users interact with tray/menu
- Maintain full contract-testability via mock provider pattern

**Non-Goals:**
- Modifying existing `WebViewHostCapabilityBridge` contracts or `IWebViewHostCapabilityProvider` interface
- Custom tray context menus (use `MenuApplyModel` instead)
- Animated tray icons, badge counts, or progress indicators
- Platform-specific tray APIs beyond what Avalonia `TrayIcon` exposes

## Decisions

### D1: Single provider class vs separate tray/menu providers

**Choice**: Single `AvaloniaHostCapabilityProvider` class implementing `IWebViewHostCapabilityProvider`.

**Rationale**: The interface is already a single contract. Splitting would require adapter composition, adding unnecessary complexity. The provider delegates to focused helper classes (`AvaloniaTrayManager`, `AvaloniaMenuManager`) for separation of concerns within the single provider.

**Alternatives considered**:
- Separate `AvaloniaTrayProvider` + `AvaloniaMenuProvider` composed via decorator — rejected because `IWebViewHostCapabilityProvider` is a flat interface, composition would need a routing layer.

### D2: Icon resolution strategy

**Choice**: `ITrayIconResolver` abstraction with built-in implementations for embedded resources, file paths, and platform-adaptive icons.

**Rationale**: Avalonia `TrayIcon` accepts `WindowIcon` which wraps platform-specific icon formats. Icon resolution must handle:
- Embedded resources (`avares://`)
- File paths (for dynamic icons)
- Platform differences (ICO on Windows, PNG on macOS/Linux)

### D3: Menu model mapping — recursive vs flat

**Choice**: Recursive mapping from `WebViewMenuItemModel` tree to `NativeMenuItem` tree.

**Rationale**: `WebViewMenuItemModel` already has `Children` property for hierarchical menus. Avalonia `NativeMenu` supports nested `NativeMenuItem` with submenus. Direct 1:1 recursive mapping is natural and lossless.

### D4: Inbound event dispatch — who triggers it

**Choice**: `AvaloniaHostCapabilityProvider` subscribes to Avalonia `TrayIcon.Clicked` and `NativeMenuItem.Click` events, then calls back into `WebViewShellExperience` to dispatch inbound events through the existing bridge path.

**Rationale**: Keeps event flow unidirectional: Avalonia UI → Provider → Shell → Bridge → JS. The provider needs a callback reference to the shell experience, injected via constructor or event delegate.

### D5: Thread safety

**Choice**: All Avalonia UI operations dispatched to the UI thread via `Dispatcher.UIThread.InvokeAsync`.

**Rationale**: Avalonia requires UI operations on the main thread. Bridge calls arrive on arbitrary threads. The provider must marshal all `TrayIcon`/`NativeMenu` mutations to the UI thread.

## Testing Strategy

- **Contract tests**: Mock `IWebViewHostCapabilityProvider` with `AvaloniaHostCapabilityProvider` driven by deterministic request objects — verify tray/menu state transitions, icon resolution, menu tree construction
- **Unit tests**: `AvaloniaTrayManager` and `AvaloniaMenuManager` tested in isolation with mock Avalonia services
- **Integration tests**: Representative tray show/hide + menu apply/click round-trip through full capability bridge pipeline

## Risks / Trade-offs

- **[Platform variance]** Avalonia `TrayIcon` behavior differs across platforms (no tray on mobile, limited on Linux Wayland) → Mitigation: capability provider returns platform-unsupported result for non-desktop platforms, tested via contract tests
- **[Icon format]** Windows expects ICO, macOS expects PNG/ICNS → Mitigation: `ITrayIconResolver` handles format conversion; document supported formats
- **[Lifecycle coupling]** Tray icon must be disposed when the application exits → Mitigation: provider implements `IDisposable`, wired into application shutdown
