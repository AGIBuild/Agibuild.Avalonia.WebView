## 1. Contract and model definition

- [x] 1.1 Define `IDeepLinkRegistrationService` and canonical activation envelope models in runtime contracts (Deliverable: shell-deep-link-registration; Acceptance: contracts compile and expose typed registration + canonical activation fields).
- [x] 1.2 Define deterministic validation result and duplicate-outcome metadata types for activation ingress (Deliverable: shell-deep-link-registration; Acceptance: invalid declaration/payload and duplicate outcomes are representable without platform-specific types).

## 2. Runtime activation ingress pipeline

- [x] 2.1 Implement native activation normalization pipeline from platform payload to canonical envelope (Deliverable: shell-deep-link-registration; Acceptance: equivalent URI variants normalize to equivalent canonical route fields).
- [x] 2.2 Integrate policy admission checks before orchestration dispatch (Deliverable: shell-deep-link-registration; Acceptance: deny path blocks dispatch and returns deterministic reason metadata).
- [x] 2.3 Implement idempotency window enforcement for ingress duplicates (Deliverable: shell-deep-link-registration; Acceptance: duplicate payload within replay window does not trigger second dispatch).

## 3. Orchestration integration updates

- [x] 3.1 Extend activation coordinator to accept canonical native envelopes while preserving existing primary/secondary ownership semantics (Deliverable: shell-activation-orchestration; Acceptance: primary receives exactly once when active, failure is deterministic when no primary exists).
- [x] 3.2 Handle overlap between native ingress and secondary forwarding using shared duplicate semantics (Deliverable: shell-activation-orchestration; Acceptance: equivalent payload across both paths dispatches at most once).

## 4. Platform entrypoint wiring and diagnostics

- [x] 4.1 Add platform adapter entrypoint hooks that map native deep-link startup/activation events into runtime ingress API (Deliverable: shell-deep-link-registration; Acceptance: each supported platform has a deterministic mapping path or explicit not-supported marker).
- [x] 4.2 Emit structured diagnostics for registration validation, policy denial, duplicate suppression, and dispatch outcomes (Deliverable: shell-activation-orchestration; Acceptance: diagnostics include stable event type, outcome, and correlation identifiers).

## 5. Compatibility evidence and tests

- [x] 5.1 Add contract tests for registration validation, canonicalization, policy admission, duplicate suppression, and orchestration integration (Deliverable: shell-deep-link-registration + shell-activation-orchestration; Acceptance: CT suite covers happy path + malformed + deny + duplicate branches).
- [x] 5.2 Add platform integration smoke tests for protocol activation to primary dispatch flow where executable environments are available (Deliverable: webview-compatibility-matrix; Acceptance: IT evidence links to per-platform deep-link support claims).
- [x] 5.3 Update compatibility matrix entries and governance mapping for deep-link native registration parity (Deliverable: webview-compatibility-matrix; Acceptance: matrix row lists platform status and traceable CT/IT tokens).
