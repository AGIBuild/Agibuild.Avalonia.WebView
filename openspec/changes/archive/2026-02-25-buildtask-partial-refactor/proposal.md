## Why

`build/Build.cs` has grown too large, which makes ownership boundaries unclear and increases maintenance cost for CI/build changes.
The current entry class name `_Build` also creates naming ambiguity with the `Build` target semantics.

## What Changes

- Rename the Nuke entry class from `_Build` to `BuildTask`.
- Split `build/Build.cs` into focused partial files by responsibility (targets, warning governance, helpers, publishing, react/npm helpers).
- Keep all existing target names and command interfaces unchanged (`nuke Test`, `nuke Coverage`, `nuke Ci`, `nuke CiPublish`).

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `build-pipeline-resilience`: improve build script maintainability with partial class modularization while preserving execution behavior.

## Impact

- Affected files are limited to build orchestration source and related governance tests.
- No runtime/library API behavior changes.
- CI invocation path and target contract remain stable.

## Non-goals

- No change to target names or CLI contract.
- No new build/publish feature.
- No workflow or release policy redesign.
