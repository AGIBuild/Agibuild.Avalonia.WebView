## Why

Hybrid apps frequently need structured local data storage (user preferences, cached API responses, offline queues). The existing LocalStorage plugin only supports key-value strings. A database plugin using SQLite (via Microsoft.Data.Sqlite) would provide SQL query capability, schema management, and structured data access from both C# and JavaScript. Goal: Phase 11 M11.3 (Official Plugin Suite).

## What Changes

- New NuGet: `Agibuild.Fulora.Plugin.Database` implementing `IBridgePlugin`
- New npm: `@agibuild/bridge-plugin-database` with TypeScript types
- [JsExport] IDatabaseService with: execute(sql, params), query(sql, params), transaction support
- Schema migration support via versioned SQL scripts
- Database file stored in app-local data directory

## Capabilities

### New Capabilities
- `plugin-database`: Bridge plugin for SQLite database access with query, execute, migration

### Modified Capabilities
(none)

## Non-goals

- ORM/Entity Framework integration
- Server database connectivity
- Cross-device sync

## Impact

- New project: src/Agibuild.Fulora.Plugin.Database/
- New npm package: packages/bridge-plugin-database/
- Dependencies: Microsoft.Data.Sqlite
