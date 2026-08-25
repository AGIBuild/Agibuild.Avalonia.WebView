using SQLitePCL;

namespace Agibuild.Fulora.Plugin.Database;

internal static class SqliteProviderInitializer
{
    private static readonly object SyncRoot = new();
    private static bool s_initialized;

    public static void EnsureInitialized()
    {
        if (s_initialized)
            return;

        lock (SyncRoot)
        {
            if (s_initialized)
                return;

            raw.SetProvider(OperatingSystem.IsWindows()
                ? new SQLite3Provider_winsqlite3()
                : new SQLite3Provider_sqlite3());

            s_initialized = true;
        }
    }
}
