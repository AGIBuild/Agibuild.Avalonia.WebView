## Context

`shell-activation-orchestration` currently guarantees deterministic in-process ownership and forwarding, but it does not define how native OS deep-link entrypoints are normalized and admitted into that flow.

This leaves a production gap: hosts can orchestrate activation only after payloads already exist in-process. To close ROADMAP Phase 8 M8.5 (platform parity), we need a contract-level native registration/ingest boundary that remains deterministic, policy-governed, and testable (G2/G4).

## Goals / Non-Goals

**Goals:**
- Define a typed native activation ingress contract that all platforms map into before orchestration dispatch.
- Extend activation orchestration with idempotency and deterministic ordering across cold start and single-instance forwarding.
- Add explicit policy validation (scheme/host/path gates) before dispatch to application handlers.
- Keep behavior fully CT-testable with mock runtime surfaces plus targeted platform IT evidence updates.

**Non-Goals:**
- Implement installer or OS packaging automation for protocol registration.
- Implement universal-link/app-link trust-chain hosting and verification.
- Replace existing single-instance orchestration transport model.

## Decisions

### 1) Introduce `IDeepLinkRegistrationService` as host-facing boundary

**Chosen:** Add a runtime service contract responsible for:
- declaring supported deep-link routes/schemes
- validating activation payload shape
- converting native payload into canonical activation envelope

**Alternatives considered:**
- Extend existing coordinator directly with registration APIs (rejected: mixes ownership and registration concerns).
- Platform-specific direct dispatch bypassing runtime contract (rejected: loses cross-platform determinism and CT coverage).

### 2) Canonical activation envelope with deterministic idempotency key

**Chosen:** Define a canonical envelope (`ActivationId`, `Route`, `RawUri`, `Source`, `OccurredAtUtc`) and derive an idempotency key from stable canonical fields.

**Alternatives considered:**
- Raw URI string dedup only (rejected: unstable under equivalent URI normalization differences).
- Timestamp-only dedup (rejected: unsafe collisions and non-deterministic retries).

### 3) Policy-first admission before orchestration dispatch

**Chosen:** Route all native activation envelopes through policy checks before the primary-handler dispatch path.

**Alternatives considered:**
- Dispatch first, then policy notify (rejected: violates secure-by-default posture and creates side-effect race).
- Per-platform policy implementations (rejected: duplicates logic and weakens governance consistency).

## Risks / Trade-offs

- **[Platform URI normalization divergence]** -> Define canonical normalization rules at runtime boundary and enforce with cross-platform fixtures.
- **[Duplicate delivery during cold start + forwarding]** -> Use deterministic idempotency key with bounded replay window and explicit diagnostics.
- **[Over-strict policy blocks valid routes]** -> Provide explicit deny diagnostics and route-level allowlist configuration tests.

## Migration Plan

1. Introduce contracts and canonical envelope model behind additive APIs.
2. Wire platform adapters/entrypoints to emit native activation into the new ingress contract.
3. Enable policy admission and idempotent dispatch in coordinator path.
4. Update compatibility matrix evidence tokens for deep-link registration parity.
5. Rollback strategy: disable new ingress path via runtime feature flag and retain existing in-process forwarding path.

## Test Strategy

- **CT:** deterministic normalization, policy deny/allow paths, idempotent dispatch, ownership forwarding interplay.
- **IT:** per-platform native activation smoke (protocol launch -> canonical envelope -> primary dispatch).
- **Governance evidence:** compatibility-matrix parity rows and deterministic acceptance criteria linkage.

## Open Questions

- Should idempotency replay window be runtime-configurable or fixed by contract?
- Do we need a separate diagnostics capability id for activation-ingress policy denials?
