## Context

The repository already has foundational OpenSpec specs focusing on contract surfaces (`webview-core-contracts`) and a basic testing harness (`webview-testing-harness`). However, the specs largely stop at type signatures and do not define the behavioral semantics that determine cross-platform consistency: threading rules, lifecycle/disposal behavior, navigation ordering, cancel/supersede semantics, and how Tasks map to navigation completion or failures.

In parallel, recent work produced draft documents for a compatibility matrix and v1 contract semantics (threading/lifecycle/navigation/webmessage/auth) intended to make the requirements falsifiable and to enable stable contract tests.

This change formalizes those drafts into OpenSpec requirements and aligns existing specs to include the missing data contracts (enums/fields/exceptions) required by the semantics.

Constraints:
- Specs must remain platform-agnostic; platform-specific behavior is handled via Integration Tests (IT) and explicitly documented as Extended/differences.
- Public API additions/changes must be treated as potentially breaking for consumers relying on reflection or event args shapes.
- Contract tests must be deterministic: no time-based sleeps, and event threading guarantees must be testable.

## Goals / Non-Goals

**Goals:**
- Define v1 Baseline behavioral semantics as spec requirements (threading, lifecycle/disposal, navigation ordering and Latest-wins, cancel semantics, Task completion/failure mapping).
- Define a versioned compatibility matrix requirement so “supported” means “measurable and testable”.
- Define WebMessage bridge Baseline security semantics (explicit opt-in, origin allowlist, protocol/version checks, channel isolation) plus a testable drop-diagnostics mechanism.
- Define Auth broker Baseline semantics (strict callback matching, default ephemeral session, distinct result statuses).
- Update existing specs (`webview-core-contracts`, `webview-testing-harness`) to incorporate the minimum data contract (NavigationId/status enums/diagnostics types/exceptions) and CT coverage expectations required by the semantics.

**Non-Goals:**
- Implement platform adapters or rendering behavior (belongs to platform projects and IT).
- Guarantee full parity with any specific native WebView beyond what is stated in the compatibility matrix.
- Define detailed performance SLOs in this change (can be added later once IT data exists).

## Decisions

1) Define Baseline semantics as a dedicated capability spec (`webview-contract-semantics-v1`)
- **Why**: Keeps behavioral requirements centralized, versioned, and reusable across Core + Tests. Avoids scattering “how it behaves” across multiple surface specs.
- **Alternatives**:
  - Put semantics into `webview-core-contracts`: rejected because it would mix surface/API inventory with behavioral invariants and become harder to version.

2) Introduce a compatibility matrix as a first-class capability (`webview-compatibility-matrix`)
- **Why**: “Cross-platform consistency” is only meaningful if explicitly bounded per platform/mode with Baseline vs Extended. This also gives a stable acceptance target for IT.
- **Alternatives**:
  - Keep the matrix as informal docs: rejected because it cannot gate changes or drive acceptance.

3) Require NavigationId + completion status enums in core event args (spec-level)
- **Why**: Exactly-once completion, superseded semantics, and deterministic tests require correlating events and tasks. Without an identifier, ordering invariants are not testable.
- **Alternatives**:
  - Infer correlation by URI and timing: rejected (ambiguous with redirects, repeated URIs, concurrent navigations).

4) Threading rules: Async APIs marshal to UI-thread; Sync APIs require UI-thread
- **Why**: Provides usability for async methods while keeping synchronous methods predictable and avoiding hidden cross-thread blocking.
- **Alternatives**:
  - Require UI-thread for all APIs: rejected (poor ergonomics and makes CT harder for off-thread callers).
  - Allow sync APIs off-thread with implicit marshaling: rejected (return value semantics become timing-dependent).

5) Latest-wins navigation concurrency with explicit Superseded completion
- **Why**: Provides a deterministic and testable rule for concurrent navigations and matches common browser control expectations.
- **Alternatives**:
  - Queue navigations: rejected for Baseline because it introduces hidden buffering and makes cancellation/Stop semantics less predictable.

6) WebMessage “drop observability” must be testable
- **Why**: Security rules are meaningless if failures are silent and cannot be asserted in CT. Logging-only is not testable in a stable way.
- **Alternatives**:
  - Only log: rejected.
  - Expose counters/diagnostics sink: accepted; exact API surface to be defined in Core contracts spec updates.

## Risks / Trade-offs

- **[Risk]** Public contract changes could break consumers that reflect on event args shapes  
  → **Mitigation**: Mark breaking in specs; minimize surface area; version semantics; keep old fields where possible.

- **[Risk]** Semantics may constrain platform adapters too tightly (especially WebMessage/Auth differences)  
  → **Mitigation**: Keep Baseline minimal; allow Extended items to be explicitly marked as platform-different in the compatibility matrix.

- **[Risk]** Deterministic CT requirements increase harness complexity (dispatcher, diagnostics)  
  → **Mitigation**: Encode harness requirements in `webview-testing-harness` spec; ensure they remain platform-agnostic.

