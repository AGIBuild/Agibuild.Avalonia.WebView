## Context

The `dotnet new agibuild-hybrid` template scaffolds a Fulora hybrid application with Desktop host, Bridge project, and Web frontend (React/Vue/Vanilla). Currently the template:
- Checks bridge connectivity via raw `window.agWebView?.rpc` in the frontend
- Does NOT use the `@agibuild/bridge` npm package
- Does NOT generate or consume TypeScript declarations (`bridge.d.ts`)
- Does NOT demonstrate shell capabilities (tray, menu, theme)
- Does NOT include bridge middleware

In contrast, `samples/avalonia-react/` demonstrates full typed bridge usage with `@agibuild/bridge`, service layer, readiness hook, and event handling. The gap between template and sample creates a poor first impression.

## Goals / Non-Goals

**Goals:**
- Upgrade all template frontend variants (React, Vue, Vanilla) to use `@agibuild/bridge` npm package
- Include typed service layer (`bridge/services.ts`) in template frontends
- Include `useBridgeReady()` hook for React, equivalent composable for Vue
- Integrate `bridge.d.ts` generation into template TypeScript configuration
- Demonstrate tray, menu, and theme capabilities in `app-shell` preset
- Include development-mode logging middleware
- Update template `HybridApp.Bridge` project with shell service interfaces

**Non-Goals:**
- Making the template a production application (it's a scaffold)
- Including every possible bridge service (show the pattern)
- Global shortcuts in template (too specialized for general use)
- Plugin system demonstration in template (separate concern)

## Decisions

### D1: @agibuild/bridge dependency — npm registry vs local reference

**Choice**: npm registry reference (`"@agibuild/bridge": "^1.0.0"`) in template `package.json`.

**Rationale**: The 1.0.0 package is published to npm (Phase 9 M9.3). Local file references only work in the monorepo. Published templates must use registry packages.

### D2: Service layer structure

**Choice**: Template includes `src/bridge/services.ts` with typed service proxies and `src/bridge/client.ts` with middleware-configured bridge client.

```
HybridApp.Web/
├── src/
│   ├── bridge/
│   │   ├── client.ts      # bridgeClient with middleware (logging in dev)
│   │   └── services.ts    # typed service proxies (getService<T>)
│   ├── hooks/
│   │   └── useBridge.ts   # useBridgeReady() hook (React)
│   └── App.tsx
```

**Rationale**: Separating client setup from service proxies follows the pattern in `samples/avalonia-react/`. The hook abstraction hides bridge readiness polling from components.

### D3: Shell capabilities in template — preset-gated

**Choice**: Tray, menu, and theme demonstrations are only included in the `app-shell` preset, not in the `baseline` preset. The `baseline` preset gets bridge + middleware only.

**Rationale**: Not all apps need shell capabilities. Preset gating keeps the baseline simple while showcasing full power in app-shell.

### D4: bridge.d.ts integration

**Choice**: Template includes MSBuild post-build step that generates `bridge.d.ts` into the web project's `src/bridge/` directory. `tsconfig.json` includes the generated file.

**Rationale**: This is the existing pipeline (`GenerateBridgeTypeScript` MSBuild property + `BridgeTypeScriptOutputDir`). Template just needs to configure it correctly.

### D5: Vue template — Composition API

**Choice**: Vue template uses Composition API with `useBridgeReady()` composable, consistent with Vue 3 best practices.

**Rationale**: Composition API is the standard for Vue 3+. Options API version not needed.

### D6: Vanilla template — minimal but typed

**Choice**: Vanilla template uses plain TypeScript with `bridge.d.ts` types. No framework, no bundler complexity. Demonstrates `bridgeClient.getService<T>()` directly.

**Rationale**: Vanilla variant exists for developers who don't want a framework. It should still show typed bridge usage.

## Testing Strategy

- **Template E2E tests**: `dotnet new agibuild-hybrid` → `dotnet build` → verify compilation succeeds
- **Template E2E tests**: `npm install && npm run build` in web project → verify frontend builds
- **Template E2E tests**: `app-shell` preset includes tray/menu/theme service registration → verify runtime startup
- **Smoke tests**: Verify bridge.d.ts is generated and includes expected service interfaces

## Risks / Trade-offs

- **[Template maintenance burden]** More code in template = more to maintain → Mitigation: template code should be minimal service layer, not full application logic
- **[npm version drift]** `@agibuild/bridge` version in template may lag → Mitigation: template version is pinned to latest at template release time; document update process
- **[Shell preset complexity]** `app-shell` preset adds significant code → Mitigation: clear separation between baseline and app-shell; commented sections explaining each capability
