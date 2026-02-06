## Why

The current WebView specs define mostly type surfaces but leave key behavioral semantics undefined, which makes "cross-platform consistency" unfalsifiable and contract tests unstable.
This change establishes testable, versioned contract semantics (threading, lifecycle, navigation/event ordering, error mapping) and a compatibility matrix so requirements are measurable.

## What Changes

- Define baseline contract semantics v1 for threading/marshalling, lifecycle/disposal, navigation state & ordering (including Latest-wins), and Task completion/failure mapping.
- Define baseline WebMessage bridge security semantics (explicit opt-in, origin allowlist, protocol/version checks, channel isolation) including a **testable** drop-diagnostics requirement.
- Define baseline Auth broker semantics (strict callback matching, default ephemeral/isolated session, distinct result statuses).
- Introduce a compatibility matrix artifact as a first-class spec requirement input (Baseline vs Extended, per platform/mode).
- Update contract test harness requirements to cover the baseline semantics (exactly-once completion, cancel behavior, superseded behavior, UI-thread event guarantees).
- Add explicit platform-agnostic public contracts for:
  - deterministic UI-thread dispatching (dispatcher contract)
  - WebMessage policy evaluation and drop diagnostics (policy + diagnostics contracts)
- Clarify Linux support scope across specs: Baseline Linux is Dialog-only; Embedded is not promised.

## Capabilities

### New Capabilities
- `webview-contract-semantics-v1`: Baseline behavioral contract for WebView (threading, lifecycle, navigation/event semantics, error & exception mapping) with testable invariants.
- `webview-compatibility-matrix`: A versioned compatibility matrix defining supported platforms/modes and Baseline vs Extended coverage with acceptance criteria.
- `webview-dispatcher-contracts`: Public dispatcher contract(s) for UI-thread identity and deterministic marshaling used by Core and CT.
- `webmessage-policy-contracts`: Public WebMessage policy and diagnostics contracts enabling testable baseline bridge security semantics.

### Modified Capabilities
- `webview-core-contracts`: Extend from "surface signatures only" to include required event args fields/enums needed by the semantics (e.g., NavigationId, completion status, diagnostics types).
- `webview-testing-harness`: Expand from example tests to a baseline CT suite aligned with the v1 semantics (including deterministic dispatcher and diagnostics assertions).
- `webview-platform-skeletons`: Clarify that Gtk adapter presence does not imply Baseline Linux Embedded support.

## Impact

- Specs:
  - Add new specs under `openspec/specs/` for semantics and compatibility matrix.
  - Update existing specs `webview-core-contracts` and `webview-testing-harness`.
- Code:
  - Public contracts may gain additional fields/enums/exceptions (potentially **BREAKING** for consumers reflecting on event args shapes).
  - Unit test infrastructure will need deterministic dispatcher + mock adapter capabilities to satisfy CT.
- Documentation:
  - Align existing design docs/CT checklists/compatibility drafts with the new spec wording (no implementation details in proposal).

