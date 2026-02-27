## ADDED Requirements

### Requirement: Core runtime boundary SHALL be framework-neutral
`Agibuild.Fulora.Core`, `Agibuild.Fulora.Runtime`, and `Agibuild.Fulora.Adapters.Abstractions` SHALL NOT expose or reference host-framework-specific contract types (including Avalonia UI/platform types) in their public API.

#### Scenario: Core/runtime API surface is host-neutral
- **WHEN** public API signatures and package dependencies of `Core`, `Runtime`, and `Adapters.Abstractions` are inspected
- **THEN** no Avalonia namespaces or Avalonia package dependencies are present

### Requirement: Host-specific bindings SHALL live in host integration layer
Concrete host lifecycle bindings (UI-thread dispatcher implementation, visual host control, dialog host integration, app-builder bootstrapping) SHALL be implemented in a host-specific layer/package.

#### Scenario: Avalonia binding is isolated
- **WHEN** Avalonia-specific WebView control and dispatcher binding are resolved
- **THEN** they are provided from the Avalonia host integration layer rather than `Core`/`Runtime`
