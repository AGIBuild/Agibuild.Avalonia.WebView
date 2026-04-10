# Deep Interview Spec — Fulora Developer Experience Refactor

## Metadata

- Profile: quick
- Rounds: 5
- Final ambiguity: 28.8%
- Threshold: 30%
- Context type: brownfield
- Context snapshot: `.omx/context/ellipsis-20260406T073800Z.md`
- Interview transcript: `.omx/interviews/fulora-developer-experience-refactor-20260406T081331Z.md`
- Recommended next mode: `$ralplan`

## Clarity breakdown


| Dimension   | Score |
| ----------- | ----- |
| Intent      | 0.80  |
| Outcome     | 0.85  |
| Scope       | 0.72  |
| Constraints | 0.62  |
| Success     | 0.55  |
| Context     | 0.75  |


## Intent

Improve, optimize, and selectively refactor Fulora's developer experience so Fulora feels easier to adopt and operate as an application framework, not just a capable hybrid platform.

## Desired outcome

Produce an execution-ready planning package that combines:

1. a prioritized DX improvement list with rationale
2. a phased roadmap
3. a concrete RPC/client ergonomics redesign direction with code examples

## In scope

- Prioritizing the application developer's day-1 / day-2 journey
- Improving the primary path around `fulora new -> fulora dev -> fulora package`
- Lowering ceremony around bridge/RPC usage for app developers
- Improving generated client ergonomics, default calling patterns, and example code
- Improving surrounding DX surfaces that directly shape app-builder experience: docs framing, template defaults, dev loop, diagnostics, packaging path discoverability
- Identifying what should be improved first versus later

## Out of scope / non-goals

- No deep rewrite of the low-level transport/runtime protocol in this planning pass
- No plugin-authoring model redesign as the first priority
- No contributor-workflow overhaul as the primary target
- No removal of Fulora's bridge architecture, platform layering, or advanced capabilities

## Decision boundaries

OMX may decide without further confirmation:

- which DX areas are highest priority
- which improvements should be grouped into phases
- how to redesign the generated RPC/client surface at the API level
- what code examples best illustrate the redesigned RPC/client experience

OMX should preserve without overriding:

- the app-builder-first priority
- the boundary against a transport/runtime rewrite
- the requirement that final planning output include prioritization, roadmap, and RPC redesign examples together

## Constraints

- Brownfield repo with existing runtime, generator, CLI, templates, samples, docs, and tests
- Existing DX material already points toward `new -> dev -> package`
- Existing runtime/generator behavior already emits string-based RPC stubs, so proposed DX improvements should account for migration and backward compatibility
- Planning should use repository evidence rather than assume greenfield freedom

## Testable acceptance criteria

A successful next-stage plan should:

- identify the top DX problems in ranked order with evidence from the repository and docs
- define a phased roadmap with clear scope boundaries
- include a proposed app-facing RPC/client API shape with concrete code examples
- distinguish short-term DX wins from medium-term structural work
- preserve current transport/runtime foundations unless a later, separately justified phase is approved
- produce planning artifacts suitable for downstream execution modes (`$ralph` or `$team`)

## Assumptions exposed + resolutions


| Assumption                                          | Resolution                                                                    |
| --------------------------------------------------- | ----------------------------------------------------------------------------- |
| DX work might need to cover every persona equally   | Rejected; app builders are the first priority                                 |
| RPC ergonomics may be out of scope                  | Rejected; user explicitly wants it considered and exemplified                 |
| Better DX might require rewriting runtime transport | Rejected for this planning pass                                               |
| User wants only one artifact type                   | Rejected; user wants priority list + roadmap + RPC redesign examples together |


## Pressure-pass findings

The key pressure pass challenged whether the effort should remain narrowly focused on app builders or broaden toward plugin/contributor workflows. The narrowed scope was accepted, but the user additionally expanded priority to include redesign of the auto-generated RPC surface. This changed the likely plan center of gravity toward app-facing client ergonomics rather than generic documentation cleanup alone.

## Brownfield evidence vs inference

### Evidence

- README already markets Fulora around a fast path: `fulora new`, `fulora dev`, `fulora package`.
- `docs/superpowers/specs/2026-04-04-fulora-dx-decision-document.md` already argues that Fulora's main DX issue is overexposure of platform complexity, not missing capability.
- `src/Agibuild.Fulora.Runtime/RuntimeBridgeService.cs` currently generates string-based JS stubs like `window.agWebView.rpc.invoke('{service}.{method}', params)`.
- Template code at `templates/agibuild-hybrid/HybridApp.Web.Vite.React/src/bridge/generated/bridge.client.ts` currently exposes generated service objects directly over raw `rpc.invoke(...)` calls.

### Inference

- The highest-leverage DX work is likely not inventing new capability, but productizing and simplifying the existing surface.
- RPC/client ergonomics are a strong leverage point because they expose core platform sophistication directly to app developers.

## Technical context findings

- Relevant docs: `README.md`, `docs/product-platform-roadmap.md`, `docs/superpowers/specs/2026-04-04-fulora-dx-decision-document.md`
- Relevant runtime/generation surfaces:
  - `src/Agibuild.Fulora.Runtime/RuntimeBridgeService.cs`
  - `src/Agibuild.Fulora.Cli/Commands/GenerateCommand.cs`
  - `templates/agibuild-hybrid/HybridApp.Web.Vite.React/src/bridge/client.ts`
  - `templates/agibuild-hybrid/HybridApp.Web.Vite.React/src/bridge/generated/bridge.client.ts`
  - sample `src/bridge/client.ts` and `src/bridge/services.ts` files under `samples/`

## Candidate RPC/client redesign examples

### Current feel

```ts
await window.agWebView.rpc.invoke("GreeterService.greet", { name: "World" });
```

### Preferred app-facing shape

```ts
import { services } from "./bridge/client";

const msg = await services.greeter.greet({ name: "World" });
await services.notifications.show({ message: "File saved!" });
```

### Alternative factory shape

```ts
import { createFuloraClient } from "./bridge/client";

const app = createFuloraClient();
const profile = await app.userProfile.get();
await app.dialogs.openFile();
```

### Generated client sketch

```ts
export interface GreeterService {
  greet(input: { name: string }): Promise<string>;
}

export const services = {
  greeter: {
    greet(input) {
      return rpc.invoke("GreeterService.greet", input);
    }
  }
};
```

## Condensed transcript

- User asked for a deep interview, then tried to jump to ralplan with an unspecified task.
- Through five rounds, the task clarified to a Fulora DX improvement/refactor plan centered on app-builder experience.
- The user delegated prioritization to OMX.
- The user explicitly asked to include better RPC client design and code examples.
- The user accepted the boundary that this effort should not rewrite low-level runtime transport.

