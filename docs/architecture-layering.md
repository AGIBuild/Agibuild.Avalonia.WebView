# Architecture Layering

## Allowed Dependencies

- `Kernel` depends only on BCL and internal kernel contracts.
- `Platform Services` may depend on `Kernel` but never on `Experience Extensions`.
- `Experience Extensions` may depend on `Kernel` and `Platform Services`.
- `Product Surface` may depend on all public APIs but cannot introduce reverse dependencies into lower layers.

## Allowed Public API Categories

- `Kernel API` — core lifecycle, scheduling, bridge contracts, and invariant enforcement.
- `Platform API` — host-neutral service abstractions (storage, shell, diagnostics, security policy).
- `Extension API` — framework adapters and plugin contracts with explicit support tier labels.
- `Product API` — template-facing composition APIs and app bootstrap surfaces.

## Classification Decision Tree

1. Does the API define runtime invariants used across hosts/frameworks?
   - Yes: classify as `Kernel API`.
2. Does the API expose host-neutral platform service behavior?
   - Yes: classify as `Platform API`.
3. Does the API bind to framework/host/plugin-specific behavior?
   - Yes: classify as `Extension API`.
4. Is the API only for app composition/template setup?
   - Yes: classify as `Product API`.
5. If none apply, keep internal and do not publish.

## Kernel API Architectural Approval Rule

- Any new public `Kernel API`, breaking `Kernel API` change, or semantic behavior change requires architecture approval before merge.
- Approval must include:
  - dependency-boundary impact statement,
  - compatibility plan,
  - rollback/fallback plan,
  - linked governance evidence for release gates.
