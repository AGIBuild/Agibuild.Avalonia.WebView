## Archive-Ready Checklist

- [x] Aggregate metadata payload budget is enforced with deterministic deny reason and deny-before-policy semantics.
- [x] Profile diagnostics include optional `ProfileVersion` / `ProfileHash` and preserve deterministic null behavior when omitted.
- [x] Template app-shell source includes explicit ShowAbout opt-in snippet marker and keeps default deny behavior.
- [x] Governance assertions cover new marker and contract branches.
- [x] CT matrix includes metadata budget and profile revision diagnostic evidence branches.
- [x] Focused unit/integration verification commands passed and are recorded in `verification-evidence.md`.
- [x] `openspec validate shell-federated-governance-followup --strict` passed.

## Open Questions

1. Should aggregate metadata budget be externally configurable in future phases, or remain fixed as a contract constant?
2. Should profile hash require algorithm-prefix format (for example `sha256:<hex>`) at contract level?
