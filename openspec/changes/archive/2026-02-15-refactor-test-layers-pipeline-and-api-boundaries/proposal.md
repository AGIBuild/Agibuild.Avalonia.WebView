## Why

Phase 3 still has a confidence gap between contract-level test pass rates and real runtime behavior on platform adapters. Recent `NugetPackageTest` failures also showed pipeline instability caused by cache/root mismatches and transient packaging noise, while async-boundary governance remains partially enforced.  
This change advances **G4 (Contract-Driven Testability)** and Phase 3 deliverables **3.5 (GTK/Linux smoke validation)** and **3.8 (API surface audit + breaking-change readiness)** by tightening runtime validation, pipeline determinism, and boundary governance.

## What Changes

- Introduce a layered automation model that clearly separates mock-driven contract automation from runtime-adapter automation, with mandatory critical-path runtime scenarios per platform lane.
- Add pipeline resilience rules for NuGet smoke validation, including deterministic cache-root handling, transient-failure classification, and bounded retry policy.
- Strengthen async-boundary governance by extending blocking-wait rules beyond `src/` to build/pipeline orchestration where applicable.
- Add API-boundary governance requirements for removing remaining implicit/global option coupling and reducing reflection-only lifecycle test hooks.
- Add explicit evidence/reporting requirements so CI can distinguish contract-pass vs runtime-pass confidence.

## Capabilities

### New Capabilities
- `runtime-automation-validation`: Define mandatory runtime-adapter automation coverage and reporting semantics separate from mock-based suites.
- `build-pipeline-resilience`: Define deterministic package smoke pipeline behavior, transient failure handling, and reproducible retry boundaries.

### Modified Capabilities
- `webview-testing-harness`: Add layered test-lane taxonomy and runtime-lane acceptance criteria.
- `blocking-wait-governance`: Extend governance to build/pipeline blocking waits and polling helpers.
- `webview-contract-semantics-v1`: Add explicit async-boundary governance scenarios for lifecycle and environment-option isolation.
- `api-surface-review`: Add boundary-closure checks for remaining sync/global couplings and reflection-only test seams.

## Impact

- Affected tests: `tests/Agibuild.Avalonia.WebView.Integration.Tests.Automation`, `tests/Agibuild.Avalonia.WebView.Integration.NugetPackageTests`, `tests/Agibuild.Avalonia.WebView.Testing`, and selected `UnitTests` governance checks.
- Affected build flow: `build/Build.cs` target behavior for package smoke and retry classification.
- Affected runtime boundaries: option propagation and async/sync boundary audit points in runtime/control layers.
- CI/reporting impact: test result taxonomy and confidence reporting become explicit artifacts.

## Non-goals

- No new platform adapter feature work (WebView API capability expansion is out of scope).
- No migration to a new build orchestrator or CI provider.
- No broad rewrite of existing unit test suites unrelated to async-boundary and runtime-confidence goals.
