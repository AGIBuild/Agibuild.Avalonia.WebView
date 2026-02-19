## 1. Template Metadata & Preset Surface (Deliverable 4.5)

- [x] 1.1 Add `shellPreset` choice symbol (`baseline`/`app-shell`) to `agibuild-hybrid` template metadata (Acceptance: `template.json` exposes symbol choices and default).
- [x] 1.2 Define preset descriptions and ensure existing framework parameter remains intact (Acceptance: both framework and shell preset parameters are discoverable and valid together).

## 2. Template Scaffold Wiring (Deliverable 4.5)

- [x] 2.1 Add preset-conditional desktop host wiring for app-shell in template source (Acceptance: generated app-shell output contains shell experience bootstrap and deterministic disposal hook).
- [x] 2.2 Ensure baseline preset output omits app-shell wiring while keeping baseline behavior (Acceptance: baseline output compiles and does not include app-shell initialization markers).

## 3. Governance Test Coverage (E1/G4, Deliverable 4.5)

- [x] 3.1 Add/extend repository unit governance tests for template metadata shell preset symbol (Acceptance: tests fail if shell preset metadata drifts or is removed).
- [x] 3.2 Add/extend repository unit governance tests for preset-specific source markers (Acceptance: tests assert template source carries deterministic preset markers).

## 4. Template E2E Lane Update (E1/G4, Deliverable 4.5)

- [x] 4.1 Update `TemplateE2E` build flow to instantiate template with `--shellPreset app-shell` (Acceptance: E2E lane exercises app-shell generation path).
- [x] 4.2 Validate generated project build/test still succeeds under app-shell preset path (Acceptance: template E2E flow remains green).

## 5. Milestone Exit Checks (M4.5)

- [x] 5.1 Run impacted unit and automation/template verification commands and capture outcomes (Acceptance: all impacted suites pass with reproducible commands).
- [x] 5.2 Produce requirements traceability for `template-shell-presets` and `project-template` deltas (Acceptance: each added/modified scenario maps to test evidence).
