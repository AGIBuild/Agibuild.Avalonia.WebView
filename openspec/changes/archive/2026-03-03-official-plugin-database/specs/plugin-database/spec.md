# Plugin Database — Spec

## Purpose

Define BDD-style requirements for the Database bridge plugin: SQLite access via `IDatabaseService` with query execution, parameterized queries, transactions, schema migrations, error handling, and database path configuration. Enables structured local data storage from JavaScript in Fulora hybrid apps.

## Requirements

### Requirement: Query execution returns rows as JSON-serializable objects

`IDatabaseService.Query` SHALL execute a SELECT statement and return result rows as an array of objects, each representing a row with column names as keys.

#### Scenario: Query returns rows with column names as keys
- **GIVEN** a database with a table `users` containing columns `id`, `name`, `email`
- **WHEN** JS calls `databaseService.query("SELECT id, name FROM users")` with empty params
- **THEN** the method SHALL return an array of objects
- **AND** each object SHALL have keys `id` and `name` with values matching the row data
- **AND** values SHALL be JSON-serializable (string, number, boolean, null)

#### Scenario: Query with no results returns empty array
- **GIVEN** a database with an empty table
- **WHEN** JS calls `databaseService.query("SELECT * FROM users")`
- **THEN** the method SHALL return an empty array `[]`
- **AND** SHALL NOT throw

#### Scenario: Execute runs non-query statements and returns affected row count
- **GIVEN** a database with a table `users`
- **WHEN** JS calls `databaseService.execute("INSERT INTO users (name) VALUES ('Alice')")`
- **THEN** the method SHALL execute the statement
- **AND** SHALL return the number of rows affected (e.g., 1 for INSERT)
- **AND** SHALL NOT return result rows

---

### Requirement: Parameterized queries prevent SQL injection

All SQL execution SHALL use parameterized queries. User-provided values SHALL be bound as parameters, never concatenated into the SQL string.

#### Scenario: Query accepts params object for named parameters
- **GIVEN** a database with table `users` and column `name`
- **WHEN** JS calls `databaseService.query("SELECT * FROM users WHERE name = @name", { name: "Alice" })`
- **THEN** the method SHALL bind `@name` to the value `"Alice"`
- **AND** SHALL execute the parameterized query
- **AND** SHALL return matching rows
- **AND** SHALL NOT interpret `"Alice"` as SQL (e.g., `"Alice'; DROP TABLE users;--"` must be treated as literal string)

#### Scenario: Query accepts params array for positional parameters
- **GIVEN** a database with table `users`
- **WHEN** JS calls `databaseService.query("SELECT * FROM users WHERE id = ?", [42])`
- **THEN** the method SHALL bind the first `?` to `42`
- **AND** SHALL execute the parameterized query
- **AND** SHALL return matching rows

#### Scenario: Execute rejects raw SQL with concatenated values
- **GIVEN** the Database plugin is configured
- **WHEN** a caller attempts to pass user input as part of the SQL string (e.g., `"SELECT * FROM users WHERE name = '" + userInput + "'"`)
- **THEN** the plugin SHALL NOT provide a non-parameterized API
- **AND** documentation SHALL require params for all user-provided values

---

### Requirement: Transaction support for atomic multi-statement operations

`IDatabaseService` SHALL support transactions: begin, execute multiple statements, commit or rollback.

#### Scenario: Transaction commits when no error
- **GIVEN** a database with tables `accounts` and `transactions`
- **WHEN** JS calls `databaseService.beginTransaction()`
- **AND** executes multiple `execute` calls within the transaction
- **AND** calls `databaseService.commit()`
- **THEN** all statements SHALL be applied atomically
- **AND** subsequent queries SHALL see the committed data

#### Scenario: Transaction rolls back on error
- **GIVEN** a transaction has been begun
- **WHEN** one of the `execute` calls fails (e.g., constraint violation)
- **AND** JS calls `databaseService.rollback()`
- **THEN** no statements in the transaction SHALL be applied
- **AND** the database SHALL remain in the state before `beginTransaction`

#### Scenario: Nested transactions are not supported (single active transaction)
- **GIVEN** a transaction has been begun
- **WHEN** JS calls `beginTransaction()` again before commit/rollback
- **THEN** the plugin SHALL throw or return an error indicating a transaction is already active
- **AND** documentation SHALL specify single-transaction semantics

---

### Requirement: Schema migration runs at plugin initialization

The plugin SHALL apply schema migrations when the database is first opened. Migrations SHALL be versioned SQL scripts applied in order.

#### Scenario: Migrations run in order on first connection
- **GIVEN** the plugin is configured with migrations `001_create_users.sql`, `002_add_email.sql`
- **WHEN** the database is opened for the first time (first query or execute)
- **THEN** the plugin SHALL create a `schema_version` table if it does not exist
- **AND** SHALL execute `001_create_users.sql`, then `002_add_email.sql` in order
- **AND** SHALL record each applied version in `schema_version`
- **AND** SHALL make the database ready for queries

#### Scenario: Only pending migrations run on subsequent opens
- **GIVEN** a database that has already had migrations 001 and 002 applied
- **WHEN** the plugin opens the database and a new migration `003_add_index.sql` exists
- **THEN** the plugin SHALL execute only `003_add_index.sql`
- **AND** SHALL NOT re-execute 001 or 002
- **AND** SHALL update `schema_version` with 003

#### Scenario: Migration failure rolls back and throws
- **GIVEN** a migration script that fails (e.g., syntax error or constraint violation)
- **WHEN** the plugin attempts to apply the migration
- **THEN** the plugin SHALL roll back the migration transaction
- **AND** SHALL throw an exception with a clear message
- **AND** SHALL NOT update `schema_version` for the failed migration

---

### Requirement: Error handling returns actionable information

Database errors (SQLite errors, constraint violations, connection failures) SHALL be surfaced to JavaScript with sufficient context for debugging.

#### Scenario: SQL error returns structured error
- **GIVEN** a query with invalid SQL (e.g., `"SELEC * FROM users"`)
- **WHEN** JS calls `databaseService.query(...)`
- **THEN** the method SHALL throw (or return a rejected promise)
- **AND** the error SHALL include a message describing the failure (e.g., "near 'SELEC': syntax error")
- **AND** the error MAY include an error code or SQLite error number for programmatic handling

#### Scenario: Constraint violation returns clear error
- **GIVEN** a table with a UNIQUE constraint on `email`
- **WHEN** JS attempts to insert a duplicate email via `execute`
- **THEN** the method SHALL throw
- **AND** the error message SHALL indicate a constraint violation
- **AND** the caller SHALL be able to catch and handle the error

#### Scenario: Connection or file access failure is reported
- **GIVEN** the database path is invalid or the app lacks write permission
- **WHEN** the plugin attempts to open the database
- **THEN** the plugin SHALL throw with a message indicating the failure
- **AND** SHALL NOT silently fail or return empty results

---

### Requirement: Database path is configurable

The database file location SHALL be configurable. Default SHALL be the app-local data directory.

#### Scenario: Default path uses app data directory
- **GIVEN** the plugin is registered with no path override
- **WHEN** the database is opened
- **THEN** the database file SHALL be created at `{AppData}/fulora/database.db` (or platform-equivalent)
- **AND** the directory SHALL be created if it does not exist

#### Scenario: Custom path is configurable via plugin options
- **GIVEN** the plugin is registered with `DatabasePath: "/custom/path/app.db"` (or equivalent option)
- **WHEN** the database is opened
- **THEN** the database file SHALL be created at the specified path
- **AND** relative paths SHALL be resolved relative to the app data directory (or document resolution rules)

#### Scenario: In-memory database is supported for testing
- **GIVEN** the plugin is configured with `DatabasePath: ":memory:"` (SQLite in-memory)
- **WHEN** the database is opened
- **THEN** the plugin SHALL use an in-memory database
- **AND** data SHALL NOT persist across connection close
- **AND** migrations SHALL still run on first use
