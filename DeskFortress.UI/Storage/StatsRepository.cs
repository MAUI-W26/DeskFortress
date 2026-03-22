using System.Text.Json;

namespace DeskFortress.UI.Storage;

/// <summary>
/// Holds aggregate player statistics.
///
/// This repository is intentionally simple for now:
/// - one JSON file
/// - one DTO
/// - safe bootstrap creation
///
/// </summary>
public sealed class StatsRepository
{
    private const string FileName = "stats.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task EnsureCreatedAsync()
    {
        var path = GetFilePath();

        if (File.Exists(path))
        {
            return;
        }

        var initial = new StatsData();
        var json = JsonSerializer.Serialize(initial, JsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    public async Task<StatsData> LoadAsync()
    {
        await EnsureCreatedAsync();

        var json = await File.ReadAllTextAsync(GetFilePath());
        return JsonSerializer.Deserialize<StatsData>(json) ?? new StatsData();
    }

    public async Task SaveAsync(StatsData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var json = JsonSerializer.Serialize(data, JsonOptions);
        await File.WriteAllTextAsync(GetFilePath(), json);
    }

    private static string GetFilePath()
        => Path.Combine(FileSystem.AppDataDirectory, FileName);
}
