# Interactive Playground — Spec

## Purpose

Define BDD-style requirements for the Fulora interactive playground: web-based editor for bridge interface definitions, live TypeScript generation, mock bridge call testing, and shareable URL state. Ensures new developers can experiment with Fulora bridge contracts without project setup.

## Requirements

### Requirement: Playground provides Monaco editor for C# interface definition

The playground SHALL provide a Monaco-based editor for C# bridge interface definitions with syntax highlighting and basic editing support.

#### Scenario: User edits C# interface in Monaco
- **GIVEN** the playground is loaded at docs.agibuild.dev/playground
- **WHEN** the user types or pastes C# interface code (e.g., `[JsExport] public interface IAppService { Task<string> GetData(); }`)
- **THEN** the editor SHALL display C# syntax highlighting
- **AND** the editor SHALL support standard editing (undo, redo, selection, copy/paste)

#### Scenario: Editor has sensible default content
- **GIVEN** the playground is loaded with no URL state
- **WHEN** the editor is displayed
- **THEN** it SHALL show a default C# interface example (e.g., a minimal `[JsExport]` interface)
- **AND** the example SHALL be valid and generate TypeScript when processed

### Requirement: Live preview of generated TypeScript

The playground SHALL display generated TypeScript types that correspond to the C# interface definition, updating as the user types (with debouncing).

#### Scenario: TypeScript preview updates on C# change
- **GIVEN** the playground with a valid C# `[JsExport]` interface
- **WHEN** the user modifies the C# interface (e.g., adds a method or parameter)
- **THEN** the TypeScript preview pane SHALL update to reflect the changes
- **AND** the update SHALL occur within a reasonable debounce delay (e.g., 500 ms)

#### Scenario: TypeScript preview shows service and method types
- **GIVEN** a C# interface `[JsExport] public interface IAppService { Task<User> GetUser(string id); }`
- **WHEN** the TypeScript is generated
- **THEN** the preview SHALL include a typed service proxy or method signatures
- **AND** the preview SHALL include types for `User` and parameters as applicable

#### Scenario: Invalid C# shows error state in preview
- **GIVEN** the user has entered invalid or incomplete C# (e.g., unclosed brace)
- **WHEN** generation is attempted
- **THEN** the preview SHALL display an error message or "approximate" output
- **AND** the user SHALL be informed that the output may be incomplete

### Requirement: Simulated bridge call testing with mock responses

The playground SHALL allow users to invoke bridge methods against a mock runtime and see mock responses.

#### Scenario: User can trigger a bridge call from the playground
- **GIVEN** a generated TypeScript service (e.g., `appService.getUser("1")`)
- **WHEN** the user clicks "Run" or equivalent to invoke the method
- **THEN** the mock bridge runtime SHALL execute the call
- **AND** the result (or error) SHALL be displayed in the playground

#### Scenario: Mock responses are configurable
- **GIVEN** the playground mock runtime
- **WHEN** the user configures a mock response for a method (e.g., `AppService.GetUser` → `{ id: "1", name: "Alice" }`)
- **THEN** subsequent invocations of that method SHALL return the configured mock
- **AND** the mock configuration SHALL be editable (e.g., JSON editor or form)

#### Scenario: Default mocks for common types
- **GIVEN** a method that returns `Task<string>` or `Task<int>`
- **WHEN** no custom mock is configured
- **THEN** the mock runtime SHALL return a sensible default (e.g., `""`, `0`, or `{}` for object)
- **AND** the user MAY override the default

### Requirement: Share playground state via URL

The playground SHALL encode its state (C# source, mock config, selected tab) into the URL so that the state can be shared.

#### Scenario: URL encodes playground state
- **GIVEN** the user has entered C# code and optionally mock responses
- **WHEN** the shareable URL is generated (e.g., via "Share" button or automatic update)
- **THEN** the URL SHALL contain an encoded representation of the state
- **AND** loading that URL SHALL restore the C# code and mock configuration

#### Scenario: Restored state matches original
- **GIVEN** a shareable URL that was generated from a playground session
- **WHEN** a user opens that URL in a new tab or shares it with another user
- **THEN** the playground SHALL load with the same C# interface and mock configuration
- **AND** the TypeScript preview SHALL reflect the restored C# content

#### Scenario: URL uses hash fragment for state
- **WHEN** state is encoded into the URL
- **THEN** the state SHALL be placed in the URL hash fragment (`#...`) to avoid server round-trips
- **AND** the path SHALL remain `/playground` (or equivalent)

### Requirement: Playground is hosted on docs site

The playground SHALL be accessible at docs.agibuild.dev/playground (or the configured docs base URL).

#### Scenario: Playground route is reachable
- **WHEN** a user navigates to docs.agibuild.dev/playground
- **THEN** the playground SHALL load and display the editor and preview
- **AND** the playground SHALL function without requiring a separate app deployment

#### Scenario: Playground integrates with docs deployment
- **WHEN** the docs site is built and deployed
- **THEN** the playground SHALL be included in the deployment
- **AND** the playground assets SHALL be served from the same origin as the docs

### Requirement: TypeScript generation supports server-side or client-side path

The playground SHALL support generating TypeScript via a server-side API when available, with a client-side fallback for offline or simplified output.

#### Scenario: Server-side generation when API is available
- **GIVEN** the playground is loaded and the generation API is reachable
- **WHEN** the user's C# content is submitted for generation
- **THEN** the playground SHALL call the API (e.g., POST with C# source)
- **AND** the API response SHALL be used to populate the TypeScript preview

#### Scenario: Client-side fallback when server unavailable
- **GIVEN** the generation API is unavailable (offline, error, or disabled)
- **WHEN** the user's C# content is submitted for generation
- **THEN** the playground SHALL use a client-side parser/emitter to produce approximate TypeScript
- **AND** the preview SHALL be labeled as "approximate" or similar when fallback is used
