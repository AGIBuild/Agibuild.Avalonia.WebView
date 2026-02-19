## Why

M4.3 delivered typed host capability bridge, but session and permission governance is still coarse-grained. M4.4 is required to provide per-window/per-scope profiles so enterprises can enforce auditable isolation and permission policy across multi-window shell workflows (ROADMAP Deliverable 4.4, aligned with G3/G4).

## What Changes

- Introduce explicit session/permission profile contracts with stable profile identity and deterministic evaluation semantics.
- Add profile resolution flow for root and child windows, including inheritance/override rules tied to window and scope context.
- Extend shell runtime permission handling to apply profile decisions before fallback behavior.
- Define traceability requirements for profile evaluation and policy outcomes in CT/IT automation.

## Non-goals

- Platform-specific OS permission backends beyond existing adapter abstractions.
- Full enterprise policy distribution/remote management service.
- Replacing M4.1/M4.2 baseline contracts or changing opt-in defaults.

## Capabilities

### New Capabilities
- `webview-session-permission-profiles`: Defines profile model, resolution semantics, and deterministic permission/session governance for shell-managed windows.

### Modified Capabilities
- `shell-session-policy`: Extend session policy semantics with profile-based inheritance/override and auditable profile identity correlation.
- `webview-shell-experience`: Extend permission governance to route through profile resolution pipeline before runtime fallback.
- `webview-multi-window-lifecycle`: Add deterministic parent-child profile propagation behavior for managed windows.

## Impact

- **Roadmap alignment**: Implements **Phase 4 M4.4 / Deliverable 4.4** (`Session/permission profiles and governance rules`).
- **Goal alignment**: Strengthens **G3 (Secure by Default)** with explicit least-privilege profile controls, and **G4 (Contract-Driven Testability)** through deterministic CT/IT profile assertions.
- **Affected systems**: Shell runtime policy orchestration, session/permission contracts, multi-window context propagation, and automation test lanes.
