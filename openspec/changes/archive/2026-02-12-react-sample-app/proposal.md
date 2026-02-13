## Why

ROADMAP Phase 2 deliverable 2.6 requires a **Sample: Avalonia + React** app to demonstrate the full hybrid development workflow — Type-Safe Bridge (G1), SPA Hosting (G2), and developer experience (E1–E3). The existing `minimal-hybrid` sample is vanilla JS only, lacking the modern frontend tooling that most real-world users would adopt. A production-grade React sample is essential for adoption: it proves the framework works end-to-end with a real build pipeline (Vite + TypeScript) and showcases Bridge capabilities that no competing solution offers.

## What Changes

- **New sample application** at `samples/avalonia-react/` — a multi-page hybrid app with Avalonia Desktop host + React (Vite + TypeScript + Tailwind CSS) frontend.
- **Four demo pages** (configurable, dynamically bound via a page registry — not hardcoded):
  - **Dashboard** — C# system info exposed to JS via `[JsExport]`, showcasing native capability access.
  - **Chat** — bidirectional Bridge communication: `[JsExport]` echo/streaming service + `[JsImport]` notification callback from C#.
  - **Files** — native file dialog and directory listing via `[JsExport]`, demonstrating complex type serialization.
  - **Settings** — theme switching + preferences persistence, bidirectional (`[JsExport]` + `[JsImport]`).
- **Bridge service layer** in a shared `AvaloniReact.Bridge` project with typed interfaces, models, and implementations — reusable as a reference architecture.
- **Dev mode**: Vite dev server with HMR via `SpaHostingOptions.DevServerUrl`.
- **Production mode**: Vite build output embedded as resources via `EmbeddedResourcePrefix`.
- **Unit tests** using `MockBridgeService` — validates all C# services without a real WebView.

## Non-goals

- Not a UI component library — the sample uses Tailwind CSS directly, no custom design system.
- Not a mobile sample — Desktop only (mobile sample is a separate deliverable).
- Not an `@agibuild/bridge` npm package change — uses the existing package as-is.

## Capabilities

### New Capabilities

- `react-sample-app`: Production-grade Avalonia + React hybrid sample demonstrating Bridge (JsExport/JsImport), SPA Hosting (app://), Vite HMR dev workflow, and configurable multi-page architecture.

### Modified Capabilities

(none — this change uses existing APIs without requirement changes)

## Impact

- **New files**: ~30-40 files under `samples/avalonia-react/` (C# projects + React app + tests).
- **Dependencies**: React 19, Vite 6, TypeScript 5, Tailwind CSS 4, `@agibuild/bridge` 0.1.0.
- **Build**: No changes to main solution build. Sample has its own `.sln` and `package.json`.
- **CI**: No immediate CI integration (sample is validated manually; CI integration is a follow-up).
- **ROADMAP**: Completes Phase 2 deliverable 2.6; advances toward Phase 2 "Done" status.
- **Goal alignment**: G1 (Bridge demo), G2 (SPA Hosting demo), E1 (developer experience reference).
