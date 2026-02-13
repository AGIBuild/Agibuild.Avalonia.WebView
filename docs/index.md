# Agibuild.Avalonia.WebView

Cross-platform hybrid app framework — Electron's developer productivity with native performance.

## Quick Links

- [Demo: Avalonia + React Hybrid App](demo/index.md) — See the framework in action with screenshots
- [Getting Started](articles/getting-started.md)
- [Architecture](articles/architecture.md)
- [API Reference](api/index.md)

## Features

- **Type-Safe Bridge**: `[JsExport]` / `[JsImport]` attributes with Roslyn Source Generator for AOT-compatible C# ↔ JS interop
- **SPA Hosting**: Embedded resource serving with custom `app://` scheme, SPA router fallback, dev server proxy
- **Cross-Platform**: Windows (WebView2), macOS/iOS (WKWebView), Android (WebView), Linux (WebKitGTK)
- **Testable**: `MockBridgeService` for unit testing without a real browser
- **Secure**: Origin-based policy, rate limiting, protocol versioning
