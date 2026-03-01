using System.Text.Json;
using AvaloniVue.Bridge.Models;

namespace AvaloniVue.Bridge.Services;

/// <summary>
/// Manages user settings with JSON file persistence in the app data directory.
/// </summary>
public class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private AppSettings _current;
    private readonly string _filePath;

    public SettingsService()
        : this(DefaultSettingsPath())
    {
    }

    public SettingsService(string filePath)
    {
        _filePath = filePath;
        _current = LoadFromDisk();
    }

    public Task<AppSettings> GetSettings() =>
        Task.FromResult(_current);

    public Task<AppSettings> UpdateSettings(AppSettings settings)
    {
        _current = settings;
        SaveToDisk(_current);
        return Task.FromResult(_current);
    }

    private AppSettings LoadFromDisk()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
        }
        catch
        {
            // Fall through to default
        }

        return new AppSettings();
    }

    private void SaveToDisk(AppSettings settings)
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (dir is not null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Swallow in sample — production code would log
        }
    }

    private static string DefaultSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "AvaloniVue", "settings.json");
    }
}
