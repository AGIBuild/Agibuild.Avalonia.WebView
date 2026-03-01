## Context

MinVer derives package versions from Git tags. Current tag is `v0.1.21-preview` → `0.1.21-preview`. To produce `1.0.0`, we need a `v1.0.0` tag (no `-preview` suffix).

## Decisions

### D1: Version strategy

**Choice**: Tag `v1.0.0` on the release commit. MinVer produces `1.0.0` for NuGet. npm version set to `1.0.0` manually.

### D2: MinVerMinimumMajorMinor

**Choice**: Update from `0.1` to `1.0` so future commits without tags produce `1.0.x` versions rather than falling back to `0.1.x`.

## Testing Strategy

- `nuke ReleaseOrchestrationGovernance` must pass before tagging
