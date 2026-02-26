# Project Template — Design

**ROADMAP**: Phase 3.1

## Location

Template at `templates/agibuild-hybrid/`.

## template.json

- Identity: `Agibuild.Fulora.HybridTemplate`
- Short name: `agibuild-hybrid`
- Symbol `framework`: choice parameter (vanilla, react, vue)
- Conditional sources: vanilla excludes HybridApp.Web.Vite; react/vue exclude HybridApp.Web.Vanilla (when present)

## Structure

- **HybridApp.Desktop**: Avalonia desktop host with WebView, MainWindow, wwwroot/index.html
- **HybridApp.Bridge**: IGreeterService, GreeterServiceImpl, bridge contract demo
- **HybridApp.Tests**: Unit tests with MockBridgeService for GreeterService

## Content

- GreeterService demo for bridge usage
- SPA hosting setup in Desktop project
- wwwroot/index.html with bridge client usage
