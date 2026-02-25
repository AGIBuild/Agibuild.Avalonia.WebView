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
