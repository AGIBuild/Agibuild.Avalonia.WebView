## Context

Phase 5 completed typed bidirectional system-integration flow, but three contract gaps remain unresolved across shell governance and template experience: whether `ShowAbout` is part of explicit action allowlist, whether inbound tray events can carry platform raw payloads, and how menu pruning composes with session permission profiles. The change is cross-cutting (runtime, bridge contracts, template markers, and governance tests) and directly affects deterministic policy behavior.

## Goals / Non-Goals

**Goals:**
- Define deterministic `ShowAbout` allowlist behavior with explicit deny semantics.
- Define a bounded tray event payload envelope: required semantic fields + constrained metadata.
- Define federated menu pruning order using permission profiles and shell policy with stable conflict semantics.
- Preserve machine-checkable diagnostics and CT/IT/governance verifiability.

**Non-Goals:**
- No expansion to bundled-browser full API parity.
- No fallback/dual-path behavior for legacy payload routing.
- No new host framework target support.
- No unrelated capability additions beyond the scoped contract gaps.

## Decisions

### Decision 1: `ShowAbout` remains explicit allowlist-controlled
- Choice: Keep `ShowAbout` as a typed system action that is denied unless present in the configured allowlist.
- Why: Preserves G3 policy-first invariants and avoids implicit host capability expansion.
- Alternative considered: always-on `ShowAbout` default allow (rejected: inconsistent with explicit allowlist model).

### Decision 2: Tray inbound payload uses semantic-first envelope
- Choice: Inbound tray events expose canonical semantic fields as primary contract; optional platform metadata is carried in a bounded typed envelope with explicit key/value constraints.
- Why: Maintains cross-platform portability while allowing controlled diagnostics context.
- Alternative considered: raw payload passthrough (rejected: non-deterministic and weakly governable).

### Decision 3: Menu pruning uses federated decision order
- Choice: Evaluate pruning with deterministic composition order: profile decision -> shell policy decision -> effective menu mutation.
- Why: Enforces single responsibility and predictable deny precedence for governance.
- Alternative considered: independent, unordered policy/profile checks (rejected: branch ambiguity and non-deterministic outcomes).

### Decision 4: Diagnostics must encode federation and payload boundaries
- Choice: Structured diagnostics include profile identity, policy stage, pruning decision source, and metadata envelope summary fields.
- Why: Ensures CI/automation/agent workflows can assert branch correctness without log interpretation.

## Risks / Trade-offs

- [Risk] Bounded metadata envelope may still drift across platforms -> Mitigation: explicit schema constraints and governance marker tests.
- [Risk] Federated pruning may increase policy complexity -> Mitigation: strict evaluation order and deterministic conflict precedence tests.
- [Risk] `ShowAbout` deny defaults may surprise template adopters -> Mitigation: app-shell preset marker demonstrates explicit allowlist registration path.

## Migration Plan

1. Add delta requirements for four capabilities (`shell-system-integration`, `webview-host-capability-bridge`, `webview-shell-experience`, `template-shell-presets`).
2. Implement runtime contract updates for allowlist, payload envelope, and federated pruning order.
3. Update template markers and demo flow to reflect explicit allowlist + federated pruning.
4. Add CT/IT/governance matrix rows and diagnostics assertions.
5. Validate via focused unit/integration automation lanes and OpenSpec strict validation.

Rollback:
- Disable new profile-federated pruning wiring and keep existing deterministic pruning baseline.
- Keep deny-first allowlist behavior (no rollback to implicit allow path).

## Open Questions

- Should metadata envelope enforce a global size budget per event for all platforms?
- Should profile identity in diagnostics include profile version/hash for traceability?
- Should app-shell template surface a minimal configuration snippet for `ShowAbout` allowlist opt-in by default?
