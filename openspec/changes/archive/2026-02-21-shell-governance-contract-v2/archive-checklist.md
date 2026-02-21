## Archive-Ready Checklist

- [x] Bridge supports bounded host-configurable aggregate metadata budget with deterministic configuration validation.
- [x] Over-budget inbound metadata is denied before policy/dispatch using stable deny reason.
- [x] Profile revision diagnostics normalize `profileVersion` and canonicalize `profileHash` (`sha256:<64-lower-hex>` or null).
- [x] Invalid profile hash does not affect policy outcome and remains observable as deterministic null.
- [x] Template app-shell markers reflect contract v2 guidance while preserving ShowAbout default deny.
- [x] Governance tests and CT matrix include new budget-bound and normalization branches.
- [x] Focused unit/integration tests passed and outcomes recorded in `verification-evidence.md`.
- [x] `openspec validate shell-governance-contract-v2 --strict` passed.

## Open Questions

1. Should template/project scaffolding expose metadata budget as user-facing configuration in next phase?
2. Should future contract version allow additional hash algorithms beyond `sha256` with explicit versioned schema?
