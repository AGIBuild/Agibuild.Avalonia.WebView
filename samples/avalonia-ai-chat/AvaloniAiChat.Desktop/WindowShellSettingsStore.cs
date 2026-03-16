using System.Text.Json;
using Agibuild.Fulora;

namespace AvaloniAiChat.Desktop;

/// <summary>
/// Persists AI chat window shell settings to local app data.
/// </summary>
internal sealed class WindowShellSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _filePath;

    public WindowShellSettingsStore()
        : this(DefaultSettingsPath())
    {
    }

    internal WindowShellSettingsStore(string filePath)
    {
        _filePath = filePath;
    }

    public WindowShellSettings? Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return null;

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<WindowShellSettings>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public void Save(WindowShellSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Keep sample resilient if local storage is unavailable.
        }
    }

    private static string DefaultSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "AvaloniAiChat", "window-shell-settings.json");
    }
}
