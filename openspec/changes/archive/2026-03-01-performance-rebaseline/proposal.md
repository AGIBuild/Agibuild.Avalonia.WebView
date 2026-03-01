## Why

Phase 8 added cancellation token support, IAsyncEnumerable streaming, and method overloads to the Bridge. The existing performance baselines (typed-bridge-call, expose-remove, raw-rpc) were established before these additions. M9.4 requires updated benchmarks covering the new capabilities and refreshed baseline values.

## What Changes

- Add CancellationToken dispatch benchmark to BridgeBenchmarks.cs
- Add IAsyncEnumerable streaming benchmark to BridgeBenchmarks.cs
- Update RuntimeMoniker from Net90 to current runtime
- Run benchmarks and update performance-benchmark-baseline.json with new metrics
- Update ROADMAP M9.4 → Done

## Capabilities

### Modified Capabilities

- `performance-benchmarks`: Add Phase 8 dispatch benchmarks and refresh baselines

## Non-goals

- Real-browser performance testing (mock adapter only)
- CI-enforced benchmark regression gates (governance checks exist but baselines are advisory)

## Impact

- `benchmarks/Agibuild.Fulora.Benchmarks/BridgeBenchmarks.cs` — New benchmark methods
- `tests/performance-benchmark-baseline.json` — Updated metrics
- `openspec/ROADMAP.md` — M9.4 → Done
