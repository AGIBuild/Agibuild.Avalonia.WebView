# bridge-npm-package

**Goal**: G1, G2
**ROADMAP**: Phase 2, Deliverable 2.5

## Problem

JS consumers have no typed client library for the bridge. They must manually post messages and parse responses without type safety or ergonomic APIs.

## Proposed Solution

Publish `@agibuild/bridge` npm package with:

- TypeScript types for full type safety
- `invoke` / `handle` / `getService` proxy methods
- `ready()` polling for bridge availability

## References

- [PROJECT.md](../../PROJECT.md) — G1, G2
- [ROADMAP.md](../../ROADMAP.md) — Deliverable 2.5
