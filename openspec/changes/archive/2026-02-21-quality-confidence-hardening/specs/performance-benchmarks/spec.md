## ADDED Requirements

### Requirement: Benchmark baselines SHALL be regression-governed
Benchmark evidence SHALL define baseline values for key metrics and enforce regression tolerance in governance checks.

#### Scenario: Benchmark metric regresses beyond tolerance
- **WHEN** benchmark governance compares current metric with stored baseline
- **THEN** governance fails with metric name, baseline, actual value, and tolerance

#### Scenario: Baseline artifact missing
- **WHEN** benchmark governance runs without required baseline artifact
- **THEN** governance fails fast with actionable missing-artifact diagnostics
