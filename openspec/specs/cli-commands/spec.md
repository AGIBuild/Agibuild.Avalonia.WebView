## Purpose

Define requirements for the `agibuild` CLI tool distributed as a .NET global tool, providing commands for project scaffolding, TypeScript type generation, dev server orchestration, and bridge service scaffolding.

## Requirements

### Requirement: agibuild SHALL be installable as a .NET global tool

The CLI SHALL be packaged as a `DotnetToolManifest` NuGet package and SHALL be installable via `dotnet tool install -g Agibuild.Fulora.Cli`. The tool SHALL be invokable as `agibuild` after installation.

#### Scenario: Global tool installation succeeds
- **WHEN** a user runs `dotnet tool install -g Agibuild.Fulora.Cli`
- **THEN** the tool is installed and `agibuild --help` displays usage information

#### Scenario: agibuild command is available
- **WHEN** the tool is installed
- **THEN** running `agibuild` (with no arguments or `--help`) SHALL print available commands and options

### Requirement: agibuild new SHALL scaffold a new project

The `agibuild new <name> --frontend <react|vue|svelte>` command SHALL create a new Agibuild.Fulora hybrid project by delegating to `dotnet new agibuild-hybrid` with the appropriate parameters.

#### Scenario: New project with React frontend
- **WHEN** a user runs `agibuild new MyApp --frontend react`
- **THEN** a new project directory `MyApp` is created with Desktop, Bridge, and web projects
- **AND** the web project contains a React Vite scaffold

#### Scenario: New project with Vue frontend
- **WHEN** a user runs `agibuild new MyApp --frontend vue`
- **THEN** a new project directory `MyApp` is created
- **AND** the web project contains a Vue Vite scaffold

#### Scenario: Frontend option is required
- **WHEN** a user runs `agibuild new MyApp` without `--frontend`
- **THEN** the command SHALL fail with a clear error or SHALL use a documented default

### Requirement: agibuild generate types SHALL emit TypeScript declarations from C# bridge interfaces

The `agibuild generate types` command SHALL build the Bridge project (or solution) and SHALL extract or invoke TypeScript declaration generation from C# assemblies with `[JsExport]` and `[JsImport]` interfaces, writing output to the web project's types directory.

#### Scenario: Type generation from Bridge project
- **WHEN** a user runs `agibuild generate types` from a solution directory containing a Bridge project
- **THEN** the Bridge project is built
- **AND** TypeScript `.d.ts` declarations are generated and written to the web project's expected location

#### Scenario: Generated types match bridge interfaces
- **WHEN** TypeScript generation completes successfully
- **THEN** the generated declarations SHALL include interfaces for all `[JsExport]` and `[JsImport]` services in the Bridge assembly
- **AND** the output SHALL be consumable by the web project's TypeScript build

### Requirement: agibuild dev SHALL start Vite and Avalonia together

The `agibuild dev` command SHALL start the Vite dev server and the Avalonia desktop application in parallel, enabling simultaneous web and desktop development.

#### Scenario: Dev server starts both processes
- **WHEN** a user runs `agibuild dev` from a solution directory
- **THEN** the Vite dev server is started (e.g. `npm run dev` or `npx vite` in the web project)
- **AND** the Avalonia desktop app is started (e.g. `dotnet run` for the Desktop project)

#### Scenario: Graceful shutdown
- **WHEN** the user interrupts `agibuild dev` (e.g. Ctrl+C)
- **THEN** both the Vite process and the Avalonia process SHALL be terminated gracefully

### Requirement: agibuild add service SHALL scaffold a new bridge service

The `agibuild add service <name>` command SHALL generate a C# bridge interface, C# implementation, and TypeScript proxy for a new bridge service.

#### Scenario: Service scaffold creates expected files
- **WHEN** a user runs `agibuild add service MyService` from a solution directory
- **THEN** a C# interface with `[JsExport]` or `[JsImport]` is created in the Bridge project
- **AND** a C# implementation class is created
- **AND** a TypeScript proxy or stub is created in the web project

#### Scenario: Service name drives file and type names
- **WHEN** a user runs `agibuild add service UserPreferences`
- **THEN** generated types and files SHALL use consistent naming (e.g. `IUserPreferences`, `UserPreferencesService`, `userPreferences` in TS)
