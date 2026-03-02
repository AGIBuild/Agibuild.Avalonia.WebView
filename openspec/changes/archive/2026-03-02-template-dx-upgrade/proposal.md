## Why

The current `dotnet new agibuild-hybrid` template checks bridge connectivity via raw `window.agWebView?.rpc` but does not use the `@agibuild/bridge` npm package, typed service layer, or any shell capabilities (tray, menu, theme). This means developers' first experience with Fulora misses the framework's core differentiators — type-safe bridge, shell capabilities, and policy governance. The gap between the template (minimal) and the React sample (full-featured) is large enough to confuse new adopters.

**Goal IDs**: E1 (Project Template — first impression should showcase full power), G1 (Type-Safe Bridge — template must demonstrate it), E2 (Dev Tooling — template includes bridge tracing)

**ROADMAP justification**: Post-1.0 adoption. The template is the #1 onboarding path. Every new capability (tray, menu, theme, middleware) should be reflected in the template to maximize adoption impact.

## What Changes

- Integrate `@agibuild/bridge` npm package into all template frontend variants (React, Vue, Vanilla)
- Add typed service layer in template: `bridge/services.ts` with typed service proxies
- Add `useBridgeReady()` hook (React) / equivalent for Vue/Vanilla
- Include generated `bridge.d.ts` in template TypeScript configuration
- Add system tray and native menu demonstration in `app-shell` preset
- Add theme synchronization example (dark/light mode reacting to OS theme)
- Add bridge middleware example (logging middleware enabled in development)
- Update template README with architecture overview and bridge usage guide

## Non-goals

- Making the template a production application — it's a starting scaffold
- Including every possible bridge service — demonstrate the pattern, not exhaustive coverage
- Global shortcuts in template — too specialized for a general template

## Capabilities

### New Capabilities
- None (template upgrade is an integration of existing and new capabilities)

### Modified Capabilities
- `project-template`: Upgrade template to use `@agibuild/bridge` npm package, typed service layer, shell capability demonstrations (tray, menu, theme), and JS middleware examples
- `template-shell-presets`: Add tray icon, native menu, and theme sync to `app-shell` preset configuration

## Impact

- `templates/agibuild-hybrid/` — all frontend variants updated
- `templates/agibuild-hybrid/HybridApp.Web.Vite.React/` — typed services, bridge hook, middleware, theme
- `templates/agibuild-hybrid/HybridApp.Web.Vite.Vue/` — equivalent updates
- `templates/agibuild-hybrid/HybridApp.Web.Vanilla/` — simplified bridge usage
- `templates/agibuild-hybrid/HybridApp.Desktop/` — tray/menu/theme provider wiring
- `templates/agibuild-hybrid/HybridApp.Bridge/` — theme service, tray/menu service interfaces
- `tests/` — template E2E tests updated to verify tray/menu/theme/bridge flows
