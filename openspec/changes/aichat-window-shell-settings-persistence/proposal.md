## Why

`avalonia-ai-chat` currently keeps window appearance settings in memory only. After users toggle transparency and click save, settings can appear to revert across lifecycle boundaries and are lost on restart, creating unstable UX and making titlebar behavior debugging harder.

This should be fixed now as Phase 11 quality hardening for sample DX consistency and to reinforce host/web shell state determinism.

## What Changes

- Persist AI chat `WindowShellSettings` to local JSON in the sample desktop host.
- Load persisted settings at startup and apply them to `WindowShellService` before web shell bootstrap.
- Persist normalized/applied settings on every `UpdateWindowShellSettings` call.
- Add regression checks to ensure sample wiring keeps persistence behavior and system chrome path.

## Capabilities

### New Capabilities

- `aichat-window-shell-settings-persistence`: Provides local persistence lifecycle for sample appearance settings (`themePreference`, `enableTransparency`, `glassOpacityPercent`).

### Modified Capabilities

- `ai-streaming-sample`: Extend sample requirements to include durable shell appearance settings across app restarts.

## Non-goals

- No change to core runtime bridge protocol or security policy pipeline.
- No framework-wide persistence in `WindowShellService` for all applications.
- No cloud sync or cross-device settings management.

## Impact

- **Code**: `samples/avalonia-ai-chat/AvaloniAiChat.Desktop/MainWindow.axaml.cs`, new sample persistence helper in desktop project, and related tests.
- **Spec**: delta requirement under `ai-streaming-sample`.
- **Goal alignment**: supports **G2** (stable SPA-hosted shell behavior) and **G4** (testable deterministic state behavior).
- **Roadmap alignment**: post-Phase-11 ecosystem sample quality hardening, improving developer trust in reference apps.
