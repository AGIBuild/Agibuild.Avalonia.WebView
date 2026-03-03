## Why

Existing samples (avalonia-react, avalonia-vue, minimal-hybrid) demonstrate basic bridge usage but not real-world complexity. A showcase Todo app with database storage, auth, offline support, theme sync, and notifications would demonstrate the full framework capability stack and serve as a reference architecture. Goal: Adoption-readiness.

## What Changes

- New sample: samples/showcase-todo/ with Avalonia desktop + React SPA
- Features: CRUD todos with database plugin, auth token for API sync, local-first with offline support, system notifications for reminders, theme sync between host and web, global shortcuts
- Uses multiple official plugins together (database, notifications, auth-token, http-client, file-system)
- Demonstrates project template structure, DI integration, and bridge patterns at production scale

## Capabilities

### New Capabilities
- `showcase-todo-app`: Full-featured reference application demonstrating framework capabilities

### Modified Capabilities
(none)

## Non-goals

- Production-ready todo app, backend API server, mobile version in this change

## Impact

- New: samples/showcase-todo/ (Avalonia + React)
- Dependencies: all official plugins
- No changes to framework code
