## Context

The runtime already supports JSON-RPC bridge calls, shell policy orchestration, and SPA hosting. Functional gaps remain:

1. Bridge binary payloads are transport-capable but not ergonomic for generated JS/TS APIs.
2. Shell lacks a deterministic in-process activation coordinator for deep links and single-instance handoff.
3. SPA hosting lacks a signed update workflow with safe activation rollback.

## Goals

- Provide first-class generated bridge binary ergonomics without introducing dual legacy paths.
- Provide deterministic activation orchestration contracts that hosts can adopt immediately.
- Provide signed update + rollback primitives to support production web asset hot updates.

## Non-goals

- Cross-process IPC protocol standardization.
- Remote update fetch/distribution.
- Legacy compatibility shims for old bridge binary shapes.

## Decisions

### 1) Bridge binary mapping

- TypeScript mapping for `byte[]` becomes `Uint8Array`.
- Generated JS methods encode `Uint8Array` parameters to base64 before RPC invoke.
- Generated JS methods decode base64 return values to `Uint8Array` for binary-returning methods.
- JSON-RPC transport remains unchanged (string-based JSON payloads).

### 2) Shell activation orchestration

- Introduce a single owner service for app activation flow:
  - Primary registration acquisition per app identity.
  - Secondary activation forwarding to current primary handler.
  - Deterministic deep-link validation + dispatch ordering.
- Keep this runtime-only and host-driven; platform registration remains external.

### 3) SPA asset hot update

- Introduce a signed package update service:
  - Validate package digest/signature before extraction.
  - Stage assets under versioned directory.
  - Atomically switch active version pointer.
  - Rollback to previous active version on activation failure.
- SPA hosting can optionally serve from active external asset directory when configured.

## Risks and Mitigations

- **Binary conversion regressions**: enforce generated stub tests for parameter and return conversion.
- **Activation race conditions**: centralize ownership in thread-safe coordinator; test concurrent registration/forwarding semantics.
- **Update corruption**: fail closed on signature mismatch; never activate unverified payload.

## Test Strategy

- Bridge: generator + runtime tests for byte[]/Uint8Array path and failure semantics.
- Shell: activation coordinator tests for primary/secondary flows and lifecycle edge cases.
- SPA: signature verification, atomic activation, rollback behavior tests.
