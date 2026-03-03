# Showcase Todo App — Spec

## Purpose

Define BDD-style requirements for the Fulora showcase Todo app: a full-featured reference application (Avalonia + React) demonstrating all official Fulora plugins working together. Covers CRUD todos via bridge, SQLite persistence, offline-first data flow, system notifications for reminders, theme sync, global shortcuts, auth token for API sync, and project structure following template convention.

## Requirements

### Requirement: Todo CRUD operations via bridge

The SPA SHALL perform todo create, read, update, delete, list, and filter operations exclusively through bridge calls to `ITodoService`.

#### Scenario: Create todo via bridge
- **GIVEN** the SPA is loaded and bridge is ready
- **WHEN** the user submits a new todo (title, optional due date, optional reminder)
- **THEN** the SPA SHALL call `todoService.addTodo({ title, dueDate?, reminderAt? })`
- **AND** the host SHALL persist the todo and return the created todo with id
- **AND** the SPA SHALL display the new todo in the list

#### Scenario: Read and list todos via bridge
- **GIVEN** the SPA is loaded
- **WHEN** the user navigates to the todo list or refreshes
- **THEN** the SPA SHALL call `todoService.listTodos(filter?)`
- **AND** the host SHALL return todos from the local database
- **AND** the SPA SHALL render the list

#### Scenario: Update todo via bridge
- **GIVEN** a todo exists in the list
- **WHEN** the user marks it complete or edits title/due date
- **THEN** the SPA SHALL call `todoService.updateTodo(id, { completed?, title?, dueDate?, reminderAt? })`
- **AND** the host SHALL persist the change
- **AND** the SPA SHALL reflect the update in the UI

#### Scenario: Delete todo via bridge
- **GIVEN** a todo exists in the list
- **WHEN** the user deletes it
- **THEN** the SPA SHALL call `todoService.deleteTodo(id)`
- **AND** the host SHALL remove it from the database
- **AND** the SPA SHALL remove it from the list

#### Scenario: Filter todos via bridge
- **GIVEN** todos exist with various completion states
- **WHEN** the user selects a filter (all, active, completed)
- **THEN** the SPA SHALL call `todoService.listTodos({ filter: 'all' | 'active' | 'completed' })`
- **AND** the host SHALL return only matching todos
- **AND** the SPA SHALL display the filtered list

---

### Requirement: Database plugin integration for SQLite persistence

The host SHALL use the Database plugin for SQLite persistence. Todos SHALL be stored in a local SQLite database with schema migrations.

#### Scenario: Todos persist in SQLite
- **GIVEN** the host has registered the Database plugin
- **WHEN** the SPA creates, updates, or deletes todos via `ITodoService`
- **THEN** the host `TodoService` SHALL use `IDatabaseService` to execute SQL
- **AND** data SHALL be stored in the configured SQLite database file
- **AND** data SHALL survive app restart

#### Scenario: Schema migration runs on first use
- **GIVEN** the app is launched for the first time (or after schema change)
- **WHEN** the database is opened
- **THEN** the host SHALL apply migrations in order (e.g., `001_create_todos.sql`)
- **AND** the `todos` table SHALL exist with columns: id, title, completed, due_date, reminder_at, created_at, updated_at
- **AND** subsequent queries SHALL succeed

#### Scenario: Schema version is tracked
- **GIVEN** migrations have been applied
- **WHEN** the app is launched again
- **THEN** only pending migrations SHALL run
- **AND** the `schema_version` table SHALL record applied versions

---

### Requirement: Offline-first data flow

The app SHALL operate in an offline-first manner: local database is the source of truth; all CRUD works without network. Sync to a (mock) API when online is additive.

#### Scenario: CRUD works offline
- **GIVEN** the app has no network connectivity
- **WHEN** the user creates, reads, updates, or deletes todos
- **THEN** all operations SHALL complete successfully via local database
- **AND** the SPA SHALL NOT require network for core CRUD
- **AND** errors SHALL NOT be caused by network unavailability for local operations

#### Scenario: Sync when online (mock or no-op)
- **GIVEN** the app is online and a sync mechanism is configured
- **WHEN** sync is triggered (e.g., on app focus or manual button)
- **THEN** the host MAY attempt to sync with a (mock) API
- **AND** for showcase, sync MAY be a no-op or simulate with a delay
- **AND** local data SHALL remain the source of truth

#### Scenario: Local-first UI feedback
- **GIVEN** the user performs an action (add, update, delete)
- **WHEN** the bridge call completes
- **THEN** the SPA SHALL update the UI immediately from the returned data
- **AND** SHALL NOT wait for a network round-trip for local persistence

---

### Requirement: System notifications for reminders

Todos with reminders SHALL trigger system notifications when due. The host SHALL use the Notifications plugin.

#### Scenario: Reminder triggers notification
- **GIVEN** a todo has a `reminderAt` in the past or at current time
- **WHEN** the host's reminder check runs (timer or background task)
- **THEN** the host SHALL call `INotificationService.show({ title, body, tag? })`
- **AND** a system notification SHALL appear
- **AND** the notification SHALL display the todo title and optional details

#### Scenario: Notification click focuses app
- **GIVEN** a reminder notification was shown
- **WHEN** the user clicks the notification
- **THEN** the host SHALL bring the app window to the foreground
- **AND** MAY invoke a `[JsImport]` callback to navigate to the todo in the SPA

#### Scenario: Permission is requested when needed
- **GIVEN** the Notifications plugin requires permission
- **WHEN** the app first attempts to show a notification
- **THEN** the host SHALL request permission via the plugin if not yet granted
- **AND** SHALL handle permission denial gracefully (e.g., log, skip notification)

---

### Requirement: Theme sync between Avalonia host and React SPA

Theme (light/dark) SHALL sync bidirectionally between the host and the web. The SPA SHALL reflect the host theme and MAY request a theme change.

#### Scenario: SPA reads current theme
- **GIVEN** the SPA is loaded and bridge is ready
- **WHEN** the SPA needs to apply theme (e.g., on mount)
- **THEN** it SHALL call `themeService.getTheme()`
- **AND** the host SHALL return the current theme (`'light' | 'dark'`)
- **AND** the SPA SHALL apply the theme via CSS variables or class (e.g., `data-theme="dark"`)

#### Scenario: SPA requests theme change
- **GIVEN** the user toggles theme in the SPA (e.g., via a switch)
- **WHEN** the SPA calls `themeService.setTheme('dark')`
- **THEN** the host SHALL update the host theme (Avalonia)
- **AND** SHALL invoke `[JsImport] IThemeCallback.onThemeChanged('dark')` to notify the SPA
- **AND** the SPA SHALL apply the new theme

#### Scenario: Host-initiated theme change notifies SPA
- **GIVEN** the host theme changes (e.g., user changes system theme or host setting)
- **WHEN** the host detects the change
- **THEN** the host SHALL call `IThemeCallback.onThemeChanged(newTheme)` if the callback is registered
- **AND** the SPA SHALL update its theme to match

---

### Requirement: Global shortcuts

The host SHALL register global shortcuts. When triggered, the host SHALL invoke `[JsImport]` callbacks to the SPA.

#### Scenario: Ctrl+Shift+N (or equivalent) adds new todo
- **GIVEN** the host has registered the global shortcut for "add todo"
- **WHEN** the user presses the shortcut (e.g., Ctrl+Shift+N)
- **THEN** the host SHALL invoke `IShortcutCallback.onAddTodo()` (or equivalent)
- **AND** the SPA SHALL handle the callback and show the add-todo UI or focus the input

#### Scenario: Ctrl+Shift+F (or equivalent) focuses search
- **GIVEN** the host has registered the global shortcut for "focus search"
- **WHEN** the user presses the shortcut
- **THEN** the host SHALL invoke `IShortcutCallback.onFocusSearch()`
- **AND** the SPA SHALL focus the search/filter input

#### Scenario: Shortcuts work when app is in background
- **GIVEN** the app is running but not focused
- **WHEN** the user presses a registered global shortcut
- **THEN** the host SHALL receive the shortcut
- **AND** SHALL bring the app to foreground and invoke the callback
- **AND** the SPA SHALL respond to the callback

---

### Requirement: Auth token for API sync

The host SHALL use the Auth Token plugin to provide a token for API sync. The SPA SHALL be able to request the token when syncing.

#### Scenario: Host provides auth token via plugin
- **GIVEN** the Auth Token plugin is registered
- **WHEN** the SPA or host needs to sync with an API
- **THEN** the host SHALL obtain the token via the Auth Token plugin (e.g., `IAuthTokenService.getToken()`)
- **AND** for showcase, the token MAY be a mock value
- **AND** the token SHALL be passed to the HTTP client when making sync requests

#### Scenario: Token is not exposed to SPA for security
- **GIVEN** the Auth Token plugin stores a token
- **WHEN** the SPA needs to perform sync
- **THEN** the SPA SHALL call a host method (e.g., `todoService.sync()`) that performs the sync server-side
- **AND** the SPA SHALL NOT receive the raw token
- **AND** token handling SHALL remain in the host

---

### Requirement: Project structure follows template convention

The showcase SHALL follow the template structure: Desktop (Avalonia host), Bridge (shared interfaces), Web (React SPA), Tests (xUnit). The host SHALL reference all required plugins.

#### Scenario: Solution has expected projects
- **GIVEN** the showcase is located at `samples/showcase-todo/`
- **WHEN** the solution is inspected
- **THEN** it SHALL contain: `ShowcaseTodo.Desktop`, `ShowcaseTodo.Bridge`, `ShowcaseTodo.Web`, `ShowcaseTodo.Tests`
- **AND** the Desktop project SHALL be the Avalonia host with WebView
- **AND** the Web project SHALL be a React (Vite + TypeScript) SPA
- **AND** the Bridge project SHALL contain `[JsExport]` and `[JsImport]` interfaces

#### Scenario: Host registers all required plugins
- **GIVEN** the Desktop project
- **WHEN** the host is configured
- **THEN** it SHALL register: `DatabasePlugin`, `NotificationsPlugin`, `AuthTokenPlugin`
- **AND** it MAY register: `HttpClientPlugin`, `FileSystemPlugin` (if available)
- **AND** it SHALL register custom services: `ITodoService`, `IThemeService`

#### Scenario: Web project uses @agibuild/bridge
- **GIVEN** the Web project
- **WHEN** `package.json` is inspected
- **THEN** `@agibuild/bridge` SHALL be a dependency
- **AND** `bridge.d.ts` SHALL be generated from the Bridge project
- **AND** the SPA SHALL use `bridgeClient.getService<T>()` for typed service access

#### Scenario: Build and run succeed
- **GIVEN** the showcase solution
- **WHEN** `dotnet build` is run from `samples/showcase-todo/`
- **THEN** the solution SHALL build successfully
- **AND** `dotnet run` from the Desktop project SHALL launch the app
- **AND** the React SPA SHALL load in the WebView and display the todo UI
