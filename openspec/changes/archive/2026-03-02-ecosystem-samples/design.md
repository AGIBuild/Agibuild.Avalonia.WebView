## Context

The project already has React (`samples/avalonia-react/`) and Vue (`samples/avalonia-vue/`) samples. Each follows a four-project structure: Desktop (Avalonia host), Bridge (shared interfaces), Web (frontend), Tests (xUnit). The bridge uses `[JsExport]`/`[JsImport]` with typed client generation. SPA hosting supports Vite dev server and embedded resources.

**Gap**: No Svelte or Angular samples exist. Developers must extrapolate from React/Vue patterns.

## Goals / Non-Goals

**Goals:**
- Add Svelte and Angular samples with structural parity to React/Vue
- Each sample: Desktop host + Bridge + Web frontend + Tests
- Typed bridge usage, dynamic page registry, SPA hosting (dev/prod), unit tests

**Non-Goals:**
- Modifying React/Vue samples
- Adding more frameworks (Solid, Lit, etc.)
- Framework-specific optimizations beyond parity

## Decisions

### D1: Sample structure alignment

**Choice**: Mirror React sample structure: `AvaloniSvelte.Desktop`, `AvaloniSvelte.Bridge`, `AvaloniSvelte.Web`, `AvaloniSvelte.Tests` (and analogous for Angular).

**Alternatives considered**:
- Shared Bridge project across samples: Increases coupling, complicates per-sample evolution
- Monorepo with shared packages: Overkill for samples

**Rationale**: Per-sample isolation keeps each sample self-contained and easy to copy.

### D2: Build tooling for Svelte and Angular

**Choice**: SvelteKit or Vite+Svelte for Svelte; Angular CLI for Angular. Align with each framework's standard tooling.

**Rationale**: Developers expect familiar tooling. Vite is already used for React/Vue; Svelte has first-class Vite support. Angular CLI is the canonical choice for Angular.

### D3: Bridge service parity

**Choice**: Reuse the same bridge service contracts (IAppShellService, ISystemInfoService, IChatService, IFileService, ISettingsService) as React/Vue samples.

**Rationale**: Proves bridge framework-agnosticism. Shared contracts simplify maintenance.

## Risks / Trade-offs

- **[Risk] Angular build complexity** → Angular CLI has different output structure; SPA hosting may need path adjustments. Mitigate with explicit asset path configuration.
- **[Trade-off] Duplicate Bridge code** → Accepted: each sample is self-contained for copy-paste adoption.
