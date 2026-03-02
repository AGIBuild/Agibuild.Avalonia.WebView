## Purpose

Define Svelte and Angular sample application requirements for Agibuild.Fulora, demonstrating framework versatility with parity to existing React and Vue samples.

## ADDED Requirements

### Requirement: Svelte sample project structure
Repository SHALL provide a Svelte sample at `samples/avalonia-svelte/` with four projects: Desktop (Avalonia host), Bridge (shared interfaces/implementations), Web (Svelte + Vite + TypeScript), and Tests (xUnit).

#### Scenario: Svelte solution builds successfully
- **WHEN** a developer runs `dotnet build` from `samples/avalonia-svelte/`
- **THEN** all C# projects (Desktop, Bridge, Tests) SHALL compile without errors

#### Scenario: Svelte web app builds successfully
- **WHEN** a developer runs `npm install && npm run build` from the Svelte Web project
- **THEN** the build SHALL produce output in `dist/` with `index.html` and bundled assets

### Requirement: Angular sample project structure
Repository SHALL provide an Angular sample at `samples/avalonia-angular/` with four projects: Desktop (Avalonia host), Bridge (shared interfaces/implementations), Web (Angular + TypeScript), and Tests (xUnit).

#### Scenario: Angular solution builds successfully
- **WHEN** a developer runs `dotnet build` from `samples/avalonia-angular/`
- **THEN** all C# projects (Desktop, Bridge, Tests) SHALL compile without errors

#### Scenario: Angular web app builds successfully
- **WHEN** a developer runs `npm install && npm run build` from the Angular Web project
- **THEN** the build SHALL produce output in `dist/` with `index.html` and bundled assets

### Requirement: Svelte sample uses typed bridge client
Svelte sample SHALL consume `@agibuild/bridge` typed service client and demonstrate at least one typed C# bridge service call end-to-end.

#### Scenario: Svelte sample resolves generated bridge types
- **WHEN** the Svelte TypeScript build runs
- **THEN** it SHALL resolve generated `bridge.d.ts` and compile without bridge typing errors

#### Scenario: Svelte sample demonstrates typed bridge roundtrip
- **WHEN** contributors inspect Svelte sample app code
- **THEN** at least one typed C# bridge service call SHALL be demonstrated end-to-end

### Requirement: Angular sample uses typed bridge client
Angular sample SHALL consume `@agibuild/bridge` typed service client and demonstrate at least one typed C# bridge service call end-to-end.

#### Scenario: Angular sample resolves generated bridge types
- **WHEN** the Angular TypeScript build runs
- **THEN** it SHALL resolve generated `bridge.d.ts` and compile without bridge typing errors

#### Scenario: Angular sample demonstrates typed bridge roundtrip
- **WHEN** contributors inspect Angular sample app code
- **THEN** at least one typed C# bridge service call SHALL be demonstrated end-to-end

### Requirement: SPA hosting with dev and production modes
Both Svelte and Angular samples SHALL support dev mode (Vite/ng serve proxy) and production mode (embedded resources).

#### Scenario: Dev mode with live reload
- **WHEN** the Desktop app is launched in Debug configuration
- **THEN** the WebView SHALL proxy requests to the dev server and live reload SHALL work

#### Scenario: Production mode with embedded resources
- **WHEN** the Desktop app is built in Release configuration
- **THEN** the frontend build output SHALL be embedded as resources and served via `app://localhost/`

### Requirement: Unit tests for Bridge services
Both samples SHALL have unit tests for Bridge service implementations using `MockBridgeService` or equivalent.

#### Scenario: Svelte sample tests pass
- **WHEN** a developer runs `dotnet test` on the Svelte sample Tests project
- **THEN** all tests SHALL pass

#### Scenario: Angular sample tests pass
- **WHEN** a developer runs `dotnet test` on the Angular sample Tests project
- **THEN** all tests SHALL pass
