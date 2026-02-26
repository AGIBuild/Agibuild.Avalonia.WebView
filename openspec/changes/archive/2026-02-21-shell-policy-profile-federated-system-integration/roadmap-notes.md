## Roadmap Notes (Phase 5 Alignment)

### In Scope (This Increment)

- Phase `5.2`: `ShowAbout` system action explicitly moved under typed allowlist governance with deny-by-default behavior.
- Phase `5.3`: inbound tray event metadata envelope became bounded and policy-first with deterministic deny semantics.
- Phase `5.3`: menu pruning now uses deterministic federated order (`profile -> policy -> mutation`) with short-circuit deny and failure isolation.
- Phase `5.4`: app-shell template demonstrates federated pruning result path (`profile identity`, `permission state`, `pruning stage`) and bounded metadata consumption.
- Phase `5.5`: CT matrix and integration automation expanded with combined scenario coverage for whitelist + metadata boundary + federated pruning.

### Out of Scope (Explicit Boundary)

- No bundled-browser full API parity expansion (plugin/runtime ecosystem, installer/update pipeline, Chromium API compatibility target).
- No additional host framework targets (WPF/WinForms/MAUI).
- No fallback/dual-path compatibility routing.
- No widening of system action surface beyond explicit typed action + allowlist governance model.
