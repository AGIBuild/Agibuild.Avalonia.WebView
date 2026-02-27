## Why

The current public/runtime contracts in `Agibuild.Fulora.Core` and `Agibuild.Fulora.Runtime` still expose Avalonia types (`IPlatformHandle`, `Dispatcher`-bound implementation), which prevents true host-agnostic adoption and weakens the "one runtime core, two usage paths" promise in `PROJECT.md`.  
Now is the right time to fix this because the project has completed Phase 5 foundation outcomes and is in Phase 6 governance hardening, where stable semantic contracts should no longer be tied to a specific UI framework.

## What Changes

- Introduce framework-neutral native handle contracts in core/runtime layers and remove Avalonia type exposure from those layers (**BREAKING**).
- Split Avalonia-specific implementations (control, dialog host, dispatcher bridge, AppBuilder extensions) into an Avalonia host boundary/package.
- Update platform adapter abstractions to consume framework-neutral parent/native handles instead of Avalonia handle interfaces (**BREAKING**).
- Keep the user-facing Avalonia integration path explicit via dedicated host package/namespace, not through core/runtime transitive dependency.
- Update template/runtime wiring and test suites to validate the new host-neutral contract boundary.

## Capabilities

### New Capabilities
- `host-neutral-runtime-contracts`: Define and enforce a framework-agnostic runtime boundary for handles and UI-thread dispatch contracts.

### Modified Capabilities
- `webview-core-contracts`: Replace framework-specific public contract types with host-neutral abstractions.
- `webview-adapter-abstraction`: Update adapter attach/handle contracts to remove Avalonia-coupled type dependencies.
- `typed-platform-handles`: Rebase typed handle semantics on host-neutral handle primitives.
- `webview-dispatcher-contracts`: Make dispatcher contract framework-neutral and move concrete Avalonia dispatcher binding to host layer.
- `project-template`: Ensure template dependencies and initialization path remain explicit for Avalonia host integration after decoupling.

## Impact

- Affected code: `src/Agibuild.Fulora.Core`, `src/Agibuild.Fulora.Runtime`, `src/Agibuild.Fulora.Adapters.*`, `src/Agibuild.Fulora`, templates, and related tests.
- API impact: public contract type changes are breaking for consumers directly using Avalonia-typed handles from core interfaces.
- Dependency impact: remove `Avalonia` package dependency from core/runtime/adapter abstraction packages; keep Avalonia dependency in host integration layer only.
- Verification impact: requires contract, integration, automation, packaging, and template tests to re-baseline under the new boundary.
- Goal alignment: advances G4 (contract-driven testability) and reinforces framework positioning from Phase 5 outcomes.
- Roadmap alignment: not a direct M6.1-M6.3 deliverable; treated as strategic debt payoff required to preserve phase-neutral architecture and future host extensibility.

## Non-goals

- Adding a second UI framework integration in this change.
- Preserving old Avalonia-typed contract paths in parallel (no dual-path compatibility layer).
- Expanding platform feature scope (cookies, PDF, permissions, shell capabilities) beyond boundary decoupling.
