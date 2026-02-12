# bridge-mock-and-migration

**Goals**: G4 (Testing), backward compatibility
**ROADMAP**: Phase 1, Deliverables 1.5 + 1.6

## Problem

Consumers cannot unit test ViewModels that use the Bridge without a real WebView. Additionally, projects using raw RPC (F6) need a smooth migration path to typed Bridge.

## Proposed Change

1. **MockBridgeService (Core)**: Implements `IBridgeService` with `ConcurrentDictionary` stores. Provides `SetupProxy<T>`, `WasExposed<T>`, `GetExposedImplementation<T>` helpers for test setup and assertions.
2. **Migration / backward compat**: Raw RPC handlers coexist with typed Bridge; Bridge is opt-in; Remove typed service does not affect raw handlers.

## Non-goals

- Source Generator producing mock types (V1 uses manual MockBridgeService)
- Migration tooling or automatic RPC→Bridge conversion

## References

- [PROJECT.md](../../PROJECT.md) — G4
- [ROADMAP.md](../../ROADMAP.md) — Deliverables 1.5, 1.6
