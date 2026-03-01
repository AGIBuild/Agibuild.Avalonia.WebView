## Why

Phase 8 completed bridge/runtime parity, but product-facing capabilities are still missing in three high-value areas: binary payload ergonomics, deep-link/single-instance activation orchestration, and signed web asset hot updates.

Delivering these together advances framework adoption with concrete functional value while preserving deterministic contracts and testability.

## What Changes

- Add typed binary payload ergonomics for bridge calls (Uint8Array ↔ byte[] contract, including generated JS/TS surfaces).
- Add host-side deep link and single-instance activation orchestration primitives for shell-level lifecycle routing.
- Add signed SPA asset package update + rollback service for production web asset hot update workflows.
- Add/extend unit tests for main path, edge cases, and failure paths for all three tracks.

## Capabilities

### New Capabilities
- `shell-activation-orchestration`: Deterministic deep-link and single-instance activation routing contract for host shells.
- `spa-asset-hot-update`: Signed package validation, activation, and rollback contract for SPA assets.
- `bridge-binary-payload`: Binary payload mapping contract for bridge-generated JS/TS and runtime transport.

### Modified Capabilities
- `bridge-typescript-generation`: Add byte-array TypeScript shape and generated method-level binary conversion semantics.
- `bridge-contracts`: Clarify binary payload support in generated stubs/proxies.
- `spa-hosting`: Add optional runtime path to consume activated external asset package directory.
- `webview-shell-experience`: Extend shell feature surface with activation orchestration integration points.

## Impact

- Bridge generator/runtime: `src/Agibuild.Fulora.Bridge.Generator/*`, `src/Agibuild.Fulora.Runtime/WebViewRpcService.cs`.
- Shell runtime: new activation orchestration service under `src/Agibuild.Fulora.Runtime/Shell/`.
- SPA runtime: new hot update service and optional hosting integration in `src/Agibuild.Fulora.Runtime/*`.
- Tests: bridge generation/runtime tests, shell tests, SPA hosting/update tests.

## Non-goals

- No transport protocol switch away from JSON-RPC.
- No platform-native deep-link registration implementation (focus on orchestration contracts).
- No remote update distribution backend; only local signed package application and rollback semantics.
