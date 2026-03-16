## Why

`avalonia-ai-chat` sample currently has cross-layer titlebar drift: desktop window title text and web header title are not governed as one contract, and web fallback titlebar height (`44`) is inconsistent with host drag region height (`28`). This causes first-render misalignment and weakens confidence in the shell-window-chrome integration path.

This should be fixed now as post-Phase-11 quality hardening to preserve developer experience consistency and keep the sample aligned with hybrid-shell best practices.

## What Changes

- Standardize title text semantics between desktop host window title and AI chat web header title.
- Align web titlebar fallback height with host drag-region height to remove `44 vs 28` layout drift during bridge-not-ready window.
- Add requirement-level contract coverage in OpenSpec for sample title/titlebar consistency.
- Add/adjust tests to guard against future regression in sample titlebar fallback contract.

## Capabilities

### New Capabilities

- `aichat-titlebar-consistency`: Defines a deterministic cross-layer contract for AI chat sample title text and titlebar fallback metrics.

### Modified Capabilities

- `ai-streaming-sample`: Adds normative requirements for desktop/window title alignment and titlebar fallback-to-drag-region consistency in the sample.

## Non-goals

- No change to bridge transport protocol, security policy pipeline, or runtime message semantics.
- No redesign of generic shell-window-chrome framework behavior.
- No compatibility shim for multiple titlebar fallback values.

## Impact

- **Code**: `samples/avalonia-ai-chat/AvaloniAiChat.Desktop/MainWindow.axaml`, `samples/avalonia-ai-chat/AvaloniAiChat.Web/src/App.tsx`, `samples/avalonia-ai-chat/AvaloniAiChat.Web/src/index.css`, related unit tests.
- **Specs**: delta under `ai-streaming-sample`.
- **Goal alignment**: strengthens **G2** (First-Class SPA Hosting) by keeping host+web shell composition coherent, and **G4** (Contract-Driven Testability) by adding regression assertions.
- **Roadmap alignment**: supports Phase 11 Ecosystem/DX outcomes and post-completion maintenance hardening toward Phase 12 readiness.
