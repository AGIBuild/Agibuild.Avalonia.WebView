# Showcase Todo App — Tasks

## 1. Project Scaffold

- [x] 1.1 Create `samples/showcase-todo/` directory with solution `ShowcaseTodo.sln`
- [x] 1.2 Create `ShowcaseTodo.Desktop` project (Avalonia host with WebView, wwwroot)
- [x] 1.3 Create `ShowcaseTodo.Bridge` project (shared interfaces, models)
- [x] 1.4 Create `ShowcaseTodo.Web` project (React + Vite + TypeScript)
- [x] 1.5 Create `ShowcaseTodo.Tests` project (xUnit)
- [x] 1.6 Configure Desktop to reference Bridge, Avalonia packages, Fulora packages
- [x] 1.7 Configure Web to reference `@agibuild/bridge` and plugin npm packages
- [x] 1.8 Configure bridge.d.ts generation from Bridge project into Web `src/bridge/`
- [x] 1.9 Add `useBridgeReady()` hook and typed service layer in Web project
- [x] 1.10 Verify `dotnet build` and `dotnet run` succeed with minimal shell

## 2. Bridge Interfaces

- [x] 2.1 Define `[JsExport] ITodoService` with: `addTodo(todo)`, `listTodos(filter?)`, `updateTodo(id, patch)`, `deleteTodo(id)`, `sync()`
- [x] 2.2 Define `Todo` model: id, title, completed, dueDate, reminderAt, createdAt, updatedAt
- [x] 2.3 Define `[JsExport] IThemeService` with: `getTheme()`, `setTheme(theme)`
- [x] 2.4 Define `[JsImport] IThemeCallback` with: `onThemeChanged(theme)`
- [x] 2.5 Define `[JsImport] IShortcutCallback` with: `onAddTodo()`, `onFocusSearch()`
- [x] 2.6 Implement `TodoService` in Bridge/Desktop using `IDatabaseService` (from Database plugin)
- [x] 2.7 Implement `ThemeService` (or use theme-sync bridge if available)
- [x] 2.8 Register `ITodoService` and `IThemeService` in host DI; expose via bridge
- [x] 2.9 Wire `IThemeCallback` and `IShortcutCallback` so host can invoke SPA

## 3. Database Schema and Migrations

- [x] 3.1 Create migration `001_create_todos.sql`: CREATE TABLE todos (id TEXT PRIMARY KEY, title TEXT NOT NULL, completed INTEGER DEFAULT 0, due_date TEXT, reminder_at TEXT, created_at TEXT, updated_at TEXT)
- [x] 3.2 Add migration to Database plugin configuration (embedded resource or path)
- [x] 3.3 Configure database path (e.g., app data directory)
- [x] 3.4 Verify migrations run on first launch; schema_version tracks applied migrations
- [x] 3.5 Implement TodoService CRUD using parameterized queries via IDatabaseService

## 4. React SPA

- [x] 4.1 Create TodoList component (display todos, filter controls)
- [x] 4.2 Create TodoItem component (title, complete checkbox, delete, edit)
- [x] 4.3 Create AddTodo component (form for new todo with optional due date, reminder)
- [x] 4.4 Create TodoDetail or inline edit for updating todos
- [x] 4.5 Add routing if multi-page (e.g., list view, detail view)
- [x] 4.6 Implement state management (React state, or lightweight store) for todos
- [x] 4.7 Wire components to `todoService` via bridge (add, list, update, delete, filter)
- [x] 4.8 Add theme-aware styling (CSS variables or class based on `themeService.getTheme()`)
- [x] 4.9 Register `IThemeCallback` and `IShortcutCallback` with host on bridge ready
- [x] 4.10 Add loading and error states for bridge calls

## 5. Plugin Integration

- [x] 5.1 Register `DatabasePlugin` in Desktop host with database path and migrations
- [x] 5.2 Register `NotificationsPlugin` in Desktop host
- [x] 5.3 Register `AuthTokenPlugin` in Desktop host (mock token for showcase)
- [x] 5.4 Register `HttpClientPlugin` if available (for future sync)
- [x] 5.5 Register `FileSystemPlugin` if available (for export/import)
- [x] 5.6 Wire TodoService to use IDatabaseService from Database plugin
- [x] 5.7 Wire reminder logic to use INotificationService from Notifications plugin
- [x] 5.8 Wire sync (mock) to use IAuthTokenService and optionally IHttpClientService
- [x] 5.9 Ensure all plugin npm packages are referenced in Web project if needed

## 6. Feature Implementation

- [x] 6.1 Implement todo CRUD: add, list, update, delete via TodoService
- [x] 6.2 Implement filter (all, active, completed) in listTodos
- [x] 6.3 Implement reminder check: timer or background task compares reminderAt with now; show notification when due
- [x] 6.4 Implement notification click handler: focus app, optionally invoke JsImport to navigate to todo
- [x] 6.5 Implement theme sync: getTheme on load, setTheme on toggle, onThemeChanged callback updates SPA
- [x] 6.6 Implement global shortcuts: Ctrl+Shift+N (add todo), Ctrl+Shift+F (focus search)
- [x] 6.7 Implement sync (mock): TodoService.sync() uses auth token, simulates API call or no-op
- [x] 6.8 Implement offline-first: ensure all CRUD uses local DB; no network dependency for core operations
- [x] 6.9 Add optional export/import via FileSystem plugin if integrated

## 7. Documentation

- [x] 7.1 Create `samples/showcase-todo/README.md` with: overview, features, how to run, project structure
- [x] 7.2 Document architecture: host + SPA, bridge services, plugin usage
- [x] 7.3 Add architecture diagram (Mermaid or image): host, WebView, bridge, plugins, SPA
- [x] 7.4 Document which plugins are used and how (Database, Notifications, Auth Token, etc.)
- [x] 7.5 Add README section on extending the showcase (e.g., adding real API sync)

## 8. Tests

- [x] 8.1 Unit tests: TodoService CRUD with in-memory database
- [x] 8.2 Unit tests: TodoService listTodos with filter
- [x] 8.3 Unit tests: Migration runner applies schema correctly
- [x] 8.4 Integration test: Bridge call from simulated JS to TodoService returns expected data
- [x] 8.5 Manual/E2E: Full flow — add todo, filter, complete, delete, theme toggle, shortcut
- [x] 8.6 Verify `dotnet test` passes for ShowcaseTodo.Tests
