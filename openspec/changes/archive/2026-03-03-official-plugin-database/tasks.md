# Official Plugin: Database — Tasks

## 1. Project Setup

- [x] 1.1 Create new project `plugins/Agibuild.Fulora.Plugin.Database/Agibuild.Fulora.Plugin.Database.csproj`
- [x] 1.2 Add package references: `Microsoft.Data.Sqlite`, `Agibuild.Fulora.Core`, `Agibuild.Fulora.Bridge.Generator` (Analyzer)
- [x] 1.3 Configure package metadata: PackageId, Description, PackageTags (fulora, bridge, plugin, database, sqlite), Authors, License
- [x] 1.4 Add `fulora-plugin` tag for plugin registry discovery
- [x] 1.5 Add project to solution and verify build

## 2. IDatabaseService Contract

- [x] 2.1 Define `[JsExport] IDatabaseService` interface with: `Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> Query(string sql, object? params)`, `Task<int> Execute(string sql, object? params)`
- [x] 2.2 Add transaction methods: `Task BeginTransaction()`, `Task Commit()`, `Task Rollback()`
- [x] 2.3 Define parameter binding: support `Dictionary<string, object?>` for named params (`@name`), `object[]` for positional params (`?`)
- [x] 2.4 Define result row format: array of `Dictionary<string, object?>` or equivalent JSON-serializable structure
- [x] 2.5 Document type mapping: SQLite types → JSON (INTEGER→number, TEXT→string, REAL→number, BLOB→base64, NULL→null, DateTime→ISO 8601)

## 3. SQLite Implementation

- [x] 3.1 Create `DatabaseService` class implementing `IDatabaseService`
- [x] 3.2 Implement connection management: open on first use, connection per service instance (or document pooling strategy)
- [x] 3.3 Implement `Query`: create `SqliteCommand`, bind params, execute `ExecuteReaderAsync`, read rows into list of dictionaries
- [x] 3.4 Implement `Execute`: create `SqliteCommand`, bind params, execute `ExecuteNonQueryAsync`, return affected rows
- [x] 3.5 Implement param binding: map `@name` from params object, map `?` from params array; convert JS types to SqliteType
- [x] 3.6 Implement transaction: `BeginTransaction` starts `SqliteTransaction`, `Commit`/`Rollback` complete it; ensure single active transaction
- [x] 3.7 Add `SqliteConnection` with `busy_timeout` or retry for concurrent access (if applicable)

## 4. Migration Runner

- [x] 4.1 Define migration script convention: embedded resources or configurable paths, naming `NNN_description.sql`
- [x] 4.2 Create `schema_version` table on first open: `CREATE TABLE IF NOT EXISTS schema_version (version INTEGER PRIMARY KEY, applied_at TEXT)`
- [x] 4.3 Implement migration runner: read applied versions, enumerate pending scripts, execute each in a transaction
- [x] 4.4 On migration success: insert version into `schema_version`, commit
- [x] 4.5 On migration failure: rollback, throw with migration name and error
- [x] 4.6 Invoke migration runner during `DatabaseService` initialization (lazy, on first query/execute)
- [x] 4.7 Support plugin options for migration source (embedded assembly resources, or path)

## 5. Plugin Registration and Options

- [x] 5.1 Create `DatabasePlugin : IBridgePlugin` with `GetServices()` returning `IDatabaseService` descriptor
- [x] 5.2 Define `DatabasePluginOptions` (or use BridgeOptions extension): `DatabasePath`, `MigrationSource`
- [x] 5.3 Pass options to service factory; resolve default path: `Environment.GetFolderPath(ApplicationData)/fulora/database.db`
- [x] 5.4 Support `:memory:` for in-memory database
- [x] 5.5 Add `fulora-plugin.json` manifest: id, displayName, services, npmPackage

## 6. npm Companion Package

- [x] 6.1 Create `packages/bridge-plugin-database/` directory structure
- [x] 6.2 Add `package.json` with name `@agibuild/bridge-plugin-database`, peer dependency on `@agibuild/bridge`
- [x] 6.3 Add TypeScript declarations for `IDatabaseService`: `query(sql, params?)`, `execute(sql, params?)`, `beginTransaction()`, `commit()`, `rollback()`
- [x] 6.4 Add typed `getDatabaseService(bridgeClient)` helper
- [x] 6.5 Add result row type: `Record<string, unknown>[]` or `DatabaseRow[]`
- [x] 6.6 Copy or generate types from bridge.d.ts output; ensure parity with C# interface

## 7. Tests

- [x] 7.1 Unit tests: `DatabaseService.Query` — in-memory DB, create table, insert rows, query returns correct structure
- [x] 7.2 Unit tests: `DatabaseService.Execute` — INSERT, UPDATE, DELETE return affected row count
- [x] 7.3 Unit tests: Parameterized queries — named and positional params, SQL injection attempt returns literal (no injection)
- [x] 7.4 Unit tests: Transaction — commit applies all, rollback reverts all, nested transaction throws
- [x] 7.5 Unit tests: Migration runner — first run applies all, second run applies only new, failed migration rolls back
- [x] 7.6 Unit tests: Error handling — invalid SQL throws, constraint violation throws with clear message
- [x] 7.7 Unit tests: Database path — custom path creates file at location, `:memory:` works
- [x] 7.8 Integration test: Install plugin → `UsePlugin<DatabasePlugin>` → call query/execute from JS → verify results
