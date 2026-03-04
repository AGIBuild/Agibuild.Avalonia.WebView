namespace Agibuild.Fulora.Plugin.Database;

/// <summary>
/// Options for configuring the database plugin.
/// </summary>
public sealed class DatabaseOptions
{
    /// <summary>Path to the SQLite database file. Use <c>:memory:</c> for in-memory.</summary>
    public string DatabasePath { get; init; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Fulora", "fulora.db");

    /// <summary>Paths to SQL migration scripts (e.g. 001_init.sql) applied on first connection.</summary>
    public string[]? MigrationScripts { get; init; }
}
