# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-02-25

First stable release. Delivers a complete Electron replacement foundation for Avalonia hybrid apps across 5 platforms.

### Added

- **Cross-platform WebView control** — Windows (WebView2), macOS (WKWebView), Linux (WebKitGTK), iOS (WKWebView), Android (WebView) with unified API surface
- **Type-safe bidirectional bridge** — `[JsExport]` / `[JsImport]` attributes with Roslyn source-generated proxies, AOT-safe, zero runtime reflection
- **SPA hosting** — `app://` custom protocol scheme, embedded resource serving, dev server proxy with HMR, SPA router fallback
- **Application shell** — shell policy framework, multi-window lifecycle, host capability bridge (clipboard, file dialogs, external open, notifications)
- **Policy-first execution model** — capability gateway with deterministic allow/deny/failure outcomes, zero-bypass enforcement
- **Agent-friendly diagnostics** — structured runtime diagnostics for critical flows, machine-readable export protocol
- **Project template** — `dotnet new agibuild-hybrid` with React/Vue/vanilla options and shell presets (baseline, app-shell)
- **MockBridge testing** — source-generated mock-friendly types for unit testing bridge contracts without a real browser
- **Bridge security** — origin allowlisting, channel isolation, rate limiting, WebMessage policy pipeline
- **Rich feature set** — cookies, commands, screenshots, PDF export, zoom, find-in-page, preload scripts, context menu, downloads, permissions, web resource interception
- **DevTools toggle** — runtime open/close inspector API
- **WebDialog and WebAuthBroker** — OAuth authentication and dialog workflows
- **DI integration** — `AddWebView()` service collection extensions
- **Bridge call tracing** — structured logging with `IBridgeTracer`
- **TypeScript generation** — `.d.ts` type definitions from C# bridge interfaces
- **React sample app** — full-stack Avalonia + React (Vite) hybrid application
- **CI/CD pipeline** — Nuke-based build with automated test lanes, coverage enforcement (90%+ threshold), warning governance, OpenSpec strict validation
- **915 automated tests** (766 unit + 149 integration) with 95.87% line coverage

### Release Notes

This release represents the culmination of Phases 0-5 of the project roadmap:

- **Phase 0** — Foundation: cross-platform adapters, navigation, WebMessage bridge
- **Phase 1** — Type-Safe Bridge: source generator, proxy generation, MockBridge
- **Phase 2** — SPA Hosting: custom protocol, embedded resources, HMR proxy
- **Phase 3** — Polish & GA: project template, API docs, GTK/Linux validation
- **Phase 4** — Application Shell: shell policies, multi-window, host capabilities
- **Phase 5** — Electron Replacement Foundation: typed gateway, policy-first execution, agent-friendly diagnostics

#### Version Progression

To produce stable 1.0.0 packages:

```bash
git tag v1.0.0
git push origin v1.0.0
nuke CiPublish
```

MinVer derives the version from the nearest ancestor git tag. Preview builds use `v1.0.0-preview.N` tags.
