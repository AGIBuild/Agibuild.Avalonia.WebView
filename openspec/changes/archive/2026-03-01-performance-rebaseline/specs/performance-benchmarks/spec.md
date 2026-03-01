## MODIFIED Requirements

### Requirement: Phase 8 dispatch paths have benchmark coverage
The benchmark suite SHALL include measurements for CancellationToken-enabled dispatch and IAsyncEnumerable streaming bridging overhead added in Phase 8.

#### Scenario: Cancellation token dispatch is benchmarked
- **WHEN** benchmark automation runs
- **THEN** a cancellable RPC dispatch benchmark is executed and metrics are produced

#### Scenario: Streaming dispatch is benchmarked
- **WHEN** benchmark automation runs
- **THEN** an IAsyncEnumerable streaming benchmark is executed and metrics are produced

### Requirement: Benchmark baselines include Phase 8 metrics
The baseline artifact SHALL include metrics for Phase 8 dispatch scenarios alongside existing metrics.

#### Scenario: Baseline artifact covers all dispatch paths
- **WHEN** benchmark governance checks baselines
- **THEN** metrics for cancellation-dispatch and streaming-dispatch are present with `baselineMs > 0`
