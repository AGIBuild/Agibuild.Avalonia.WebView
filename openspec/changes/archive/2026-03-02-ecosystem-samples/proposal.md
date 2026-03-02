## Why

Agibuild.Fulora currently demonstrates framework versatility only through React and Vue samples. Developers using Svelte or Angular lack reference implementations, reducing adoption confidence and requiring them to infer integration patterns from dissimilar ecosystems.

**Goal alignment**: Broaden framework appeal, prove bridge and hosting work across major frontend frameworks, and provide copy-paste starting points for Svelte/Angular adopters.

## What Changes

- Add Svelte sample application at `samples/avalonia-svelte/` with Desktop host, Bridge, Web frontend, and Tests
- Add Angular sample application at `samples/avalonia-angular/` with same structure
- Each sample mirrors React/Vue: typed bridge client, SPA hosting (dev + prod), dynamic page registry, and unit tests

## Non-goals

- Replacing or deprecating React/Vue samples
- Supporting additional frameworks (e.g., Solid, Lit) in this change
- Changing core bridge or hosting APIs

## Capabilities

### New Capabilities
- `svelte-angular-samples`: Svelte and Angular sample applications demonstrating Fulora integration parity with React/Vue

## Impact

- New directories: `samples/avalonia-svelte/`, `samples/avalonia-angular/`
- CI: Add build and test jobs for both samples
- Documentation: Update samples overview to include Svelte and Angular
