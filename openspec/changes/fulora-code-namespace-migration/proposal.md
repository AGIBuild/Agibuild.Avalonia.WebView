## Why

Branding has shifted to **Agibuild.Fulora**, but code-level identity (NuGet IDs, namespaces, template/package naming, and contract specs) is still anchored to legacy names. This mismatch weakens product clarity and onboarding trust. We need a spec-governed hard cutover now to complete Phase 5 framework positioning and keep E1/E2 developer experience coherent.

## What Changes

- Define a repository-wide naming cutover contract from legacy product identifiers to `Agibuild.Fulora.*` across docs, package metadata, namespaces, templates, and automation artifacts.
- **BREAKING**: switch canonical public naming for package and namespace surfaces to `Agibuild.Fulora.*` with no compatibility aliases.
- Remove legacy identifiers from release-facing surfaces and fail governance if they reappear.
- Update CI/governance checks to enforce naming consistency and prevent any reintroduction of legacy brand tokens in governed artifacts.
- Align this change with PROJECT goals **G1/G4** (stable contracts + testability) and **E1/E2** (template/tooling coherence), and with ROADMAP Phase 5 “Framework Positioning Foundation” closeout continuity.

## Non-goals

- No runtime behavior redesign for navigation, policy, shell semantics, or adapter execution flow.
- No platform-support expansion or new feature capability surface.
- No compatibility layer, alias package, forwarding type, or dual-brand maintenance path.

## Capabilities

### New Capabilities
- `agibuild-fulora-brand-identity-cutover`: Defines canonical `Agibuild.Fulora` naming and hard-cutover governance rules across code, packages, templates, and governance outputs.

### Modified Capabilities
- `webview-core-contracts`: Update contract-level assembly/root namespace identity requirements.
- `webview-adapter-abstraction`: Update abstraction assembly naming requirements aligned with `Agibuild.Fulora` identity.
- `project-template`: Update template identity/metadata and output branding constraints for `Agibuild.Fulora`.
- `release-versioning-strategy`: Update release artifact naming/version lineage requirements for renamed `Agibuild.Fulora` package family.
- `api-docs`: Update API documentation identity requirements to `Agibuild.Fulora` naming.

## Impact

- Public API identity surfaces (namespaces, assembly/package names, template identifiers).
- Build/release and governance pipelines (artifact names, checks, reports, strict validations).
- Consumer impact from one-time hard cutover in package/namespace identity.
- Documentation, samples, and template-generated project metadata.
