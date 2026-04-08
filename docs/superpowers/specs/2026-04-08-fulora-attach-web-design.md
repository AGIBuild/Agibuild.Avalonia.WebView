# Fulora Attach Web Design

## Summary

This document defines a first-class Fulora workflow for adopting an existing web application into a Fulora-powered desktop host without rewriting the frontend.

The new workflow centers on a proposed command surface:

```bash
fulora attach web
```

Its purpose is to convert the current manual, expert-only adoption path into a guided, productized path for teams that already have a React, Vue, Next.js, Angular, or similar web application.

The intended user outcome is:

- keep the existing web project
- generate or wire a Fulora desktop host and bridge project around it
- preserve the existing dev-server workflow in development
- preserve a clean packaged/static hosting path in production
- expose native capabilities gradually through typed app services

This is the brownfield counterpart to Fulora’s greenfield default path:

```bash
fulora new -> fulora dev -> fulora package
```

For existing products, Fulora should offer an equally clear path:

```bash
fulora attach web -> fulora dev -> fulora package
```

## Goals

- Make “bring your own web app” a first-class adoption path, not just a manual workaround.
- Reduce the amount of host/web wiring users must discover from scattered docs and examples.
- Standardize project structure, bridge artifact location, and CLI configuration for existing-app adoption.
- Let users get to a working desktop shell quickly while preserving their current web development workflow.
- Create a clear contract for future CLI automation (`attach web`, shared config, preflight integration).
- Keep the brownfield story aligned with Fulora’s newer `services.*`-first bridge ergonomics.

## Non-Goals

- This document does not redesign Fulora’s transport/runtime substrate.
- This document does not replace the existing `fulora new` greenfield path.
- This document does not attempt to make every frontend framework fully zero-config on day one.
- This document does not redesign plugin authoring.
- This document does not define the entire implementation plan in task-level detail.

## Problem Statement

Fulora’s DX has improved significantly for the template-based path (`new -> dev -> package`), but adoption for existing web applications still has several DX gaps:

- users must manually assemble desktop host, bridge project, and web/bridge directory conventions
- users must understand `DevServerUrl`, embedded asset hosting, and bridge artifact output layout too early
- CLI support is still more optimized for Fulora-shaped projects than for externally shaped web workspaces
- documentation for existing web app adoption is not yet a single canonical path
- bridge/generated artifact handling still feels like platform knowledge rather than an implementation detail

As a result, Fulora is easier to adopt for greenfield projects than for teams who already have production web apps — even though those teams are a major adoption opportunity.

## Primary User

A team that already has a working web application and wants to:

- run it inside a native desktop host
- preserve the existing frontend workflow
- avoid a rewrite
- add native capabilities only where needed

Typical examples:

- React + Vite
- Vue + Vite
- Angular
- Next.js
- internal web platform/app-shell setups

## Design Principles

1. **Preserve the existing frontend workflow.** Adoption should not require a frontend rewrite.
2. **Productize the adoption path.** Existing-app onboarding should feel as guided as `fulora new`.
3. **Prefer convention over explanation.** Generate directories, config, and defaults instead of forcing users to discover them manually.
4. **Hide bridge mechanics behind app-service language.** The adoption story should emphasize services and hosting, not RPC plumbing.
5. **Make the path incremental.** Users should be able to stop at “host existing app” before adding native services.
6. **Be explicit rather than magical.** The command should wire predictable structure and config, not silently infer unstable behavior.
7. **Prefer consistency checks over auto-fix.** Fulora should detect bridge/manifest drift clearly instead of mutating generated state behind the user’s back.

## Candidate Approaches

### Option A — New guided command surface (**Recommended**)

Add a dedicated CLI workflow:

```bash
fulora attach web --web ./app/web --desktop ./app/desktop --framework react
```

The command generates and/or wires the Fulora-specific pieces around an existing web app:

- desktop host
- bridge project
- generated bridge directory convention
- shared config
- dev/package guidance

**Pros**
- best product fit
- easiest to document and teach
- minimizes manual host/wiring work
- creates a clear future automation surface

**Cons**
- requires new CLI behavior and careful heuristics
- needs config/schema decisions

### Option B — Extend existing commands only

Keep the workflow distributed across:

- `fulora dev --web ... --desktop ...`
- `fulora generate types --project ... --output ...`
- docs only

**Pros**
- lower immediate implementation cost
- minimal new command surface

**Cons**
- still feels manual
- existing-app adoption remains fragmented
- users still have to compose the path themselves

### Option C — Template export / migration wizard

Create a “generate wrapper project around existing app” tool that primarily emits files from templates without becoming a stable CLI surface.

**Pros**
- fast to prototype
- simpler than full attach semantics

**Cons**
- weaker long-term UX
- likely to drift into a half-supported path
- less discoverable than an explicit adoption command

## Recommendation

Choose **Option A**.

A dedicated `fulora attach web` path is the best match for the problem. Fulora already has a strong greenfield path; the missing piece is a similarly productized brownfield adoption path.

## Proposed UX

### Basic form

```bash
fulora attach web \
  --web ./app/web \
  --desktop ./app/desktop \
  --framework react
```

### Advanced form

```bash
fulora attach web \
  --web ./apps/product-web \
  --desktop ./apps/product-desktop \
  --bridge ./apps/product-bridge \
  --framework react \
  --web-command "pnpm dev" \
  --dev-server-url http://localhost:5173
```

### Minimum expected output

After a successful run, the user should have:

- a Fulora-enabled desktop host
- a bridge project with contract placeholders and generator reference
- a `src/bridge/client.ts` app-facing entrypoint in the web app
- a `src/bridge/services.ts` app-service façade in the web app
- a `src/bridge/generated/` directory reserved for generated artifacts
- a shared config file so future commands can auto-detect the same structure
- a clear “next steps” message:
  - run frontend dev server
  - run `fulora dev`
  - optionally run `fulora generate types`

## Workflow Shape

The command should behave like a guided wiring pass, not a one-shot black box.

### Phase 1 — Inspect

Fulora inspects the supplied web root and determines:

- whether a package manager project exists
- whether the repo already contains a desktop host and/or bridge project
- whether the selected framework maps to a known dev/build convention
- whether a compatible generated artifact directory already exists

### Phase 2 — Scaffold or wire

Fulora then creates or updates only the Fulora-owned pieces:

- desktop project (if missing)
- bridge project (if missing)
- bridge generator reference
- frontend bridge entrypoints (`client.ts`, `services.ts`)
- config file (`fulora.json`)

The user’s existing application routes, components, state, and build scripts remain untouched unless the user explicitly opts into framework-specific helper edits later.

### Phase 3 — Explain next steps

The command finishes with an adoption-oriented handoff:

- how to run the existing web dev server
- how to start Fulora dev mode
- where generated artifacts will appear
- how to add the first host capability
- how bridge consistency is verified in later commands

## Proposed File/Config Model

### Shared config file

Introduce a root-level config file such as `fulora.json`:

```json
{
  "web": {
    "root": "./app/web",
    "command": "pnpm dev",
    "devServerUrl": "http://localhost:5173",
    "generatedDir": "./app/web/src/bridge/generated"
  },
  "bridge": {
    "project": "./app/bridge/MyProduct.Bridge.csproj"
  },
  "desktop": {
    "project": "./app/desktop/MyProduct.Desktop.csproj"
  }
}
```

### Why this matters

This lets the following commands share one configuration source:

- `fulora attach web`
- `fulora dev`
- `fulora generate types`
- `fulora package`
- `fulora doctor`

It also reduces repeated path arguments and fragile directory heuristics.

### Config design rules

- Paths should be stored relative to repo root when possible.
- The file should be safe to commit and easy to review.
- Later commands must consume the same config instead of rebuilding their own heuristics.
- v1 should keep the schema intentionally small.

## Generated/Wired Artifacts

The attach workflow should create or verify:

- desktop project exists and is Fulora-enabled
- bridge project exists and references:
  - `Agibuild.Fulora.Core`
  - `Agibuild.Fulora.Bridge.Generator`
- web project contains:
  - `src/bridge/client.ts`
  - `src/bridge/services.ts`
  - `src/bridge/generated/`
- CLI config points to the above paths

It should not attempt to invent a complex host-service surface automatically. The first version should create the wiring, not a giant service catalog.

### Default frontend shape

The generated frontend bridge entrypoint should reinforce the app-service mental model:

- `client.ts` owns bridge client creation and middleware setup
- `services.ts` re-exports a stable `services.*` API for application code
- `generated/` stays generated-only and should not contain hand-authored business logic

## Framework Scope for v1

### Officially supported in v1

- React + Vite
- Vue + Vite
- generic custom dev server + static build path

### Explicitly tolerated but lighter-touch

- Next.js
- Angular
- custom monorepo frontend packages

For those, Fulora should still support manual `--web-command` / `--dev-server-url`, but the generated guidance may be less opinionated.

## Best-Practice Defaults

The attach workflow should encode these defaults:

- frontend business code uses `services.*`
- generated bridge artifacts live under `src/bridge/generated/`
- development uses `DevServerUrl`
- production uses embedded or packaged local assets
- host navigation uses `app://localhost/index.html`
- bridge is used only for host capabilities, not for all application logic

## Consistency and Preflight Model

The attach workflow should integrate with Fulora’s newer manifest/hash consistency checks.

### Principle

Fulora should treat generated bridge artifacts as a verifiable build product, not as state it silently auto-repairs.

### Expected behavior

- `attach web` establishes the generated artifact directory and config once.
- `generate types` remains the explicit way to regenerate TypeScript bridge artifacts.
- `dev --preflight-only` and `package --preflight-only` verify manifest/hash consistency using the recorded bridge manifest.
- When generated artifacts are missing or stale, Fulora should surface actionable guidance instead of performing hidden auto-fix work.

### Why this matters

This keeps the workflow understandable:

- users know when generation happens
- drift is visible and reviewable
- CI can fail deterministically on inconsistent state
- attach-generated structure stays stable across local and CI environments

## Error Handling / Diagnostics

The command should fail clearly when:

- web directory does not exist
- package.json cannot be found
- desktop directory is ambiguous or invalid
- bridge project cannot be created or referenced
- configured dev server URL or web command is inconsistent with the selected framework mode
- the requested generated directory collides with an incompatible existing layout

Diagnostics should be phrased in adoption language, for example:

- “Fulora could not find your web project root.”
- “The bridge project was created, but generated artifact output could not be connected to your web app.”
- “Your configuration points to a generated directory that does not exist yet.”
- “Bridge artifacts are missing or stale. Run `fulora generate types` to refresh them.”

## Documentation Plan

The command must be introduced together with:

- README “Already have a web app?” section update
- CLI reference update
- full BYO web app guide
- quickstart guide
- best-practices guide update or cross-link

The docs should present `attach web` as the canonical brownfield path, the same way `new` is the canonical greenfield path.

### Recommended docs narrative

1. **Start with the user they already are.** “You already have a web app.”
2. **Show the brownfield happy path.** `attach web -> dev -> package`.
3. **Explain the runtime model simply.** dev server in development, packaged assets in production.
4. **Introduce typed services only after hosting is working.**
5. **Keep raw RPC and bridge internals out of the main path.**

## Acceptance Criteria

A successful v1 should allow a user to:

1. Point Fulora at an existing web app directory
2. Get a wired desktop host + bridge project + generated directory convention
3. Start development with the same frontend dev server they already use
4. Add a first typed host capability without reading deep runtime docs
5. Package the app using the normal Fulora packaging path
6. Detect bridge artifact drift through explicit preflight checks rather than hidden repair behavior

## Risks

1. **Over-automation risk** — trying to infer too much about every web framework may produce fragile behavior.
2. **Config drift risk** — if attach-generated config is not reused by later commands, the workflow fragments again.
3. **Bridge overreach risk** — users may interpret attach as a signal to move all app logic into bridge services.
4. **Monorepo ambiguity** — large repos may require explicit path flags rather than heuristics.
5. **Magic-repair risk** — if later commands silently rewrite generated outputs, attach becomes harder to reason about.

## Mitigations

- keep v1 narrow and convention-first
- introduce `fulora.json` as shared state early
- document supported and tolerated frameworks separately
- keep app-service examples focused on real host capabilities
- enforce manifest/hash consistency as a transparent verification model instead of auto-fix behavior

## Recommended next step

Turn this design into an implementation plan covering:

1. `fulora.json` schema
2. `fulora attach web` CLI contract and validation flow
3. generated file/template set
4. integration with `dev`, `generate types`, `package`, and `doctor`
5. doc/README/CLI rollout for the brownfield primary path
