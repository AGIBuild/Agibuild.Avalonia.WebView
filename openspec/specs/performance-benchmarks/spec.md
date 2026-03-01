## Purpose
Define benchmark governance for bridge and RPC dispatch performance baselines.

## Requirements

### Requirement: Typed bridge call benchmark is maintained
The benchmark suite SHALL include typed bridge-call dispatch measurements through deterministic harnesses such as `MockWebViewAdapter`.

#### Scenario: Typed bridge benchmark can be executed
- **WHEN** benchmark automation runs typed call benchmarks
- **THEN** typed dispatch metrics are produced deterministically

### Requirement: Bridge lifecycle benchmark is maintained
The benchmark suite SHALL measure lifecycle overhead for expose/remove operations.

#### Scenario: Expose/remove overhead is measurable
- **WHEN** lifecycle benchmark runs
- **THEN** expose/remove cost metrics are captured for regression tracking

### Requirement: Raw RPC baseline benchmark is maintained
The benchmark suite SHALL include raw RPC handler dispatch as a baseline for comparing higher-level bridge abstractions.

#### Scenario: Raw baseline provides comparison anchor
- **WHEN** both raw RPC and typed bridge benchmarks are executed
- **THEN** output includes comparable metrics for baseline and layered dispatch paths

### Requirement: Benchmark baselines SHALL be regression-governed
Benchmark evidence SHALL define baseline values for key metrics and enforce regression tolerance in governance checks.

#### Scenario: Benchmark metric regresses beyond tolerance
- **WHEN** benchmark governance compares current metric with stored baseline
- **THEN** governance fails with metric name, baseline, actual value, and tolerance

#### Scenario: Baseline artifact missing
- **WHEN** benchmark governance runs without required baseline artifact
- **THEN** governance fails fast with actionable missing-artifact diagnostics

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
