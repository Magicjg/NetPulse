using System.IO;
using System.Text.Json;
using NetPulse.App.Models;

namespace NetPulse.App.Services;

public sealed class AppSettingsService
{
    private readonly string _settingsFilePath;

    public AppSettingsService()
    {
        string appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NetPulse");

        Directory.CreateDirectory(appDataPath);
        _settingsFilePath = Path.Combine(appDataPath, "settings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return new AppSettings();
        }

        string json = File.ReadAllText(_settingsFilePath);
        AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json);
        return settings ?? new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_settingsFilePath, json);
    }
}
