## Why

`MSB3277` (`WindowsBase` conflict) is currently handled as governed baseline noise, which keeps CI green but preserves hidden technical debt in the dependency graph. For Phase 3 GA hardening, we should remove this conflict at source so warning governance remains focused on real regressions and aligns with [G4] test and release determinism.

## What Changes

- Normalize WebView2 dependency ingestion so all hosts (Windows/macOS/Linux) can build and pack the same package set without emitting `WindowsBase` conflict warnings.
- Disable `Microsoft.Web.WebView2` `build/buildTransitive` auto-reference injection in affected projects and use explicit `Microsoft.Web.WebView2.Core` compile reference where needed.
- Keep public package dependency metadata to `Microsoft.Web.WebView2` intact for Windows consumers while eliminating build-time WPF/WinForms conflict side effects.
- Replace baseline-only handling with a stricter pipeline invariant: `MSB3277` + `WindowsBase` must be zero in governed build targets.
- Update build governance logic and tests to fail on reintroduction of this conflict pattern.

## Capabilities

### New Capabilities
- (none)

### Modified Capabilities
- `build-pipeline-resilience`: change warning policy from "explicitly governed WindowsBase baseline" to "conflict eliminated; any recurrence is regression".

## Impact

- Affected code: `src/Agibuild.Fulora*.csproj`, `src/Agibuild.Fulora.Adapters.Windows/*.csproj`, `build/Build.cs`, warning-governance tests and baseline files.
- CI/pipeline impact: stricter warning gate; fewer non-actionable warnings on all hosts.
- Dependency impact: WebView2 package consumption model changes from implicit target-injected references to explicit compile reference boundaries.
- Product requirement impact: preserves "build/pack all-platform artifacts from any host" constraint.
- Goal/roadmap alignment: supports [G4] and Phase 3 release-readiness quality ratchet (ROADMAP Phase 3, deliverable 3.8), and improves cross-platform build signal quality for ongoing validation.

## Non-goals

- No new WebView runtime features, API surface additions, or behavior changes in `WebViewCore`.
- No broad "zero all warnings" mandate for unrelated warnings.
- No replacement of existing warning governance reporting artifacts; only policy and classification rules for this specific conflict class are tightened.
