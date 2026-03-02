## Purpose

Define requirements for the bridge plugin packaging convention using IBridgePlugin and NuGet/npm dual distribution.

## Requirements

### Requirement: Bridge plugins are defined via IBridgePlugin marker interface

The system SHALL provide an `IBridgePlugin` interface with a static abstract method for declaring plugin services, enabling NativeAOT-safe plugin discovery.

#### Scenario: Plugin class declares services via GetServices

- **WHEN** a class implements `IBridgePlugin` and provides `static IEnumerable<BridgePluginServiceDescriptor> GetServices()`
- **THEN** the plugin manifest SHALL return one or more service descriptors containing interface type, factory, and optional bridge options

#### Scenario: Plugin without GetServices implementation fails at compile time

- **WHEN** a class implements `IBridgePlugin` but does not implement `GetServices()`
- **THEN** the compiler SHALL report an error (static abstract member not implemented)

### Requirement: UsePlugin registers all plugin services in one call

The `IBridgeService` SHALL expose a `UsePlugin<TPlugin>()` extension method that registers all services declared by the plugin.

#### Scenario: UsePlugin registers all JsExport services from plugin

- **WHEN** `Bridge.UsePlugin<MyPlugin>()` is called with a plugin declaring 3 services
- **THEN** all 3 services SHALL be registered via `Bridge.Expose<T>()` and accessible from JS

#### Scenario: UsePlugin with service provider passes provider to factories

- **WHEN** `Bridge.UsePlugin<MyPlugin>(serviceProvider)` is called
- **THEN** each service factory SHALL receive the service provider for dependency resolution

#### Scenario: UsePlugin applies plugin-declared BridgeOptions

- **WHEN** a plugin service descriptor includes `BridgeOptions` with `AllowedOrigins`
- **THEN** the service SHALL be registered with those options applied

### Requirement: Plugin service descriptors support factory-based construction

The `BridgePluginServiceDescriptor` SHALL contain a factory function for creating service implementations, supporting both simple and DI-backed construction.

#### Scenario: Simple factory creates service without DI

- **WHEN** a plugin descriptor has factory `_ => new MyService()`
- **THEN** `UsePlugin` SHALL create the service via that factory with null service provider

#### Scenario: DI-aware factory resolves dependencies

- **WHEN** a plugin descriptor has factory `sp => new MyService(sp.GetRequiredService<ILogger>())`
- **AND** `UsePlugin` is called with a valid service provider
- **THEN** the service SHALL be created with resolved dependencies

### Requirement: npm companion package provides TypeScript types for plugin services

Each bridge plugin SHALL have a corresponding npm package containing pre-generated TypeScript declarations for the plugin's bridge service interfaces and DTOs.

#### Scenario: npm package exports typed service interfaces

- **WHEN** a developer installs `@agibuild/bridge-plugin-local-storage`
- **THEN** the package SHALL export TypeScript interfaces for `ILocalStorageService` methods and parameter/return types

#### Scenario: npm package provides getService helper

- **WHEN** a developer imports the plugin npm package
- **THEN** the package SHALL provide a typed `getLocalStorageService()` helper that calls `bridgeClient.getService<ILocalStorageService>("LocalStorageService")`

### Requirement: Reference plugin LocalStorage demonstrates the full plugin pattern

The system SHALL include a reference plugin (`Agibuild.Fulora.Plugin.LocalStorage`) that demonstrates the complete plugin authoring pattern.

#### Scenario: LocalStorage plugin exposes CRUD operations

- **WHEN** the LocalStorage plugin is registered via `UsePlugin`
- **THEN** JS SHALL be able to call `get(key)`, `set(key, value)`, `remove(key)`, `clear()`, and `getKeys()` through the bridge

#### Scenario: LocalStorage plugin data persists across app restarts

- **WHEN** JS calls `set("theme", "dark")` via the LocalStorage plugin and the app restarts
- **THEN** `get("theme")` SHALL return `"dark"` after restart

#### Scenario: LocalStorage plugin has companion npm package

- **WHEN** the reference plugin is published
- **THEN** `@agibuild/bridge-plugin-local-storage` SHALL be available on npm with TypeScript declarations matching the C# interface
