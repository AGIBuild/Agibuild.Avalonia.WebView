## Context

The app-shell template already demonstrates typed capability calls, but developers and AI agents still need manual, repetitive steps to verify policy/whitelist branches. Plan B prioritizes template usability and deterministic demo validation before deeper observability work.

## Goals / Non-Goals

**Goals:**
- Add a visible strategy panel in the template demo for system integration outcomes.
- Provide one-click ShowAbout deny/allow scenario toggles suitable for iterative web-first development.
- Expose a reusable regression script function that can be invoked by automation or AI agent workflows.
- Cover this flow with governance assertions and focused tests.

**Non-Goals:**
- No changes to core bridge policy model.
- No external service dependency for diagnostics storage.
- No backward compatibility layer for pre-v2 template demo behavior.

## Decisions

### Decision 1: Keep scenario switch at template host boundary
- Choice: Drive ShowAbout scenario through environment-based host toggle and expose current mode in demo panel.
- Rationale: Keeps secure-by-default baseline while avoiding runtime fallback clutter.

### Decision 2: Add deterministic UI output contract
- Choice: Render structured one-line status payloads for each strategy action (`mode`, `action`, `outcome`, `reason`).
- Rationale: Enables machine parsing and stable assertions.

### Decision 3: Ship reusable demo regression function in template web bundle
- Choice: Add a script entry (`window.runTemplateRegressionChecks`) that executes canonical checks and returns structured results.
- Rationale: Low-friction validation path for AI agents and onboarding.

## Risks / Trade-offs

- [Risk] Demo UI complexity may obscure baseline path.  
  → Mitigation: keep panel compact and deterministic text format only.
- [Risk] Environment toggle misuse in production sample copy.  
  → Mitigation: explicit marker comments + governance assertions for default deny.
- [Risk] Script output drift breaking agent consumers.  
  → Mitigation: fixed keys and test assertions for payload structure.

## Testing Strategy

- Unit/governance tests assert panel markers, toggle markers, and regression script marker.
- Integration smoke validates ShowAbout deny/allow behavior with strategy panel output path.
- Full gates: `nuke Test`, coverage check, and strict OpenSpec validation before archive.
