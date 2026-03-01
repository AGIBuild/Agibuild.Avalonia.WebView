## 1. Host package identity hard cut (Deliverable: D1 package identity)

- [ ] 1.1 Rename primary Avalonia host package identity to `Agibuild.Fulora.Avalonia` in host project/package metadata (AC: `dotnet pack` output contains `Agibuild.Fulora.Avalonia.*.nupkg` and no primary `Agibuild.Fulora.*.nupkg` host artifact).
- [ ] 1.2 Update host packaging targets/assets paths to new identity tokens (AC: packaging target validation passes and build-transitive asset paths resolve deterministically).

## 2. Release governance and distribution determinism (Deliverable: D2 release policy)

- [ ] 2.1 Update release-versioning and distribution-readiness governance assertions for new primary host package ID (AC: governance checks assert `Agibuild.Fulora.Avalonia` as required primary host package).
- [ ] 2.2 Update machine-readable release/distribution evidence mapping to emit deterministic diagnostics for missing/legacy host package identity (AC: failure output includes explicit expected-vs-actual package ID diagnostics).

## 3. Template, sample, and consumer wiring (Deliverable: D3 adoption path consistency)

- [ ] 3.1 Update hybrid template desktop dependency wiring to `Agibuild.Fulora.Avalonia` (AC: scaffolded Desktop project references new package identity and omits legacy identity).
- [ ] 3.2 Update sample apps and NuGet integration test consumer project references to new package identity (AC: sample/consumer restore succeeds against new package name with no legacy package reference).

## 4. Validation and regression safety net (Deliverable: D4 quality gate)

- [ ] 4.1 Add or update unit/governance/template E2E assertions for hard-cut identity invariants (AC: tests fail deterministically when `Agibuild.Fulora` is reintroduced as primary host package).
- [ ] 4.2 Run verification suite (`dotnet test` targeted lanes + `openspec validate --all --strict`) and capture pass evidence (AC: all targeted tests and strict OpenSpec validation pass).
