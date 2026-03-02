## 1. Core Adapter Implementation

- [x] 1.1 Create `Agibuild.Fulora.Maui` package project with multi-target (net8.0-android, net8.0-ios, net8.0-maccatalyst, net8.0-windows10.0.19041)
- [x] 1.2 Define or identify host-neutral contracts (IWebViewHost, IWebViewLifecycle, etc.) in core
- [x] 1.3 Implement MAUI adapter wrapping `Microsoft.Maui.Controls.WebView` and bridging to `EvaluateJavaScriptAsync` / navigation events
- [x] 1.4 Wire adapter to Fulora runtime (bridge registration, SPA hosting)

## 2. Bridge and SPA Hosting

- [x] 2.1 Enable bridge invocation (JS→C# and C#→JS) through MAUI WebView
- [x] 2.2 Implement SPA hosting: embedded resources and dev server URL configuration
- [x] 2.3 Handle platform-specific WebView behavior (Android, iOS, Windows, macOS)

## 3. Sample and Documentation

- [x] 3.1 Create MAUI sample app demonstrating bridge and SPA hosting
- [x] 3.2 Add MAUI setup and usage documentation
- [x] 3.3 Document platform support matrix and known limitations
