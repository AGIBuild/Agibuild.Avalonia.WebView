# Project Template Spec

## Overview
`dotnet new agibuild-hybrid` template for scaffolding Avalonia + WebView hybrid apps.

## Requirements

### PT-1: template.json structure
- Identity: Agibuild.Avalonia.WebView.HybridTemplate
- Short name: agibuild-hybrid
- Classifications: Desktop, Mobile, Hybrid, Avalonia, WebView
- PreferNameDirectory: true

### PT-2: Desktop/Bridge/Tests scaffolding
- Desktop project: Avalonia host with WebView, MainWindow, wwwroot
- Bridge project: Bridge interfaces and implementations (e.g. GreeterService)
- Tests project: Unit tests with MockBridgeService

### PT-3: framework parameter
- Choice parameter: vanilla, react, vue
- Conditional sources based on framework selection
