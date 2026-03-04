namespace Agibuild.Fulora.Plugin.Database;

/// <summary>
/// Result of a SQL query containing column names and rows as dictionaries.
/// </summary>
public sealed class QueryResult
{
    /// <summary>Column names in result order.</summary>
    public string[] Columns { get; init; } = [];

    /// <summary>Result rows, each as a dictionary of column name to value.</summary>
    public List<Dictionary<string, object?>> Rows { get; init; } = [];

    /// <summary>Number of rows returned.</summary>
    public int RowCount { get; init; }
}
