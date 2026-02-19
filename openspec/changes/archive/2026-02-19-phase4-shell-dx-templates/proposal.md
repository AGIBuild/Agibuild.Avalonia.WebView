## Why

M4.4 completed shell governance primitives, but new adopters still start from a mostly baseline template and manually wire shell capabilities. M4.5 is needed to deliver shell presets directly in `dotnet new agibuild-hybrid`, reducing setup friction and aligning with ROADMAP Deliverable 4.5 and E1.

## What Changes

- Add template-level shell preset parameter for `agibuild-hybrid` with explicit baseline/app-shell choices.
- Scaffold shell-aware startup wiring in generated Desktop host when app-shell preset is selected.
- Extend template governance tests to validate preset metadata and generated shell wiring markers.
- Extend template E2E flow in build automation to exercise shell preset generation path.

## Non-goals

- Full Electron API parity from template defaults.
- Dynamic runtime preset switching.
- Introducing new runtime shell contracts (M4.5 consumes existing M4.1–M4.4 capabilities).

## Capabilities

### New Capabilities
- `template-shell-presets`: Defines shell preset options and expected generated host wiring behavior in project templates.

### Modified Capabilities
- `project-template`: Extend requirements to include shell preset parameter and preset-driven generated output.

## Impact

- **Roadmap alignment**: Implements **Phase 4 M4.5 / Deliverable 4.5** (`Template shell presets + samples`).
- **Goal alignment**: Advances **E1** by providing shell-enabled starter output and **G4** via deterministic preset validation tests.
- **Affected systems**: template metadata and scaffold files, build template-e2e automation target, and repository governance tests for template consistency.
