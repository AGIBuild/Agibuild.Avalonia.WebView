using Agibuild.Fulora;

namespace Agibuild.Fulora.Plugin.Database;

/// <summary>
/// Bridge service for SQLite database access.
/// Provides query, execute, and transaction operations backed by Microsoft.Data.Sqlite.
/// </summary>
[JsExport]
public interface IDatabaseService
{
    /// <summary>Executes a SELECT query and returns rows as dictionaries.</summary>
    Task<QueryResult> Query(string sql, Dictionary<string, object?>? parameters = null);

    /// <summary>Executes a non-query statement and returns the number of affected rows.</summary>
    Task<int> Execute(string sql, Dictionary<string, object?>? parameters = null);

    /// <summary>Executes multiple statements in a single transaction.</summary>
    Task<int> ExecuteBatch(string[] statements);

    /// <summary>Begins a new transaction.</summary>
    Task BeginTransaction();

    /// <summary>Commits the active transaction.</summary>
    Task CommitTransaction();

    /// <summary>Rolls back the active transaction.</summary>
    Task RollbackTransaction();

    /// <summary>Returns the current schema version from applied migrations.</summary>
    Task<int> GetSchemaVersion();
}
