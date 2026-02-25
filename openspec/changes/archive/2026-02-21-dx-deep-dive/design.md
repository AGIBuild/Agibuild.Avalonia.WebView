## Context

The repository already generates `bridge.d.ts`, but consumers still lack a first-class npm runtime package and sample parity proving "web-first" onboarding beyond React.  
This change focuses on DX closure without changing bridge protocol semantics.

## Goals

- Provide in-repo source for `@agibuild/bridge` with zero-runtime-dependency client APIs.
- Validate generated TypeScript declarations through actual `tsc` compile checks.
- Add Vue sample parity to reduce frontend-framework lock-in.
- Ensure template debug startup path remains deterministic and testable.

## Decisions

### D1. Introduce `packages/bridge` workspace package

- Add `packages/bridge/package.json`, `tsconfig.json`, and `src/index.ts`.
- Export minimal stable surface: `createBridgeClient`, `BridgeClient`, `BridgeReadyOptions`.
- Keep implementation transport-agnostic, relying on `window.__agibuildBridge` message path.

### D2. Add declaration compile governance

- Add a governance test that compiles generated `bridge.d.ts` in a sample TypeScript project.
- Fail with actionable diagnostics when declarations are invalid or miss required bridge symbols.

### D3. Vue sample parity

- Add `samples/avalonia-vue` sample mirroring React sample host structure.
- Wire generated `bridge.d.ts` into Vue sample TS config and include one typed bridge roundtrip.

### D4. Template debug startup determinism

- Add template governance checks for debug startup conventions (dev server port/env handshake).
- Validate bridge typing and debug helper script presence in template web entrypoint.

## Alternatives Considered

### A1. Publish npm package directly from CI in this change

Rejected: release and token governance should be isolated to a later release operations change.

### A2. Keep Vue sample as docs-only snippet

Rejected: executable sample coverage is required for deterministic DX confidence (G4/E1).
