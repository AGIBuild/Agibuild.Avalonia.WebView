## Roadmap Notes (Phase 5.2 / 5.3 / 5.4 / 5.5)

### In Scope

- System action whitelist v2 hardening around `ShowAbout` with deterministic deny taxonomy.
- Tray payload v2 governance: required core fields + `platform.*` extension namespace + bounded metadata budget.
- Bridge and shell ordering guarantees: schema/whitelist first, then policy, then provider.
- App-shell template canonical demonstration: typed ShowAbout outcome rendering + v2 tray payload consumption markers.
- CT/IT/governance matrix updates for machine-checkable coverage and bypass regression prevention.

### Out of Scope

- Bundled-browser full API parity or one-to-one API surface replacement.
- Multi-host support expansion (WPF/WinForms/MAUI) in this increment.
- Legacy dual-path compatibility or fallback adapters for old tray payload format.

### Boundary Statement

This change is a contract hardening increment for AI-agent-friendly typed system integration, not a broad feature-surface expansion.
