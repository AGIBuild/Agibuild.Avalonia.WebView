# bridge-integration-tests

**Goals**: G1 (Type-Safe Bridge), G4 (Testing)
**ROADMAP**: Phase 1, Deliverables 1.7 + 1.1c

## Problem

Phase 1 Bridge needs comprehensive edge-case and lifecycle testing, plus E2E validation against a real WebView. Unit tests cover contracts; integration tests cover multi-service scenarios, expose/remove cycles, disposed state, error boundaries, and concurrency.

## Proposed Change

1. **BridgeIntegrationTests**: 8 integration tests covering multi-service coexistence, Export+Import coexistence, expose/remove cycles, removed-service returns method-not-found, dispose prevents operations, handler exceptions return JSON-RPC errors, different exception types, thread safety for expose.
2. **E2E scenario**: `IE2EGreeter` [JsExport] + `IE2ENotifier` [JsImport] in `FeatureE2EViewModel.RunBridgeAsync` — loads page, exposes greeter, invokes via script, validates response, removes service.
3. **Generator reference**: Integration Tests project references Bridge Generator for Source Generator–generated types in E2E scenario.

## Non-goals

- Automated UI/Browser E2E (current E2E is manual/auto-run within Integration Test App)
- Performance/load testing

## References

- [PROJECT.md](../../PROJECT.md) — G1, G4
- [ROADMAP.md](../../ROADMAP.md) — Deliverables 1.7, 1.1c
