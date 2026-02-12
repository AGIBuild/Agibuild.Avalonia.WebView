# bridge-typescript-generation

**Goal**: G1 (Type-Safe Bidirectional Bridge)
**ROADMAP**: Phase 1, Deliverable 1.3

## Problem

There is no JS-side type safety. Web developers consume the bridge without TypeScript declarations, losing IntelliSense and compile-time validation.

## Proposed Change

The Source Generator emits `TypeScriptEmitter` that produces `.d.ts` content as C# string constants. Per-service interfaces plus a combined `All` field with `Window` augmentation are generated. An MSBuild target writes `bridge.d.ts` after build (when the consumer assembly contains `BridgeTypeScriptDeclarations`).

## Non-goals

- npm package `@agibuild/bridge` (Phase 2)
- Runtime TypeScript validation

## References

- [PROJECT.md](../../PROJECT.md) — G1
- [ROADMAP.md](../../ROADMAP.md) — Deliverable 1.3
