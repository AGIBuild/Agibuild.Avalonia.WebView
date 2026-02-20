## Why

Clipboard/file dialogs/external-open are already policy-governed, but shell-level shortcut command execution (`copy/cut/paste/select-all/undo/redo`) is still outside shell governance. This leaves a gap in system-integration consistency for application-shell workflows.

## What Changes

- Add shell command policy contracts for standard editing commands.
- Add shell command execution entry point that routes `WebViewCommand` through policy-first evaluation and deterministic failure reporting.
- Add unit and automation integration tests for allow/deny/isolation behavior.

## Non-goals

- Global hotkey registration and OS-level accelerator management.
- Additional editor command sets beyond existing `WebViewCommand`.
- Legacy fallback compatibility branches.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `webview-shell-experience`: Extend shell policy model with command/shortcut governance for standard editing commands.

## Impact

- **Roadmap alignment**: Completes remaining "system integration (shortcuts)" gap under Phase 4 application-shell capability trajectory.
- **Goal alignment**: Advances **G3** (explicit policy governance) and **G4** (contract-testable deterministic behavior).
- **Affected systems**: `WebViewShellExperience` policy orchestration and shell unit/automation integration tests.
