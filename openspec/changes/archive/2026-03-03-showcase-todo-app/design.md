# Showcase Todo App — Design

## Context

Existing Fulora samples (avalonia-react, avalonia-vue, minimal-hybrid) demonstrate basic bridge usage: a simple host + SPA with a few bridge calls. They do not show how to combine multiple plugins, handle offline-first data, sync theme, or use notifications. A showcase Todo app would demonstrate the full framework capability stack and serve as a reference architecture for adoption. Goal: Adoption-readiness.

**Existing contracts**: `IBridgePlugin`, official plugins (database, notifications, auth-token, http-client, file-system), theme-sync bridge, global-shortcut bridge, project template structure, `@agibuild/bridge`, React integration patterns.

## Goals / Non-Goals

### Goals

- Full-featured Todo app: CRUD, filtering, completion, due dates, reminders
- Database plugin for local persistence (SQLite)
- Auth token plugin for API sync (when backend exists; mock for showcase)
- Offline-first: local DB is source of truth; sync when online
- System notifications for todo reminders (notifications plugin)
- Theme sync: host theme (light/dark) syncs to web; web can request theme change
- Global shortcuts: e.g., Ctrl+Shift+T to add todo, Ctrl+Shift+F to focus
- HTTP client plugin for future API sync (optional in showcase)
- File-system plugin for export/import (optional)
- Demonstrates DI, bridge patterns, project structure at production scale

### Non-Goals

- Production-ready todo app (no auth server, no real backend)
- Backend API server implementation
- Mobile version (Avalonia desktop only in this change)
- Real user authentication (mock token for showcase)

## Decisions

### D1: Architecture — Avalonia Desktop + React SPA

**Decision**: The showcase SHALL follow the standard Fulora hybrid structure: Avalonia desktop host with embedded WebView, React SPA for the UI. The host SHALL register all required plugins and bridge services. The SPA SHALL consume bridge services via `@agibuild/bridge` and generated types.

**Rationale**: Matches the primary Fulora target (desktop hybrid). React is widely used; the patterns apply to Vue/Svelte. Avalonia provides native window, global shortcuts, and system integration.

### D2: Bridge interfaces for host services

**Decision**: Define `[JsExport]` interfaces for: `ITodoService` (CRUD, sync), `IThemeService` (get/set theme), `INotificationService` (show, request permission). The database plugin provides `IDatabaseService`; auth-token and notifications are plugins. Theme and shortcuts MAY be host-provided or from existing bridge specs.

**Rationale**: Clear separation of concerns. Todo CRUD goes through a host service that uses the database plugin internally. Theme and notifications are either plugin services or host-wrapped. The SPA only sees typed bridge services.

### D3: Local-first data flow

**Decision**: Todos SHALL be stored in the local SQLite database (database plugin). The SPA SHALL call `ITodoService` methods (add, update, delete, list). The host `TodoService` SHALL use `IDatabaseService` for persistence. Offline support: all CRUD works offline; sync to a (mock) API when online is a future enhancement. For showcase, "sync" can be a no-op or simulate with a delay.

**Rationale**: Local-first is a key Fulora use case. Database plugin provides SQLite; the host orchestrates. Offline-first means the app works without network; sync is additive.

### D4: Plugin integration points

**Decision**: The host SHALL register: `DatabasePlugin`, `NotificationsPlugin`, `AuthTokenPlugin` (if available), `HttpClientPlugin` (optional), `FileSystemPlugin` (optional). The host SHALL also expose `ITodoService` (custom) and `IThemeService` (from theme-sync or custom). Each plugin is registered via `Bridge.UsePlugin<T>()`.

**Rationale**: Demonstrates multi-plugin composition. Each plugin adds a capability; the host wires them together. Consumers see how to combine plugins in a real app.

### D5: Theme sync flow

**Decision**: Theme SHALL sync bidirectionally: host can set theme (e.g., from system or user preference); web can read theme and request change. The host SHALL expose `IThemeService` with `getTheme()` and `setTheme(theme)`. The SPA SHALL apply theme via CSS variables or class. When host theme changes (e.g., system), host SHALL call `[JsImport] IThemeCallback.onThemeChanged(theme)` to notify web.

**Rationale**: Theme sync is a common hybrid requirement. Bidirectional ensures host and web stay in sync. Uses existing theme-sync patterns if available.

### D6: Notifications for reminders

**Decision**: Todos MAY have a due date/reminder. When a reminder is due, the host SHALL use the notifications plugin to show a system notification. The host SHALL run a background check (timer or similar) to compare due dates with current time; when due, call `INotificationService.show()`. Notification click SHALL focus the app and optionally navigate to the todo.

**Rationale**: Demonstrates notifications plugin in a realistic scenario. Reminders are a natural fit for todos. Click-to-focus uses `[JsImport]` callback pattern.

### D7: Global shortcuts

**Decision**: The host SHALL register global shortcuts (e.g., Ctrl+Shift+T for "add todo", Ctrl+Shift+F for "focus search"). When triggered, the host SHALL invoke a bridge call to the web (e.g., `[JsImport] IShortcutCallback.onAddTodo()` or `onFocusSearch()`). The SPA SHALL handle the callback and show the add UI or focus the input.

**Rationale**: Global shortcuts demonstrate host-to-web interaction. Uses `[JsImport]` for host-initiated calls. Common pattern for desktop apps.

### D8: Project structure

**Decision**: Follow the template structure: `ShowcaseTodo.Desktop/` (Avalonia host), `ShowcaseTodo.Web/` (React SPA), `ShowcaseTodo.Bridge/` (shared bridge interfaces if needed). The host project references all plugins; the Web project references `@agibuild/bridge` and plugin npm packages.

**Rationale**: Matches the agibuild-hybrid template and existing samples. Clear separation for maintainability.

## Risks / Trade-offs

### R1: Plugin availability

**Risk**: Some official plugins (auth-token, http-client, file-system) may not exist yet. Dependency on other OpenSpec changes.

**Mitigation**: Design for optional plugins. Core showcase: database + notifications + theme. Add others as they become available. Use mocks for missing plugins.

### R2: Scope creep

**Risk**: A "full" todo app can grow indefinitely (tags, projects, subtasks, etc.).

**Mitigation**: Define explicit scope: CRUD, filtering, completion, due dates, reminders, theme, shortcuts. Defer advanced features to follow-up.

### R3: Sample maintenance

**Risk**: Showcase may drift from framework updates (plugin API changes, bridge changes).

**Mitigation**: Document that showcase tracks main branch. Add to CI to ensure it builds and runs. Keep it as a living reference.
