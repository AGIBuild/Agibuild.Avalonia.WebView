# Official Plugin: Database — Design

## Context

Hybrid apps need structured local data storage beyond key-value strings. The LocalStorage plugin provides only string CRUD. A SQLite-based database plugin enables SQL queries, schema management, and structured data access from both C# and JavaScript via the bridge. This change introduces an official plugin following the `IBridgePlugin` convention with NuGet + npm dual distribution.

## Goals / Non-Goals

### Goals

- Provide SQLite database access from JavaScript via the bridge
- Support parameterized queries (SQL injection prevention)
- Support transactions for atomic multi-statement operations
- Support schema migrations via versioned SQL scripts
- Store database file in app-local data directory
- Expose async Task-based API for non-blocking I/O

### Non-Goals

- ORM/Entity Framework integration
- Server database connectivity (SQL Server, PostgreSQL, etc.)
- Cross-device sync
- Full-text search or advanced SQLite extensions beyond core

## Decisions

### D1: SQLite via Microsoft.Data.Sqlite

**Decision**: Use SQLite as the embedded database engine, accessed via `Microsoft.Data.Sqlite`.

**Rationale**: SQLite is lightweight, file-based, requires no server, and is widely used for local storage. Microsoft.Data.Sqlite is the official .NET provider, well-maintained, and supports async operations. No additional native dependencies beyond SQLite's single-file distribution.

### D2: Parameterized queries only (SQL injection prevention)

**Decision**: All SQL execution SHALL use parameterized queries. The `IDatabaseService` API SHALL accept a `params` object (or array) that is bound to named or positional parameters. Raw string concatenation for SQL SHALL NOT be supported.

**Rationale**: SQL injection is a critical security risk. Parameterized queries ensure user input is never interpreted as SQL. The bridge serializes params from JS; the C# side binds them to `SqliteParameter` objects.

### D3: JSON serialization for result rows

**Decision**: Query results SHALL be returned as JSON-serializable structures. Each row SHALL be represented as an object (dictionary) with column names as keys and values serialized to JSON-compatible types (string, number, boolean, null). Complex types (DateTime, byte[]) SHALL be converted to ISO 8601 strings and base64 respectively.

**Rationale**: The bridge serializes C# return values to JSON for JavaScript. Dictionary<string, object?> or a DTO per row enables straightforward serialization. Consistent type mapping ensures predictable JS consumption.

### D4: Async Task API

**Decision**: All `IDatabaseService` methods SHALL return `Task` or `Task<T>` and SHALL perform I/O asynchronously. No synchronous blocking calls SHALL be exposed.

**Rationale**: Async avoids blocking the bridge thread and aligns with bridge convention (all JsExport methods are async). SQLite I/O can block; async wrappers prevent UI freeze in hybrid apps.

### D5: Migration runner in plugin init

**Decision**: The plugin SHALL run schema migrations during initialization (when the database connection is first established). Migrations SHALL be versioned SQL scripts embedded in the assembly or supplied via configuration. The plugin SHALL maintain a `schema_version` table to track applied migrations and SHALL apply only pending migrations in order.

**Rationale**: Schema evolution is essential for production apps. Running migrations at init ensures the database is ready before any queries. Versioned scripts enable deterministic, reproducible upgrades.

### D6: Database path configurable via BridgeOptions

**Decision**: The database file path SHALL be configurable. Default: `{AppData}/fulora/database.db`. The plugin SHALL accept a `DatabasePath` (or equivalent) option via `BridgeOptions` or a plugin-specific options type passed to the service factory. The path SHALL resolve to the app-local data directory when relative.

**Rationale**: Different apps may need different database names or locations (e.g., per-user, per-environment). Configurability supports testing (in-memory or temp path) and multi-tenant scenarios.

## Risks / Trade-offs

### R1: SQLite file locking and concurrency

**Risk**: SQLite uses file-level locking. Concurrent writes from multiple threads or processes can cause `SQLITE_BUSY` or `SQLITE_LOCKED`. The bridge typically invokes from a single thread, but async re-entrancy could cause issues.

**Mitigation**: Use `SqliteConnection` with default mode (serialized). Consider `busy_timeout` for retries. Document single-writer expectation. For high concurrency, recommend connection pooling or WAL mode (SQLite default in recent versions).

### R2: Migration failure leaves database in partial state

**Risk**: If a migration fails mid-execution, the database may be in an inconsistent state. `schema_version` might not reflect the actual schema.

**Mitigation**: Run each migration in a transaction. On failure, roll back and throw. Document migration authoring best practices (idempotent where possible, avoid destructive changes without backup).

### R3: Large result sets and memory

**Risk**: `query()` returning many rows could consume significant memory when serialized to JSON.

**Mitigation**: Document pagination patterns (LIMIT/OFFSET). Consider future streaming API for large datasets. For MVP, accept in-memory results with documented limits.

### R4: Type mapping edge cases

**Risk**: SQLite type affinity and .NET type mapping can produce surprises (e.g., INTEGER as long, REAL as double, BLOB as byte[]).

**Mitigation**: Define explicit type mapping in the spec. Document supported column types and their JSON representation. Test edge cases (null, empty string, large integers, floating point precision).
