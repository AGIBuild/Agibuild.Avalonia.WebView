## Why

Phase 5 is now in closeout mode, but governance tests do not yet enforce the final state as a hard regression gate. We need explicit assertions to keep roadmap status, matrix/manifest IDs, and key regression entry points synchronized.

## What Changes

- Extend governance tests to assert roadmap closeout state and evidence mapping markers.
- Add deterministic consistency checks between runtime critical-path scenario IDs and shell production capability IDs.
- Assert that diagnostic export and template regression entry points remain discoverable.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `runtime-automation-validation`: add cross-artifact consistency governance for critical IDs.
- `shell-production-validation`: enforce matrix/manifest consistency and closeout marker checks.
- `openspec-strict-validation-governance`: include roadmap closeout status governance assertions.

## Impact

- Affected governance unit tests only.
- No runtime behavior changes.
- Improves anti-regression confidence for Phase 5 completion baseline.

## Non-goals

- No new runtime feature.
- No test lane expansion beyond governance checks.
- No roadmap auto-generation logic in this change.
