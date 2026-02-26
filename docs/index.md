# Fulora

Web-first product velocity with Avalonia-native performance, control, and security.

## Documentation Hub

This project targets a **framework-grade C# + web development model** while preserving **standalone WebView control integration flexibility**.
One runtime core supports both paths.

Use this page as the entry point based on what you want to do next.

## Start Here

- **Build your first app**: [Getting Started](articles/getting-started.md)
- **Understand architecture decisions**: [Architecture](articles/architecture.md)
- **See a real sample experience**: [Demo: Avalonia + React Hybrid App](demo/index.md)
- **Check product direction and phases**: [Roadmap](../openspec/ROADMAP.md)
- **Review goals and positioning**: [Project Vision & Goals](../openspec/PROJECT.md)

## Features

- **Type-Safe Bridge**: `[JsExport]` / `[JsImport]` attributes with Roslyn Source Generator for AOT-compatible C# ↔ JS interop
- **SPA Hosting**: Embedded resource serving with custom `app://` scheme, SPA router fallback, dev server proxy
- **Cross-Platform**: Windows (WebView2), macOS/iOS (WKWebView), Android (WebView), Linux (WebKitGTK)
- **Testable**: `MockBridgeService` for unit testing without a real browser
- **Secure**: Origin-based policy, rate limiting, protocol versioning

## Current Product Objective

Current roadmap focus is **Phase 5: Framework Positioning Foundation**:

- Turn C# + web into a default-ready product architecture, not just a rendering choice
- Consolidate host/system operations behind one typed capability gateway
- Enforce policy-first execution with deterministic `allow/deny/failure` semantics
- Emit machine-checkable diagnostics for CI and AI-agent automation
- Provide a web-first template flow while retaining control-level integration freedom

## Roadmap Snapshot

| Phase | Focus | Status |
|---|---|---|
| Phase 0 | Foundation | ✅ Done |
| Phase 1 | Type-Safe Bridge | ✅ Done |
| Phase 2 | SPA Hosting | ✅ Core Done |
| Phase 3 | Polish & GA | ✅ Done |
| Phase 4 | Application Shell | ✅ Done |
| Phase 5 | Framework Positioning Foundation | ✅ Completed |
