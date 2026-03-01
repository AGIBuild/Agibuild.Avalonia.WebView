## Context

The project has 9 completed phases with 80+ commits. The changelog must be structured per-phase with semver-aligned version markers. The migration guide targets Electron as the most common framework adopters would migrate from.

## Decisions

### D1: Changelog format

**Choice**: Keep Changelog format (keepachangelog.com) with phase-grouped sections. Each phase maps to a version range.

### D2: Migration guide scope

**Choice**: Focus on Electron → Fulora migration covering: architecture mapping, IPC → Bridge mapping, packaging, and a step-by-step checklist.

**Rationale**: Electron has the largest market share among desktop web-hybrid frameworks. One well-done guide is better than two shallow ones.

## Testing Strategy

- `nuke Test` must pass (no code changes, documentation only)
- `openspec validate --all --strict` must pass
