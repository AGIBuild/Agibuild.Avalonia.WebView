# PRD — Fulora Developer Experience Refactor

## Source of truth

- Deep-interview spec: `.omx/specs/deep-interview-fulora-developer-experience-refactor.md`
- Interview transcript: `.omx/interviews/fulora-developer-experience-refactor-20260406T081331Z.md`
- Evidence anchors:
  - `README.md`
  - `docs/superpowers/specs/2026-04-04-fulora-dx-decision-document.md`
  - `src/Agibuild.Fulora.Runtime/RuntimeBridgeService.cs`
  - `templates/agibuild-hybrid/HybridApp.Web.Vite.React/src/bridge/generated/bridge.client.ts`
  - `src/Agibuild.Fulora.Cli/Commands/GenerateCommand.cs`

## Problem

Fulora already has broad capability coverage, but the app-builder experience still exposes too much platform machinery too early. The main path is conceptually heavier than it should be, and the bridge/RPC surface still feels like framework plumbing instead of app-service usage.

## Users

Primary user:
- application developers adopting Fulora for desktop apps

Secondary users:
- maintainers improving templates, CLI, generator, and docs in support of the primary user path

Not primary in this plan:
- plugin authors as the lead persona
- runtime transport maintainers seeking a protocol rewrite

## Product goal

Make Fulora feel like an opinionated app framework with a clear happy path (`new -> dev -> package`) and a low-ceremony app-service API, without rewriting the low-level transport/runtime protocol.

## Success outcomes

1. The default app-builder path is singular and easier to discover.
2. Generated bridge clients feel like typed app services rather than stringly RPC wrappers.
3. The dev loop hides generation mechanics by default and improves diagnostics when things go wrong.
4. Packaging/release feels like a productized path rather than a cliff of implicit prerequisites.
5. Docs, templates, generator, and CLI tell the same story.

---

# RALPLAN-DR Summary

## Principles

1. **Optimize the app-builder path first.** Prioritize the first successful app over exposing full platform depth.
2. **Hide ceremony, keep power.** Preserve bridge/runtime capability while reducing required conceptual load.
3. **Prefer additive façades over substrate rewrites.** Improve DX by changing defaults, generated surfaces, and tooling before altering transport internals.
4. **Make the repo tell one story.** README, docs, templates, CLI, and generated outputs should reinforce the same mental model.
5. **Keep escape hatches.** Advanced users may still access raw RPC and lower-level surfaces, but they stop being the default narrative.

## Decision drivers

1. Highest user leverage is in the app-builder day-1/day-2 workflow.
2. Repository evidence suggests capability is already broad; presentation and ergonomics are the larger gap.
3. Transport/runtime rewrites carry disproportionate risk relative to likely near-term DX gain.

## Viable options

### Option A — Productize the primary path + introduce a typed service-client façade (**Recommended**)

Scope:
- tighten `new -> dev -> package`
- redesign generated app-facing client API
- auto-manage generation and drift in dev/build/package workflows
- improve docs/templates/diagnostics around the primary path

Pros:
- high DX leverage without destabilizing core transport
- aligns with existing DX decision doc and README direction
- can ship incrementally across templates, generator, CLI, and docs

Cons:
- requires coordination across several surfaces
- legacy/raw examples may need migration guidance
- may leave deeper architectural cleanup for later phases

### Option B — Deep bridge/runtime protocol redesign first

Scope:
- revisit transport protocol, bridge contract plumbing, and generated semantics from the substrate upward

Pros:
- could enable cleaner long-term abstractions if existing substrate were the bottleneck
- may unify some edge-case semantics more elegantly

Cons:
- explicitly outside clarified scope for this effort
- high migration and compatibility risk
- weak evidence that protocol internals are the dominant app-builder pain today

### Option C — Docs/template narrative cleanup only

Scope:
- improve README, docs IA, examples, and onboarding without generator/client changes

Pros:
- low implementation risk
- fast visible improvement

Cons:
- does not address the live RPC ergonomics problem the user explicitly raised
- preserves too much bridge ceremony in actual code
- likely shifts friction rather than removing it

## Option decision

Choose **Option A**.

Option B is rejected for violating the clarified boundary against low-level transport/runtime rewrite. Option C is insufficient because it improves framing without materially improving the generated app-facing API.

---

# Recommended plan

## Workstream 1 — Primary path tightening (`new -> dev -> package`)

Objective:
- ensure the default Fulora story is visibly singular across README, docs, CLI help, templates, and samples

Deliverables:
- rewrite top-level docs and CLI framing around the default app-builder path
- demote advanced bridge/generator/plugin concepts from first-contact surfaces
- define one canonical starter template/app layout and make examples follow it

Likely touchpoints:
- `README.md`
- getting-started/docs index pages
- template READMEs and starter app content
- `src/Agibuild.Fulora.Cli/Program.cs`

## Workstream 2 — Generated RPC/client ergonomics

Objective:
- make app developers consume generated services instead of composing raw method strings

Deliverables:
- generated stable `client` API that exports named services and/or a client factory
- normalized method naming and input shapes
- better generated comments and example imports
- compatibility story that preserves raw RPC as an escape hatch

Likely touchpoints:
- `src/Agibuild.Fulora.Runtime/RuntimeBridgeService.cs`
- bridge generator output templates
- template/sample `src/bridge/client.ts` and `src/bridge/services.ts`
- bridge docs/examples

## Workstream 3 — Dev-loop automation and diagnostics

Objective:
- make generation and bridge readiness feel automatic; make failures diagnosable

Deliverables:
- auto-generation/watch behavior integrated into `fulora dev`
- drift detection and clearer preflight/build/package messages
- actionable diagnostics for missing/generated/bridge-ready failures
- docs/examples that show app-service usage, not transport plumbing

Likely touchpoints:
- `src/Agibuild.Fulora.Cli/Commands/GenerateCommand.cs`
- `fulora dev` command surface
- template hooks such as `useBridge`
- diagnostics docs and sample error flows

## Workstream 4 — Packaging productization

Objective:
- reduce the release/packaging cliff for app developers

Deliverables:
- clearer package profiles/defaults
- preflight or doctor guidance for common package prerequisites
- docs and examples aligned with the same path from app creation to shipping

Likely touchpoints:
- package/doctor CLI surfaces
- packaging docs/checklists
- templates/sample release docs

---

# RPC/client redesign direction

## Current shape (repository evidence)

Current generated stub style is close to:

```ts
await window.agWebView.rpc.invoke("GreeterService.greet", { name: "World" });
```

Template-generated client code also still surfaces direct `rpc.invoke("Service.method", params)` wrappers.

## Desired design goals

- raw RPC strings disappear from common app code
- app developers call named services, not bridge plumbing
- cancellation, streams, events, and errors follow one obvious surface
- generated files feel stable and discoverable
- advanced users still retain low-level access when needed

## Recommended app-facing API

### Preferred default

```ts
import { services } from "./bridge/client";

const msg = await services.greeter.greet({ name: "World" });
await services.notifications.show({ message: "Saved" });
```

### Alternative factory for testability/DI friendliness

```ts
import { createFuloraClient } from "./bridge/client";

const app = createFuloraClient();
const profile = await app.userProfile.get();
await app.dialogs.openFile();
```

### Event and stream direction

```ts
const app = createFuloraClient();

const sub = app.theme.changed.subscribe((evt) => {
  console.log(evt.mode);
});

for await (const item of app.search.stream({ query: "fulora" })) {
  console.log(item);
}

sub.dispose();
```

### Compatibility/escape hatch

```ts
import { rawRpc } from "./bridge/client";

await rawRpc.invoke("GreeterService.greet", { name: "World" });
```

### Generated client sketch

```ts
export interface GreeterService {
  greet(input: { name: string }): Promise<string>;
}

export function createFuloraClient(rpc = rawRpc) {
  return {
    greeter: {
      greet(input: { name: string }) {
        return rpc.invoke("GreeterService.greet", input) as Promise<string>;
      }
    }
  };
}

export const services = createFuloraClient();
```

## API design constraints

- preserve transport compatibility underneath
- keep generated shape predictable across frameworks/templates
- avoid forcing app authors to understand generator internals
- include migration path from current generated service exports and docs examples

---

# Delivery phases

## Phase 1 — Narrative and defaults alignment

- unify README/docs/template framing around one primary path
- define canonical starter structure and generated file expectations
- update examples to prefer generated `services`/client usage over raw RPC strings

## Phase 2 — RPC façade and generator alignment

- design and implement new generated client surface
- preserve raw RPC escape hatch and backward compatibility strategy
- update samples/templates/docs to the new ergonomic API

## Phase 3 — Dev-loop automation and diagnostics

- hide generation/drift inside `dev`, build, and package flows
- add clearer failure messages and bridge readiness diagnostics
- tighten template hooks and examples around the new defaults

## Phase 4 — Packaging productization

- make shipping paths and preflight expectations more explicit and easier to satisfy
- align docs and CLI around named package profiles and preflight checks

---

# ADR

## Decision

Adopt an app-builder-first DX refactor centered on primary-path productization and a new generated RPC/client façade, while explicitly avoiding a low-level transport/runtime rewrite in this cycle.

## Drivers

- repository evidence shows Fulora already has broad capability, but the experience exposes too much platform machinery too early
- the user explicitly prioritized DX and asked that generated RPC calling style be improved
- the existing DX decision doc already points toward simplifying the outward story and hiding bridge ceremony

## Alternatives considered

1. Deep transport/runtime rewrite first
2. Docs/template cleanup only
3. App-builder-first productization + generated service façade

## Why chosen

Option 3 is the highest-leverage path that addresses both conceptual and code-level friction without paying the risk cost of a substrate rewrite.

## Consequences

- multiple repo surfaces must move together: docs, templates, generator, CLI, samples
- a migration story is required for old examples and generated client expectations
- some deeper runtime cleanliness opportunities may remain deferred

## Follow-ups

- define detailed API contract for generated client vNext
- specify migration/deprecation strategy for current generated shapes
- convert roadmap into executable tasks with test specs

---

# Architect review synthesis

## Steelman antithesis

A skeptic could argue that changing generated client ergonomics before cleaning substrate abstractions may create a prettier façade over technical debt, multiplying future migration cost when the underlying bridge inevitably evolves.

## Real tradeoff tension

- **Façade-first** improves user experience quickly with lower risk.
- **Substrate-first** may yield a cleaner long-term system, but it conflicts with the clarified boundary and delays user-visible gain.

## Synthesis

Proceed façade-first, but explicitly require compatibility seams, migration guidance, and test coverage so the façade does not freeze bad internals forever.

---

# Critic review verdict

**Verdict: APPROVE**

Rationale:
- principles align with chosen option
- alternatives were fairly considered and rejected for explicit reasons
- scope is testable and bounded
- verification path is concrete enough for downstream execution planning
- risk is acknowledged via migration/compatibility follow-ups

Residual caution:
- execution must not allow docs-only work to outrun generator/template changes, or the repo story will fragment further

---

# Available agent types roster for downstream execution

- `architect` — API and boundary validation for generated client surface
- `executor` — code changes across generator, CLI, docs, templates, samples
- `debugger` — diagnose drift/dev-loop/bridge readiness failures
- `test-engineer` — define and harden regression coverage
- `verifier` — confirm completion evidence before closure
- `writer` — docs and migration notes
- `code-reviewer` / `critic` — final quality pass on plan-to-implementation alignment

## Suggested staffing for `$ralph`

Use when you want one persistent owner executing sequentially with verification loops.

Recommended lane pattern:
1. `architect` (high) — lock generated client API and migration rules
2. `executor` (high) — implement workstream by workstream
3. `test-engineer` (medium) — add/adjust regression coverage after each slice
4. `verifier` (high) — run final evidence pass

## Suggested staffing for `$team`

Use when you want coordinated parallel execution.

Recommended lane split:
- Lane A: `executor` — generator/runtime/client façade
- Lane B: `executor` + `writer` — docs/templates/samples/story alignment
- Lane C: `debugger` / `test-engineer` — dev-loop automation, drift detection, diagnostics, regression tests
- Lead review: `architect` then `verifier`

Suggested reasoning levels by lane:
- API/boundary work: high
- implementation lanes: medium/high
- docs/writer lane: medium
- verification lane: high

## Launch hints

- Sequential lane: `$ralph .omx/plans/prd-fulora-developer-experience-refactor.md`
- Coordinated lane: `$team .omx/plans/prd-fulora-developer-experience-refactor.md`
- Team CLI equivalent: `omx team ".omx/plans/prd-fulora-developer-experience-refactor.md"`

## Team verification path

1. API contract review against PRD and migration constraints
2. Generator/runtime regression tests for old + new client shapes where required
3. Template/sample smoke validation for the primary app path
4. CLI/dev/package flow verification with actionable diagnostics
5. Docs/readme consistency pass so first-contact guidance matches shipped behavior
6. Final verifier pass collecting evidence before completion claim
