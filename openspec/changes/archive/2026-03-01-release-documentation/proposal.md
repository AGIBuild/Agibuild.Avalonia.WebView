## Why

Phase 9 exit criteria require a structured changelog covering all phases (M9.5) and a migration guide covering at least one alternative framework (M9.6). These are the last documentation gates before the 1.0 stable release.

## What Changes

- Create `CHANGELOG.md` covering Phases 0–9 with structured release notes
- Create `docs/MIGRATION_GUIDE.md` for Electron → Fulora migration
- Update ROADMAP M9.5 → Done, M9.6 → Done

## Capabilities

### New Capabilities

- `release-changelog`: Structured changelog artifact for auditable release history
- `migration-guide`: Actionable migration path documentation

## Non-goals

- Tauri migration guide (Electron is the primary target; Tauri can be added post-1.0)
- API reference docs generation (separate tooling concern)

## Impact

- `CHANGELOG.md` — New file at repo root
- `docs/MIGRATION_GUIDE.md` — New file
- `openspec/ROADMAP.md` — M9.5, M9.6 → Done
