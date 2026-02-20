## 1. Shortcut Routing Public API

- [x] 1.1 Add `WebViewShortcutRouter` and shortcut binding/action contracts in `Agibuild.Avalonia.WebView` (Acceptance: default bindings cover shell editing commands and DevTools).
- [x] 1.2 Implement deterministic routing execution (`TryExecuteAsync`) with explicit non-handled semantics for unmapped or unsupported actions (Acceptance: no implicit fallback path).

## 2. Template App-Shell Wiring

- [x] 2.1 Update `MainWindow.AppShellPreset.cs` to initialize and use the shortcut router from `KeyDown` (Acceptance: mapped shortcuts set `e.Handled = true`).
- [x] 2.2 Ensure deterministic disposal detaches shortcut handler and releases router references (Acceptance: no event leak in preset lifecycle).

## 3. Tests and Governance

- [x] 3.1 Add automation tests for shortcut routing (primary flow + unmapped + missing command manager) (Acceptance: tests pass deterministically).
- [x] 3.2 Extend template governance markers test to assert shortcut router wiring exists (Acceptance: source marker assertions cover router init and key handler detach).
- [x] 3.3 Run impacted test suites and archive evidence (Acceptance: target and impacted suites pass, requirement traceability documented).
