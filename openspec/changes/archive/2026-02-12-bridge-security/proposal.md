# bridge-security

**Goal**: G3 (Bridge Security)
**ROADMAP**: Phase 1, Deliverable 1.4

## Problem

The Bridge has no rate limiting. A compromised or malicious web page could flood the bridge with RPC calls, causing DoS or resource exhaustion on the host application.

## Proposed Change

Add `RateLimit` class to `BridgeOptions` with sliding-window enforcement in `RuntimeBridgeService` via `RateLimitingRpcWrapper`. Throttled calls return JSON-RPC error code `-32029` (rate limit exceeded).

## Non-goals

- Per-method rate limits
- Distributed rate limiting

## References

- [PROJECT.md](../../PROJECT.md) — G3
- [ROADMAP.md](../../ROADMAP.md) — Deliverable 1.4
