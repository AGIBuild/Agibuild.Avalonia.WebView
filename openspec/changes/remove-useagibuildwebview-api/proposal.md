## Why

`UseAgibuildWebView` is a legacy bootstrap alias that keeps two public startup paths alive for the same behavior (`UseFulora` vs `UseAgibuildWebView`). This naming drift increases onboarding friction, weakens API consistency, and conflicts with the post-1.0 stable-surface direction.

Now is the right time to remove the alias because the codebase and official usage surfaces already use `Fulora` as canonical identity, and we need one deterministic startup API across templates, samples, and tests.

## What Changes

- Remove public `UseAgibuildWebView` extension methods from Avalonia and DI bootstrap extensions.
- Standardize all first-party call sites (templates, samples, integration smoke app, docs/comments) on `UseFulora`.
- Add/adjust tests to verify the canonical bootstrap API path remains functional after alias removal.
- Define explicit migration guidance in spec-level requirements so downstream consumers can update deterministically.

## Capabilities

### New Capabilities

- `fulora-bootstrap-api-canonicalization`: Canonicalize startup initialization API surface to `UseFulora` only, with explicit migration semantics from legacy alias usage.

### Modified Capabilities

- `project-template`: Require generated desktop host startup code to use `UseFulora()` as the canonical bootstrap entrypoint.

## Non-goals

- Changing WebView runtime semantics, bridge protocol behavior, or adapter lifecycle behavior.
- Introducing compatibility shims/fallback wrappers for removed legacy alias names.
- Refactoring unrelated startup architecture beyond API name canonicalization.

## Impact

- **Public API**: Alias removal is a breaking API cleanup for legacy callers of `UseAgibuildWebView`.
- **Codebase surfaces**: `Agibuild.Fulora.Avalonia`, `Agibuild.Fulora.DependencyInjection`, templates, samples, integration smoke tests, and related comments/docs.
- **Quality**: Regression coverage updates ensure canonical bootstrap still initializes `WebViewEnvironment` correctly.

Alignment:
- Goals: **E1** (template consistency), **E2** (developer experience consistency), supports **G4** by keeping startup behavior testable and deterministic.
- Roadmap: aligns with Phase 9 stable API governance direction (`M9.2 API Surface Freeze`) and post-Phase 12 maintenance hardening to prevent legacy naming regression.
