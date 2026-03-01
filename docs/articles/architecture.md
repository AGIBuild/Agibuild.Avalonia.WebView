# Architecture

## Architectural North Star

Architecture is aligned to **Roadmap Phase 5: Framework Positioning Foundation**.
The goal is not only rendering web content, but delivering a deterministic, policy-governed hybrid app platform that works for both framework-first delivery and control-level integration.

## System Topology

```
┌──────────────────────────────────────────────────────────┐
│                   Consumer Application                  │
│      Avalonia UI + Web UI + Bridge Contracts           │
└──────────────────────────────────────────────────────────┘
                            │
                            ▼
┌──────────────────────────────────────────────────────────┐
│                         Runtime Core                    │
│  Typed Bridge · Capability Gateway · Policy Engine      │
│  Diagnostics Pipeline · Shell Experience · SPA Hosting  │
└──────────────────────────────────────────────────────────┘
                            │
                            ▼
┌──────────────────────────────────────────────────────────┐
│                   Adapter Abstraction Layer             │
│                    IWebViewAdapter + Facets            │
└──────────────────────────────────────────────────────────┘
                            │
                            ▼
┌──────────────────────────────────────────────────────────┐
│   WebView2 (Win) · WKWebView (macOS/iOS) · Android     │
│                     WebKitGTK (Linux)                   │
└──────────────────────────────────────────────────────────┘
```

## Core Invariants

1. **Contract-first**
   - Public behavior starts from explicit contracts.
   - Runtime is the single semantic owner for contract execution.

2. **Single typed capability gateway**
   - Desktop/system operations converge through one typed capability entry model.
   - App-layer code avoids scattered direct host API invocation paths.

3. **Policy-first deterministic execution**
   - Policy evaluation happens before provider execution.
   - Every capability request resolves to `allow`, `deny`, or `failure`.

4. **Automation-first diagnostics**
   - Critical runtime paths emit machine-checkable diagnostics.
   - CI and AI agents can assert behavior without manual log reading.

5. **Web-first template architecture**
   - Starter projects keep host glue minimal.
   - Typed bridge and capability contracts remain first-class from day one.

## Bridge Model (C# <-> JS)

Bridge communication is centered on typed contracts:

- `[JsExport]`: expose C# services to JavaScript
- `[JsImport]`: consume JavaScript services from C#
- Source generation enforces AOT-safe, reflection-free stubs/proxies
- JSON-RPC transport stays internal; contract surface stays typed

## Capability Execution Semantics

Capability calls follow the same runtime sequence:

1. request enters typed capability gateway
2. policy engine evaluates authorization/governance
3. provider executes only when policy permits
4. deterministic result is returned
5. diagnostics event is emitted for automation

| Outcome | Meaning | Expected behavior |
|---|---|---|
| `allow` | Policy approved and operation completed | Return success result + diagnostics |
| `deny` | Policy rejected before execution | Return explicit deny result + diagnostics |
| `failure` | Execution attempted but failed deterministically | Return failure result + diagnostics |

## Shell Activation & Deep-link Architecture (Phase 8)

Shell activation orchestration and deep-link registration extend the runtime with OS-level app lifecycle management:

```
OS Protocol Handler → DeepLinkPlatformEntrypoint
    → DeepLinkRegistrationService (normalize → policy → idempotency)
        → WebViewShellActivationCoordinator (primary/secondary dispatch)
            → Application handler
```

Key architectural properties:
- **Single-instance ownership**: `WebViewShellActivationCoordinator` manages primary/secondary instance registration; secondary instances forward activation to the primary
- **Policy-first admission**: deep-link activations are evaluated against `IDeepLinkAdmissionPolicy` before dispatch
- **Idempotent delivery**: duplicate activations within a configurable replay window are suppressed using deterministic idempotency keys
- **Canonical envelope**: raw platform URIs are normalized into `DeepLinkActivationEnvelope` with scheme/host/path canonicalization
- **Structured diagnostics**: each lifecycle stage emits `DeepLinkDiagnosticEventArgs` with correlation ID, event type, and outcome

## Security and Governance Layers

- **WebMessage policy**: origin, channel, and protocol boundaries
- **Capability policy**: explicit policy evaluation before host provider execution
- **Rate limiting**: bounded request pressure on bridge/capability paths
- **Explicit exposure**: only declared contracts are reachable from web content

## Testability and Verification

- Contract tests validate behavior semantics independent of platform engine
- Integration tests validate runtime wiring on real platform adapters
- Automation lanes validate diagnostics/governance expectations for release readiness

## Related Documents

- [Roadmap](../../openspec/ROADMAP.md)
- [Project Vision & Goals](../../openspec/PROJECT.md)
- [Getting Started](./getting-started.md)
