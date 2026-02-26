## 1. Contract and package identity migration

- [x] 1.1 Update core and abstraction project identities to canonical `Agibuild.Fulora.*` names (Deliverable: `webview-core-contracts`, `webview-adapter-abstraction`; AC: build outputs expose `Agibuild.Fulora.Core` and `Agibuild.Fulora.Adapters.Abstractions` package/assembly identities).
- [x] 1.2 Remove legacy package/namespace identifiers from governed release-facing surfaces (Deliverable: `agibuild-fulora-brand-identity-cutover`; AC: no alias/forwarder package or compatibility type remains).
- [x] 1.3 Align public namespace declarations and API surface references with canonical `Agibuild.Fulora` naming (Deliverable: `webview-core-contracts`; AC: public API docs/metadata contain only canonical `Agibuild.Fulora` identity).

## 2. Template, release, and docs alignment

- [x] 2.1 Update template metadata identity and short name to `Agibuild.Fulora` canonical values (Deliverable: `project-template`; AC: `template.json` identity/short-name assertions pass with `Agibuild.Fulora` values).
- [x] 2.2 Update release validation rules for canonical `Agibuild.Fulora` package family naming (Deliverable: `release-versioning-strategy`; AC: release gate validates canonical `Agibuild.Fulora.` package prefix for primary stable outputs).
- [x] 2.3 Update docfx/site metadata and docs index identity to `Agibuild.Fulora` canonical naming (Deliverable: `api-docs`; AC: generated docs site top-level title/navigation shows `Agibuild.Fulora`).

## 3. Governance and verification

- [x] 3.1 Extend governance/unit tests to enforce naming invariants and hard-fail on any legacy canonical token in governed scope (Deliverable: `agibuild-fulora-brand-identity-cutover`; AC: regression test fails deterministically on first legacy token hit).
- [x] 3.2 Run targeted validation suites (`AutomationLaneGovernanceTests`, template/governance checks) and fix failures (Deliverable: all; AC: targeted suites pass deterministically).
- [x] 3.3 Run `openspec validate --all --strict` and ensure change artifacts are fully valid (Deliverable: all; AC: strict validation passes with zero failures).
