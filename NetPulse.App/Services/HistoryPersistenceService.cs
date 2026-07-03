using System.IO;
using System.Text.Json;
using NetPulse.App.Models;

namespace NetPulse.App.Services;

public sealed class HistoryPersistenceService
{
    private readonly string _historyFilePath;

    public HistoryPersistenceService()
    {
        string appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NetPulse");

        Directory.CreateDirectory(appDataPath);
        _historyFilePath = Path.Combine(appDataPath, "history.json");
    }

    public IReadOnlyList<HistoryItem> Load()
    {
        if (!File.Exists(_historyFilePath))
        {
            return [];
        }

        string json = File.ReadAllText(_historyFilePath);
        List<HistoryItem>? items = JsonSerializer.Deserialize<List<HistoryItem>>(json);
        return items ?? [];
    }

    public void Save(IEnumerable<HistoryItem> items)
    {
        string json = JsonSerializer.Serialize(items, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_historyFilePath, json);
    }
}
