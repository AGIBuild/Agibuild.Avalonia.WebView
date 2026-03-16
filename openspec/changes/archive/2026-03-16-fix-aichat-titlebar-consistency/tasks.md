## 1. OpenSpec Artifact Completion

- [x] 1.1 Finalize `proposal.md` with Goal/ROADMAP alignment and explicit non-goals for titlebar consistency hardening.
- [x] 1.2 Finalize `design.md` with cross-layer decisions for title text consistency and fallback metric unification.
- [x] 1.3 Finalize `specs/ai-streaming-sample/spec.md` delta requirements for title identity and fallback-height contract.

## 2. Desktop and Web Consistency Implementation

- [x] 2.1 Update desktop sample window title text in `AvaloniAiChat.Desktop/MainWindow.axaml` to align with sample title identity requirement.
- [x] 2.2 Update web fallback titlebar height in `AvaloniAiChat.Web/src/App.tsx` to match host drag-region height.
- [x] 2.3 Update CSS fallback token in `AvaloniAiChat.Web/src/index.css` to the same titlebar height baseline.

## 3. Regression Tests and Verification

- [x] 3.1 Add or adjust unit test assertions to guard AI chat titlebar metric wiring and fallback consistency.
- [x] 3.2 Run targeted validation (`dotnet test` for unit tests and web `npm run build`) and record pass results.
- [x] 3.3 Run lint diagnostics for touched files and resolve any newly introduced issues.
