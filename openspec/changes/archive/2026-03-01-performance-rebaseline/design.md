## Context

BenchmarkDotNet project exists at `benchmarks/Agibuild.Fulora.Benchmarks/` with 3 existing benchmarks. Baseline JSON at `tests/performance-benchmark-baseline.json` defines tolerance-governed metrics. Phase 8 added CancellationToken and IAsyncEnumerable to the bridge dispatch path.

## Decisions

### D1: New benchmark scenarios

**Choice**: Add two new benchmarks:
1. **Cancellation token dispatch** — measures overhead of cancellable RPC vs. non-cancellable
2. **Streaming dispatch** — measures IAsyncEnumerable bridging overhead

**Rationale**: These are the two major Phase 8 dispatch path additions.

### D2: Baseline values

**Choice**: Run benchmarks on current machine and record median values. Baselines are advisory (10% regression tolerance).

### D3: RuntimeMoniker

**Choice**: Keep `Net90` moniker — BenchmarkDotNet may not yet have `Net100`. The actual runtime is determined by the project TFM (`net10.0`).

## Testing Strategy

- Build benchmarks project to verify compilation
- Run `nuke Test` to verify governance checks pass with updated baseline
