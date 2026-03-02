## Purpose

Define the MAUI integration layer for Agibuild.Fulora, allowing Fulora WebView to be used in .NET MAUI apps via an adapter pattern consistent with the Avalonia host.

## ADDED Requirements

### Requirement: MAUI host adapter package
Repository SHALL provide a package (e.g., `Agibuild.Fulora.Maui`) that adapts Fulora's WebView hosting to MAUI's `WebView` control.

#### Scenario: Package references core Fulora and MAUI
- **WHEN** a developer adds the MAUI Fulora package to a MAUI project
- **THEN** the package SHALL bring in `Agibuild.Fulora` core and `Microsoft.Maui` dependencies
- **AND** the project SHALL build for supported MAUI targets (e.g., net8.0-android, net8.0-ios, net8.0-windows10.0.19041)

#### Scenario: Adapter implements host-neutral contracts
- **WHEN** the MAUI adapter is inspected
- **THEN** it SHALL implement the same host-neutral contracts (e.g., `IWebViewHost` or equivalent) as the Avalonia adapter

### Requirement: Bridge invocation from MAUI WebView
The MAUI adapter SHALL enable typed bridge invocation (C# ↔ JS) through the MAUI WebView.

#### Scenario: JS can invoke C# bridge services
- **WHEN** a MAUI app hosts a WebView with Fulora and loads content that calls a bridge service
- **THEN** the `[JsExport]` service SHALL be invoked and SHALL return results to JS

#### Scenario: C# can invoke JS bridge services
- **WHEN** C# code calls an `[JsImport]` service method
- **THEN** the MAUI WebView SHALL execute the corresponding JS and SHALL return the result to C#

### Requirement: SPA hosting in MAUI
The MAUI adapter SHALL support SPA hosting: loading content from embedded resources or a dev server URL.

#### Scenario: Production mode serves embedded resources
- **WHEN** the MAUI app is configured for production SPA hosting
- **THEN** the WebView SHALL load content from embedded resources (e.g., `app://localhost/` or platform-equivalent)

#### Scenario: Dev mode proxies to dev server
- **WHEN** the MAUI app is configured with a dev server URL (e.g., `http://localhost:5173`)
- **THEN** the WebView SHALL load content from that URL (subject to platform capabilities)

### Requirement: MAUI sample application
Repository SHALL provide a MAUI sample demonstrating Fulora integration.

#### Scenario: MAUI sample builds and runs
- **WHEN** a developer builds and runs the MAUI sample
- **THEN** the app SHALL launch, display a WebView with hosted content, and at least one bridge call SHALL succeed

#### Scenario: MAUI sample demonstrates bridge and SPA hosting
- **WHEN** contributors inspect the MAUI sample
- **THEN** it SHALL demonstrate typed bridge usage and SPA hosting configuration

### Requirement: Documentation
Documentation SHALL describe how to add Fulora to a MAUI project and configure bridge and SPA hosting.

#### Scenario: Setup steps are documented
- **WHEN** a developer consults the MAUI integration documentation
- **THEN** they SHALL find steps to add the package, configure the WebView, and register bridge services
