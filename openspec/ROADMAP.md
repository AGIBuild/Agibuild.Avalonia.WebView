# Fulora — Roadmap

> Phased delivery plan aligned with [Project Goals](./PROJECT.md).
> Each phase is independently shippable and builds on the previous one.

---

## Phase Overview

```
Phase 0 (✅ Done)        Phase 1 (✅ Done)       Phase 2 (✅ Core Done)  Phase 3 (✅ Done)      Phase 4 (✅ Done)      Phase 5 (✅ Completed)        Phase 6 (✅ Completed)         Phase 7 (✅ Completed)       Phase 8 (✅ Completed)                Phase 9 (🚧 Active)
Foundation               Type-Safe Bridge       SPA Hosting            Polish & GA            Application Shell       Framework Positioning Foundation Governance Productization        Release Orchestration      Bridge V2 & Platform Parity          GA Release Readiness
─────────────────────    ────────────────────   ────────────────────   ────────────────────   ────────────────────    ───────────────────────────────── ─────────────────────────────── ─────────────────────────   ─────────────────────────────────   ─────────────────────────
• Cross-platform         • Source Generator     • Custom protocol      • Project template      • Shell policy kit      • Typed capability gateway                                                                 • Bridge diagnostics safety net     • API surface freeze
  adapters (5 platforms)   for C# → JS proxy      file serving        • API docs site           (new window/download/ • Policy-first execution model                                                                • Cancellation + streaming parity   • npm bridge publication
• Full-control           • Source Generator     • Embedded resource    • Performance             permission/session)   • Agent-friendly diagnostics                                                                  • Overloads and generic boundaries  • Performance re-baseline
  navigation               for JS → C# proxy     provider               benchmarks             • Multi-window lifecycle • Web-first template flow                                                                    • Binary payload (byte[] ↔ Uint8Array)• Changelog & release notes
• WebMessage bridge      • TypeScript .d.ts     • Dev mode HMR proxy   • GA release            • Host capability bridge • Pain-point-driven governance                                                                • SPA asset hot update              • Migration guide
  with policy              generation           • SPA router fallback    readiness review         (clipboard/file dialogs/                                                                                             • Shell activation orchestration    • 1.0.0 stable release
• Cookies, Commands,     • Bridge security      • Bridge + SPA         • Breaking change         external open/notify)                                                                                                 • Deep-link native registration
  Screenshot, PDF,         integration            integration            audit                  • Shell presets in template                                                                                             • Platform feature parity closure
  RPC, Zoom, Find,       • MockBridge for       • Sample: Avalonia     • GTK/Linux             • Stress + soak automation
  Preload, ContextMenu     unit testing           + React app            smoke validation
• 1113 CT + 180 IT       • Migration path       • Sample: Avalonia
• WebDialog, Auth          from raw RPC           + Vue app
```

---

## Phase 0: Foundation (✅ Completed)

**Goal**: Establish a production-quality cross-platform WebView control with contract-driven design.

**Status**: All 18 changes archived. 1113 unit tests, 180 integration tests, 95%+ line coverage.

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

**Why this first**: This is the single biggest differentiator vs existing WebView wrappers. It transforms the project from "a WebView control" into "a hybrid app framework".

### Design Decisions (confirmed)

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Attribute naming | `[JsExport]` / `[JsImport]` | Concise, symmetric, clear direction (Export = C#→JS, Import = JS→C#). Namespace `Agibuild.Fulora` avoids collision with .NET WASM `JSExport`. |
| Bridge vs EnableWebMessageBridge | Bridge is independent; auto-enables bridge if needed | Backward compatible; `Bridge.Expose<T>()` works standalone |
| JS method naming | `ServiceName.methodName` (camelCase) | Avoids collision; JS-natural naming |
| Parameter format | JSON-RPC 2.0 named params (object) | Supports optional params; readable |
| Enum serialization | string (by name) | TypeScript string literal union friendly |
| DateTime serialization | ISO 8601 string | Cross-language standard |
| Complex type serialization | Source Generator generates STJ context | AOT safe; no runtime reflection |
| Bridge stub injection | Preload Script (F4) + `onBridgeReady` callback | Guarantees document-start timing |
| Error propagation | `message` always; `data.type` only when DevTools enabled | Security: no stack trace leak |
| V1 scope exclusions | No generics in methods, no ref/out | Control complexity; overloads, CancellationToken, IAsyncEnumerable, and byte[] added in Phase 8 (Bridge V2) |
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
| **bridge-source-generator** | New `Agibuild.Fulora.Bridge.Generator` project, Roslyn incremental generator, BridgeHost/BridgeProxy/JsStub emitters, CT for generated code | High |
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
├── Agibuild.Fulora.Core/
│   ├── JsExportAttribute.cs                  ← NEW
│   ├── JsImportAttribute.cs                  ← NEW
│   └── IBridgeService.cs                     ← NEW
├── Agibuild.Fulora.Runtime/
│   ├── RuntimeBridgeService.cs               ← NEW
│   └── WebViewCore.cs                        ← MODIFIED (Bridge property)
├── Agibuild.Fulora.Bridge.Generator/  ← NEW PROJECT (netstandard2.0)
│   ├── WebViewBridgeGenerator.cs
│   ├── BridgeHostEmitter.cs
│   ├── BridgeProxyEmitter.cs
│   ├── JsStubEmitter.cs
│   └── TypeMapper.cs
└── Agibuild.Fulora/
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
| 3.5 | ✅ GTK/Linux smoke tests | F1 | Medium |
| 3.6 | ✅ API reference site (docfx + XML docs) | — | Medium |
| 3.7 | ✅ Getting Started + topic guides | Phase 1 + 2 | Medium |
| 3.8 | ✅ API surface review + breaking change audit | All | Low |

---

## Phase 4: Application Shell Capabilities (✅ Completed)

**Goal**: Extend the framework from "hybrid WebView runtime" to "application shell platform" for real product scenarios, while preserving **G3 (Secure by Default)** and **G4 (Contract-Driven Testability)**.

**Why now**: Phase 0-3 established stable runtime, typed bridge, SPA hosting, and GA readiness. The next leverage point is reducing host-app boilerplate for multi-window and system integration so teams can ship full desktop/mobile products, not just embedded web surfaces.

### Milestones

| Milestone | Focus | Outcome |
|---|---|---|
| **M4.1 Shell Policy Foundation** | Define opt-in shell policy model (new-window, download, permission, session) | Unified, UI-agnostic policy surface with deterministic runtime wiring and CT coverage |
| **M4.2 Multi-window Lifecycle** | Window orchestration primitives and strategies (in-place/new dialog/external/delegate) | Predictable window lifecycle, routing, and teardown semantics across platforms |
| **M4.3 Host Capability Bridge** | Typed host capabilities (clipboard, file dialogs, external open, notifications) | Secure, explicit host capability exposure model with clear allow/deny semantics |
| **M4.4 Session & Permission Profiles** | Per-window/per-scope session isolation and permission profiles | Enterprise-ready security posture for hybrid apps with auditable policy behavior |
| **M4.5 Shell DX & Templates** | Bring shell presets into `dotnet new agibuild-hybrid` and tooling | Out-of-box shell-enabled starter experience with minimal setup friction |
| **M4.6 Hardening & Production Validation** | Long-run stress/soak automation + compatibility matrix refresh | Confidence for production adoption under sustained attach/detach and multi-window workloads |

### Phase 4 Deliverables

| # | Deliverable | Depends On | Est. Complexity |
|---|---|---|---|
| 4.1 | ✅ Shell policy contracts + runtime wiring (opt-in, non-breaking) | Phase 3 + F2/F3/F4 | Medium |
| 4.2 | ✅ Multi-window strategy framework + lifecycle semantics tests | 4.1 | High |
| 4.3 | ✅ Typed host capability bridge (initial capability set) | 4.1 + G1 | High |
| 4.4 | ✅ Session/permission profiles and governance rules | 4.1 + G3 | Medium |
| 4.5 | ✅ Template shell presets + samples (production-oriented) | 4.1-4.4 + E1 | Medium |
| 4.6 | ✅ Shell stress/soak lane + release-readiness checklist | 4.1-4.5 + G4 | Medium |

### Phase 4 Exit Criteria

- Shell policies are opt-in and do not regress existing baseline behaviors when disabled.
- Multi-window + host capability flows are testable in contract/integration automation.
- Windows/macOS/Linux shell scenarios have passing smoke/stress coverage.
- Template path demonstrates desktop-grade app shell capability with minimal host code.

---

## Phase 5: Framework Positioning Foundation (✅ Completed)

**Goal**: Establish a framework-grade C# + web development model inspired by proven web-first workflows, while preserving control-level integration flexibility for custom architectures.

**Why now**: Phase 4 established shell capabilities, but becoming a default framework path for C# + web teams requires stronger typed capability workflows, policy governance, deterministic diagnostics, and template-driven delivery ergonomics.

### Milestones

| Milestone | Focus | Outcome |
|---|---|---|
| **M5.0 Objective Reset** | Lock acceptance criteria around framework positioning and dual-path adoption | ✅ Done — objective pivot locked to framework positioning outcomes |
| **M5.1 Typed Capability Gateway** | Unify host capability entry points and result semantics | ✅ Done — typed gateway with deterministic allow/deny/failure outcomes |
| **M5.2 Policy-first Runtime** | Enforce policy before provider execution | ✅ Done — zero-bypass, explicit deny reason, provider zero-execution deny path |
| **M5.3 Agent-friendly Observability** | Structured runtime diagnostics for critical flows | ✅ Done — structured diagnostics for outbound + inbound system-integration flows |
| **M5.4 Web-first Template Flow** | Template-level best-practice architecture path | ✅ Done — app-shell template demonstrates command + event roundtrip |
| **M5.5 Production Governance** | Release evidence tied to pain-point metrics | ✅ Done — CT/IT/automation/governance matrix and release evidence completed |

### Phase 5 Deliverables

| # | Deliverable | Depends On | Est. Complexity |
|---|---|---|---|
| 5.1 | ✅ Typed capability gateway consolidation | Phase 4 capability bridge | High |
| 5.2 | ✅ Policy-first deterministic execution contract | 5.1 + Phase 4 policy foundation | Medium |
| 5.3 | ✅ Machine-checkable diagnostics contract for critical paths | 5.1-5.2 | Medium |
| 5.4 | ✅ Template workflow for web-first desktop delivery | 5.1-5.3 + Phase 3 template base | Medium |
| 5.5 | ✅ Governance suite + release-readiness matrix aligned to pain-point KPIs | 5.1-5.4 | Medium |

### Latest Evidence Snapshot

- Release: `v0.1.15-preview` (pre-release)
- `nuke Test`: Unit `766`, Integration `149`, Total `915` (pass)
- `nuke Coverage`: Line `95.87%` (pass, threshold `90%`)
- OpenSpec archive evidence:
  - `2026-02-24-system-integration-contract-v2-freeze`
  - `2026-02-24-template-webfirst-dx-panel`
  - `2026-02-24-system-integration-diagnostic-export`

### Evidence Source Mapping

- Typed gateway/policy-first closeout: `openspec/changes/archive/2026-02-24-system-integration-contract-v2-freeze/verification-evidence.md`
- Web-first template DX closeout: `openspec/changes/archive/2026-02-24-template-webfirst-dx-panel/verification-evidence.md`
- Agent-friendly diagnostics export closeout: `openspec/changes/archive/2026-02-24-system-integration-diagnostic-export/verification-evidence.md`
- Validation command baseline: `nuke Test`, `nuke Coverage`, `openspec validate --all --strict`

### Phase 5 Exit Criteria

- Framework positioning KPIs are defined and verified by automated evidence.
- Capability calls are typed, policy-governed, and produce deterministic outcomes.
- Critical runtime flows emit structured diagnostics consumable by CI and AI agents.
- Default template demonstrates the recommended web-first framework architecture path.

### Phase Transition Status (Machine-checkable)

- Completed phase id: `phase8-bridge-v2-parity`
- Active phase id: `phase9-ga-release-readiness`
- Closeout snapshot artifact: `artifacts/test-results/closeout-snapshot.json`

## Phase 6: Governance Productization (✅ Completed)

**Goal**: Productize phase transition governance so release evidence and CI gates remain phase-neutral, semantic, and deterministic across future roadmap increments.

### Milestones

| Milestone | Focus | Outcome |
|---|---|---|
| **M6.1 Closeout Contract Neutralization** | Remove phase-number-coupled target/payload naming in CI evidence generation | `ReleaseCloseoutSnapshot` and `closeout-snapshot.json` become canonical closeout contract |
| **M6.2 Semantic Transition Invariants** | Govern roadmap/evidence transitions by invariant IDs instead of hardcoded phase literals | Machine-checkable transition diagnostics with stable invariant IDs |
| **M6.3 Continuous Transition Gate** | Keep `Ci`/`CiPublish` gate continuity while roadmap moves to next active phase | Deterministic enforcement of completed-phase + active-phase transition metadata |

### Latest Evidence Snapshot

- Release: `v0.1.16-preview` (pre-release)
- `nuke Test`: Unit `779`, Integration `151`, Total `930` (pass)
- `nuke Coverage`: Line `94.17%` (pass, threshold `90%`)
- OpenSpec archive evidence:
  - `2026-02-26-phase6-foundation-governance-hardening`
  - `2026-02-27-phase6-governance-productization`
  - `2026-02-27-phase6-continuous-transition-gate`

### Evidence Source Mapping

- Governance foundation hardening closeout: `openspec/changes/archive/2026-02-26-phase6-foundation-governance-hardening/verification-evidence.md`
- Phase 6 governance productization closeout: `openspec/changes/archive/2026-02-27-phase6-governance-productization/verification-evidence.md`
- Continuous transition gate closeout: `openspec/changes/archive/2026-02-27-phase6-continuous-transition-gate/verification-evidence.md`
- Validation command baseline: `nuke Test`, `nuke Coverage`, `openspec validate --all --strict`

### Phase 6 Exit Criteria

- Closeout snapshot contract and transition continuity checks stay lane-consistent across `Ci` and `CiPublish`.
- Roadmap transition markers are machine-checkable and aligned with governance assertions.
- Deterministic transition diagnostics are emitted for parity and continuity failures.

## Phase 7: Release Orchestration (✅ Completed)

**Goal**: Convert governance-complete framework foundations into release-orchestrated product readiness with deterministic publication quality gates and adoption-oriented evidence.

### Milestones

| Milestone | Focus | Outcome |
|---|---|---|
| **M7.1 Release Evidence Consolidation** | Unify release-readiness evidence into one deterministic contract for CI and package validation | ✅ Done — machine-checkable release decision baseline established |
| **M7.2 Packaging and Distribution Determinism** | Ensure NuGet/package metadata, compatibility, and changelog expectations are policy-governed | ✅ Done — deterministic release artifact quality gate enforced |
| **M7.3 Adoption Readiness Signals** | Align docs/templates/runtime evidence with framework adoption KPIs | ✅ Done — adoption readiness section and policy-tier findings integrated |

### OpenSpec archive evidence

- `2026-02-28-phase7-release-orchestration-foundation`
- `2026-02-28-phase7-packaging-distribution-determinism`
- `2026-02-28-phase7-adoption-readiness-signals`

### Phase 7 Exit Criteria

- Release decision state is machine-checkable from unified CI evidence contract v2 payload.
- Stable publish path is blocked deterministically on distribution/adoption/governance failures.
- Release orchestration diagnostics provide invariant-linked expected-vs-actual entries for CI triage.

## Phase 8: Bridge V2 & Platform Parity (✅ Completed)

**Goal**: Consolidate Bridge V2 expressiveness and platform feature parity into a deterministic baseline suitable for the next release train.

### Milestones

| Milestone | Focus | Outcome | Status |
|---|---|---|---|
| **M8.1 Bridge Diagnostic Safety Net** | Generator diagnostics and boundary guardrails | Deterministic diagnostics for unsupported patterns | ✅ Done |
| **M8.2 Bridge Cancellation Support** | CancellationToken to AbortSignal contract | Cross-boundary cancellation semantics | ✅ Done |
| **M8.3 Bridge AsyncEnumerable Streaming** | Stream transport and iterator contract | Deterministic pull-based streaming over RPC | ✅ Done |
| **M8.4 Bridge Generics & Overload Boundary** | Overload support and generic boundary clarity | Expanded expressiveness with explicit unsupported cases | ✅ Done |
| **M8.5 Bridge Binary Payload** | byte[] ↔ Uint8Array transport | Base64 round-trip with encode/decode helpers | ✅ Done |
| **M8.6 SPA Asset Hot Update** | Signed package install, activation, rollback | Production-ready SPA version management with signature verification | ✅ Done |
| **M8.7 Shell Activation Orchestration** | Single-instance ownership and forwarding | Primary/secondary activation coordination with deterministic dispatch | ✅ Done |
| **M8.8 Deep-link Native Registration** | OS-level URI scheme registration and ingestion | Policy-governed, idempotent activation pipeline from native entrypoint | ✅ Done |
| **M8.9 Platform Feature Parity** | Adapter feature gap closure and compatibility updates | Auditable cross-platform parity baseline | ✅ Done |

### Latest Evidence Snapshot

- `nuke Test`: Unit `1113`, Integration `180`, Total `1293` (pass)
- `nuke ReleaseOrchestrationGovernance`: all targets pass

### OpenSpec Archive Evidence

- `2026-02-28-bridge-diagnostics-safety-net` (M8.1)
- `2026-02-28-bridge-cancellation-token-support` (M8.2)
- `2026-02-28-bridge-async-enumerable-streaming` (M8.3)
- `2026-02-28-bridge-generics-overloads` (M8.4)
- `2026-03-01-phase9-functional-triple-track` (M8.5 binary payload, M8.6 SPA hot update, M8.7 activation orchestration)
- `2026-03-01-deep-link-native-registration` (M8.8)
- `2026-02-28-platform-feature-parity` (M8.9)
- `2026-02-28-phase7-closeout-phase8-reconciliation` (Phase 7→8 transition)

### Evidence Source Mapping

- Bridge V2 closeout: archived changes covering M8.1–M8.5
- Shell activation + deep-link closeout: archived changes covering M8.6–M8.8
- Platform parity closeout: `openspec/changes/archive/2026-02-28-platform-feature-parity/`
- Validation command baseline: `nuke Test`, `nuke Coverage`, `openspec validate --all --strict`

### Phase 8 Exit Criteria

- All Bridge V2 capabilities (cancellation, streaming, overloads, binary payload) have CT + IT coverage.
- SPA asset hot update with signature verification and rollback is production-ready.
- Shell activation orchestration and deep-link native registration are policy-governed.
- Platform feature parity gaps are documented in compatibility matrix with clear status markers.
- All M8.1–M8.9 milestones completed and archived.

---

## Phase 9: GA Release Readiness (🚧 Active)

**Goal**: Convert the fully-featured preview framework into a 1.0 stable release with frozen API surface, published npm package, updated performance baselines, and structured release artifacts.

### Milestones

| Milestone | Focus | Outcome | Status |
|---|---|---|---|
| **M9.1 Phase 8 Evidence Closeout** | Final closeout snapshot with Phase 8 evidence | Machine-checkable transition from Phase 8 → Phase 9 | ✅ Done |
| **M9.2 API Surface Freeze** | Breaking change audit and semver 1.0.0 commitment | Stable public API surface with no preview-breaking changes | ✅ Done |
| **M9.3 npm Bridge Publication** | `@agibuild/bridge` published to npm registry | Frontend developers can `npm install @agibuild/bridge` | ✅ Done |
| **M9.4 Performance Re-baseline** | Updated benchmarks after Phase 8 changes | Current bridge latency, SPA load, and memory baselines | ✅ Done |
| **M9.5 Changelog & Release Notes** | Structured changelog from v0.1.0-preview to v1.0.0 | Auditable release history for adopters | ✅ Done |
| **M9.6 Migration Guide** | Electron/Tauri → Fulora migration documentation | Actionable migration path for target adopters | ✅ Done |
| **M9.7 Stable Release Gate** | 1.0.0 stable NuGet + npm publish | Production-ready stable release | ✅ Done |

### Phase 9 Exit Criteria

- Package version has no preview suffix (1.0.0).
- `@agibuild/bridge` is published to npm with matching version.
- Structured changelog artifact exists and covers all phases.
- All governance targets pass with stable release configuration.
- Migration guide covers at least one alternative framework (Electron or Tauri).

---

## Dependencies & Prerequisites

```
Phase 0 (✅ Done) ──► Phase 1 (✅ Done) ──► Phase 2 (✅ Core Done) ──► Phase 3 (✅ Done) ──► Phase 4 (✅ Done) ──► Phase 5 (✅ Completed) ──► Phase 6 (✅ Completed) ──► Phase 7 (✅ Completed) ──► Phase 8 (✅ Completed) ──► Phase 9 (🚧 Active)
     │                      │                       │                         │                         │                                   │
     │                      │                       └── 2.4 depends on Phase 1│                         └── framework-positioning baseline    └── release orchestration builds on deterministic transition governance
     │                      └── Builds on F6 (RPC) + F3 (Policy)             └── Shell layer builds on stable GA baseline
     └── F4 (WebResource) used by Phase 2                                       and reuses bridge/policy/testability core
```

Phase 1 and Phase 2 are mostly independent in implementation but compose together. Phase 4 depends on completed GA-grade stability from Phase 3 and focuses on product-level shell capabilities. Phase 5 pivots from "host count" to "framework positioning + dual-path adoption" with web-first developer productivity and AI-agent operability as primary outcomes. Phase 6 productizes transition governance baselines, Phase 7 closes release orchestration governance, Phase 8 closes Bridge V2 expressiveness and platform parity consolidation, and Phase 9 targets 1.0 GA release readiness.

---

## Risk Register

| Risk | Impact | Mitigation |
|------|--------|------------|
| Source Generator complexity (Roslyn API) | Phase 1 delay | Start with simple cases (no generics, no overloads), iterate |
| TypeScript generation edge cases | Type mismatch bugs | Use System.Text.Json contract model as single source of truth |
| Platform WebView JS injection timing | Bridge not ready when page loads | Use preload scripts (F4) to ensure bridge is available at document-start |
| SPA routing conflicts with custom scheme | 404 on client routes | SPA fallback is a proven industry pattern — low risk |
| AOT/NativeAOT compatibility | Source generator must not use reflection | Design constraint from day 1 — source gen is inherently AOT-safe |
| Shell behavior divergence across platforms | Inconsistent user experience | Define shell semantics in contracts first; enforce via CT + platform IT |
| Host capability overexposure | Security/compliance risk | Keep capabilities opt-in, explicit allowlists, and policy-based authorization |
| Multi-window lifecycle complexity | Leaks/crashes under stress | Introduce stress/soak lanes and deterministic teardown assertions per window |

---

## References

- [Project Goals](./PROJECT.md) — Vision, competitive analysis, and goal definitions
- [Compatibility Matrix](../docs/agibuild_webview_compatibility_matrix_proposal.md) — Platform support
- [Design Document](../docs/agibuild_webview_design_doc.md) — Architecture and contracts
