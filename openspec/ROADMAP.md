# Agibuild.Avalonia.WebView — Roadmap

> Phased delivery plan aligned with [Project Goals](./PROJECT.md).
> Each phase is independently shippable and builds on the previous one.

---

## Phase Overview

```
Phase 0 (✅ Done)        Phase 1 (✅ Done)       Phase 2 (✅ Core Done)  Phase 3
Foundation               Type-Safe Bridge       SPA Hosting            Polish & GA
─────────────────────    ────────────────────   ────────────────────   ────────────────────
• Cross-platform         • Source Generator     • Custom protocol      • Project template
  adapters (5 platforms)   for C# → JS proxy      file serving        • API docs site
• Full-control           • Source Generator     • Embedded resource    • Performance
  navigation               for JS → C# proxy     provider               benchmarks
• WebMessage bridge      • TypeScript .d.ts     • Dev mode HMR proxy   • GA release
  with policy              generation           • SPA router fallback    readiness review
• Cookies, Commands,     • Bridge security      • Bridge + SPA         • Breaking change
  Screenshot, PDF,         integration            integration            audit
  RPC, Zoom, Find,       • MockBridge for       • Sample: Avalonia     • GTK/Linux
  Preload, ContextMenu     unit testing           + React app            smoke validation
• 391 CT + 80 IT         • Migration path       • Sample: Avalonia
• WebDialog, Auth          from raw RPC           + Vue app
```

---

## Phase 0: Foundation (✅ Completed)

**Goal**: Establish a production-quality cross-platform WebView control with contract-driven design.

**Status**: All 18 changes archived. 391 unit tests, 80 integration tests, 96%+ line coverage.

<details>
<summary>Delivered capabilities (click to expand)</summary>

| Capability | Goal ID | Change |
|---|---|---|
| Project structure & contracts | F1 | init-project-structure |
| Contract semantics v1 | F2 | update-webview-contract-specs-v1 |
| WKWebView adapter (macOS) M0+M1 | F1 | wkwebview-adapter-m0, wkwebview-adapter-m1 |
| WebView2 adapter (Windows) M0 | F1 | webview2-adapter-m0 |
| Android WebView adapter M0 | F1 | android-webview-adapter-m0 |
| Adapter lifecycle & platform handles | F5 | adapter-lifecycle-and-platform-handle |
| Download management | F4 | download-management |
| Permission request handling | F4 | permission-request-handling |
| Web resource interception | F4 | web-resource-interception |
| Command manager | F4 | command-manager |
| Context menu | F4 | context-menu |
| Find in page | F4 | find-in-page |
| JS ↔ C# RPC | F6 | js-csharp-rpc |
| Preload scripts | F4 | preload-script |
| Print to PDF | F4 | print-to-pdf |
| Screenshot capture | F4 | screenshot-capture |
| Zoom control | F4 | zoom-control |

</details>

---

## Phase 1: Type-Safe Bidirectional Bridge

**Goal**: [G1] — Make C# ↔ JS communication as natural as calling local methods, with compile-time type safety.

**Why this first**: This is the single biggest differentiator vs Electron and all existing WebView solutions. It transforms the project from "a WebView control" into "a hybrid app framework".

### Design Decisions (confirmed)

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Attribute naming | `[JsExport]` / `[JsImport]` | Concise, symmetric, clear direction (Export = C#→JS, Import = JS→C#). Namespace `Agibuild.Avalonia.WebView` avoids collision with .NET WASM `JSExport`. |
| Bridge vs EnableWebMessageBridge | Bridge is independent; auto-enables bridge if needed | Backward compatible; `Bridge.Expose<T>()` works standalone |
| JS method naming | `ServiceName.methodName` (camelCase) | Avoids collision; JS-natural naming |
| Parameter format | JSON-RPC 2.0 named params (object) | Supports optional params; readable |
| Enum serialization | string (by name) | TypeScript string literal union friendly |
| DateTime serialization | ISO 8601 string | Cross-language standard |
| Complex type serialization | Source Generator generates STJ context | AOT safe; no runtime reflection |
| Bridge stub injection | Preload Script (F4) + `onBridgeReady` callback | Guarantees document-start timing |
| Error propagation | `message` always; `data.type` only when DevTools enabled | Security: no stack trace leak |
| V1 scope exclusions | No generics in methods, no overloads, no ref/out, no CancellationToken, no IAsyncEnumerable | Control complexity; iterate later |
| Source Generator project | Separate `netstandard2.0` analyzer project | Roslyn requirement |

### 1.1 Bridge Contracts & Source Generator (C# → JS)

**Attributes**: `[JsExport]` marks an interface whose C# implementation is exposed to JavaScript. `[JsImport]` marks an interface whose methods are implemented in JavaScript and called from C# via a generated proxy.

```csharp
// Developer writes — export C# service to JS:
[JsExport]
public interface IAppService
{
    Task<UserProfile> GetCurrentUser();
    Task SaveSettings(AppSettings settings);
    Task<List<Item>> SearchItems(string query, int limit);
}

// Developer implements:
public class AppService : IAppService { /* ... */ }

// Developer registers:
webView.Bridge.Expose<IAppService>(new AppService());
```

```typescript
// Auto-generated TypeScript (consumed by web app):
export interface IAppService {
    getCurrentUser(): Promise<UserProfile>;
    saveSettings(settings: AppSettings): Promise<void>;
    searchItems(query: string, limit: number): Promise<Item[]>;
}

// Usage in React/Vue/Svelte:
const user = await appService.getCurrentUser();
//                 ^^^^^^^^^^^^^^^^^ full IntelliSense
```

**Source Generator produces** (per `[JsExport]` interface):
1. `*BridgeHost` — registers RPC handlers that deserialize params, call implementation, serialize result
2. `*BridgeJsStub` — JS client stub string constant for injection
3. `*BridgeJsonContext` — STJ serialization context for all parameter/return types (AOT safe)

**Key design decisions**:
- Source Generator (not runtime reflection) for AOT compatibility and performance
- Builds on existing JSON-RPC 2.0 infrastructure (F6)
- Method name convention: C# `PascalCase` → JS `camelCase` automatic mapping
- Complex types serialized via System.Text.Json with source-generated context
- Service name derived from interface name (strip `I` prefix), customizable via `[JsExport(Name = "...")]`

### 1.2 Bridge Source Generator (JS → C#)

`[JsImport]` marks interfaces implemented in JavaScript. Source Generator produces a C# proxy class:

```csharp
[JsImport]
public interface IUiController
{
    Task ShowNotification(string message, string? title = null);
    Task<bool> ConfirmDialog(string prompt);
    Task UpdateTheme(ThemeOptions options);
}

// Usage in C#:
var ui = webView.Bridge.GetProxy<IUiController>();
await ui.ShowNotification("Settings saved!");
bool confirmed = await ui.ConfirmDialog("Delete this item?");
```

**Source Generator produces** (per `[JsImport]` interface):
1. `*BridgeProxy` — implements the interface, each method calls `Rpc.InvokeAsync` with correct name and serialized params

### 1.3 TypeScript Type Generation

Build-time MSBuild target that:
- Scans assemblies for `[JsExport]` and `[JsImport]` interfaces
- Generates `.d.ts` files into the web project's `src/types/` directory
- Generates a thin JS runtime (`@agibuild/bridge`) that connects to the WebMessage channel

### 1.4 Bridge Security Integration

Integrate with existing WebMessage policy infrastructure:

```csharp
webView.Bridge.Expose<IAppService>(new AppService(), new BridgeOptions
{
    AllowedOrigins = ["app://myapp"],    // reuses WebMessagePolicy
    RateLimit = new RateLimit(100, TimeSpan.FromSeconds(1)),
});
```

- All bridge calls go through the existing `IWebMessagePolicy` pipeline
- Failed calls return JSON-RPC error with appropriate code
- Rate limiting prevents DoS from compromised web content

### 1.5 MockBridge for Testing

Source generator also produces mock-friendly types:

```csharp
// Unit test without any WebView:
var mockBridge = new MockBridge<IAppService>();
mockBridge.Setup(s => s.GetCurrentUser(), new UserProfile { Name = "Test" });

var vm = new MyViewModel(mockBridge.Proxy);
await vm.LoadUser();
Assert.Equal("Test", vm.UserName);
```

### 1.6 Migration Path

Smooth upgrade from existing raw RPC (F6):
- `[JsExport]` / `[JsImport]` are opt-in; raw `Rpc.Handle()` continues to work
- Bridge internally uses the same JSON-RPC 2.0 transport
- Incremental adoption: convert one method at a time

### Implementation Changes (breakdown of deliverables)

Deliverable 1.1 is split into sub-changes for manageable delivery:

| Change | Scope | Complexity |
|--------|-------|------------|
| **bridge-contracts** | `[JsExport]`/`[JsImport]` attrs, `IBridgeService`, `BridgeOptions`, `RuntimeBridgeService` (manual registration), `WebViewCore.Bridge` property, CT coverage | Medium |
| **bridge-source-generator** | New `Agibuild.Avalonia.WebView.Bridge.Generator` project, Roslyn incremental generator, BridgeHost/BridgeProxy/JsStub emitters, CT for generated code | High |
| **bridge-integration** | Wire into WebView control, E2E test in Integration Test App, migration from raw RPC | Medium |

### Deliverables

| # | Deliverable | Depends On | Est. Complexity |
|---|---|---|---|
| 1.1a | Bridge contracts + runtime service (manual registration) | F6 (RPC) | Medium |
| 1.1b | C#→JS source generator (`[JsExport]`) | 1.1a | High |
| 1.1c | Bridge integration + E2E validation | 1.1b | Medium |
| 1.2 | JS→C# source generator (`[JsImport]`) + C# proxy | 1.1b | Medium |
| 1.3 | TypeScript `.d.ts` generation (MSBuild target) | 1.1b | Medium |
| 1.4 | Bridge security integration (policy, rate limit) | 1.1a + F3 (Policy) | Medium |
| 1.5 | MockBridge generator for unit testing | 1.1b | Medium |
| 1.6 | Migration guide + backward compatibility tests | 1.1a | Low |
| 1.7 | Contract tests + integration tests for bridge | 1.1a-1.5 | Medium |

### Project Structure (new/modified)

```
src/
├── Agibuild.Avalonia.WebView.Core/
│   ├── JsExportAttribute.cs                  ← NEW
│   ├── JsImportAttribute.cs                  ← NEW
│   └── IBridgeService.cs                     ← NEW
├── Agibuild.Avalonia.WebView.Runtime/
│   ├── RuntimeBridgeService.cs               ← NEW
│   └── WebViewCore.cs                        ← MODIFIED (Bridge property)
├── Agibuild.Avalonia.WebView.Bridge.Generator/  ← NEW PROJECT (netstandard2.0)
│   ├── WebViewBridgeGenerator.cs
│   ├── BridgeHostEmitter.cs
│   ├── BridgeProxyEmitter.cs
│   ├── JsStubEmitter.cs
│   └── TypeMapper.cs
└── Agibuild.Avalonia.WebView/
    └── WebView.cs                            ← MODIFIED (Bridge API)
```

---

## Phase 2: First-Class SPA Hosting

**Goal**: [G2] — Make it trivial to host a React/Vue/Svelte app inside the WebView with full bridge integration.

**Why**: The Type-Safe Bridge (Phase 1) provides the communication layer; SPA Hosting provides the content delivery layer. Together they form the complete hybrid app framework.

### 2.1 Embedded Resource Provider

High-level API for serving static files from embedded resources:

```csharp
// Register during setup
WebViewEnvironment.Initialize(options =>
{
    options.AddEmbeddedFileProvider("app",  // scheme: app://
        Assembly.GetExecutingAssembly(),
        "wwwroot");                          // embedded resource prefix
});

// WebView navigates to app://localhost/index.html
// SPA router paths (app://localhost/settings) fallback to index.html
```

Implementation builds on existing `CustomSchemeRegistration` + `WebResourceRequested`:
- Register custom scheme at environment init
- Handle `WebResourceRequested` by resolving embedded resources
- SPA fallback: if path not found and no file extension, serve `index.html`
- Content-Type detection by file extension
- Caching headers for immutable assets (hashed filenames)

### 2.2 Development Mode (HMR Proxy)

For development, proxy to a local dev server (Vite, webpack, etc.):

```csharp
#if DEBUG
WebViewEnvironment.Initialize(options =>
{
    options.AddDevServerProxy("app", "http://localhost:5173");
    // Proxies app:// requests to Vite dev server
    // WebSocket HMR works through the proxy
});
#endif
```

- Transparent switch between dev (proxy) and production (embedded)
- Same `app://` URLs in both modes — no code changes needed
- Bridge state preserved across HMR reloads

### 2.3 Bridge + SPA Integration

When both Bridge and SPA Hosting are configured:
- Auto-inject bridge client script into pages served via `app://`
- Bridge is available as `window.__agibuild.bridge` or via npm package import
- TypeScript types from Phase 1.3 resolve naturally in the web project

### 2.4 Sample Applications

| Sample | Stack | Demonstrates |
|---|---|---|
| `samples/avalonia-react` | Avalonia + React (Vite) | Full hybrid app with typed bridge |
| `samples/avalonia-vue` | Avalonia + Vue (Vite) | Same patterns, different frontend |
| `samples/minimal-hybrid` | Avalonia + vanilla JS | Minimal setup, no build tools |

### Deliverables

| # | Deliverable | Depends On | Est. Complexity |
|---|---|---|---|
| 2.1 | Embedded resource file provider | F4 (WebResource) | Medium |
| 2.2 | SPA router fallback logic | 2.1 | Low |
| 2.3 | Dev server proxy mode | 2.1 | Medium |
| 2.4 | Bridge auto-injection for `app://` | 2.1 + Phase 1 | Low |
| 2.5 | npm package `@agibuild/bridge` | Phase 1.3 | Medium |
| 2.6 | Sample: Avalonia + React | 2.1-2.5 | Medium |
| 2.7 | Sample: Avalonia + Vue | 2.1-2.5 | Low |
| 2.8 | Sample: Minimal hybrid (vanilla JS) | 2.1 | Low |

---

## Phase 3: Polish & General Availability

**Goal**: [E1, E2, E3] — Production readiness, developer experience, and ecosystem.

### 3.1 Project Template

```bash
dotnet new agibuild-hybrid -n MyApp --frontend react
# Creates:
#   MyApp/
#   ├── MyApp.Desktop/          (Avalonia desktop host)
#   ├── MyApp.Mobile/           (Avalonia mobile host)
#   ├── MyApp.Bridge/           (shared bridge interfaces)
#   ├── MyApp.Web/              (React/Vue frontend)
#   └── MyApp.Tests/            (unit tests with MockBridge)
```

### 3.2 Developer Tooling

- Bridge call tracing with structured logging
- DevTools toggle API (runtime open/close inspector)
- Bridge method call visualization (optional debug overlay)

### 3.3 Performance & Quality

- Performance benchmarks (bridge latency, SPA load time, memory)
- GTK/Linux smoke validation (currently marked "Untested")
- Branch coverage improvement (84% → 90%+)
- API surface breaking change audit (Preview → Stable)

### 3.4 Documentation & Ecosystem

- API reference site (generated from XML docs)
- Getting Started guide
- Architecture decision records
- Contributing guide

### 3.5 GA Release

- Semantic versioning (1.0.0)
- NuGet stable package
- GitHub Release with changelog

### Deliverables

| # | Deliverable | Depends On | Est. Complexity |
|---|---|---|---|
| 3.1 | ✅ `dotnet new agibuild-hybrid` project template | Phase 1 + 2 | Medium |
| 3.2 | ✅ Bridge call tracing + logging (IBridgeTracer) | Phase 1 | Low |
| 3.3 | ✅ DevTools runtime toggle API (IDevToolsAdapter) | F4 | Low |
| 3.4 | ✅ Performance benchmarks (BenchmarkDotNet) | Phase 1 + 2 | Medium |
| 3.5 | GTK/Linux smoke tests | F1 | Medium |
| 3.6 | ✅ API reference site (docfx + XML docs) | — | Medium |
| 3.7 | ✅ Getting Started + topic guides | Phase 1 + 2 | Medium |
| 3.8 | ✅ API surface review + breaking change audit | All | Low |

---

## Dependencies & Prerequisites

```
Phase 0 (✅ Done) ──► Phase 1 (✅ Done) ──► Phase 2 (✅ Core Done) ──► Phase 3 (✅ Done)
     │                      │                       │
     │                      │                       └── 2.4 depends on Phase 1
     │                      └── Builds on F6 (RPC) + F3 (Policy)
     └── F4 (WebResource) used by Phase 2
```

Phase 1 and Phase 2 are mostly independent in implementation but compose together. Phase 1 is the higher priority because it delivers the biggest differentiator.

---

## Risk Register

| Risk | Impact | Mitigation |
|------|--------|------------|
| Source Generator complexity (Roslyn API) | Phase 1 delay | Start with simple cases (no generics, no overloads), iterate |
| TypeScript generation edge cases | Type mismatch bugs | Use System.Text.Json contract model as single source of truth |
| Platform WebView JS injection timing | Bridge not ready when page loads | Use preload scripts (F4) to ensure bridge is available at document-start |
| SPA routing conflicts with custom scheme | 404 on client routes | SPA fallback is proven pattern (Tauri, Electron) — low risk |
| AOT/NativeAOT compatibility | Source generator must not use reflection | Design constraint from day 1 — source gen is inherently AOT-safe |

---

## References

- [Project Goals](./PROJECT.md) — Vision, competitive analysis, and goal definitions
- [Compatibility Matrix](../docs/agibuild_webview_compatibility_matrix_proposal.md) — Platform support
- [Design Document](../docs/agibuild_webview_design_doc.md) — Architecture and contracts
