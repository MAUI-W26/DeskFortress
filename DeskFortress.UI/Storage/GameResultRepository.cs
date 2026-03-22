using System.Text.Json;

namespace DeskFortress.UI.Storage;

/// <summary>
/// Stores per-session game results.
/// Later, the results page can read this file to display history.
/// </summary>
public sealed class GameResultRepository
{
    private const string FileName = "results.json";

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

        var json = JsonSerializer.Serialize(new List<GameResultRecord>(), JsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    public async Task<IReadOnlyList<GameResultRecord>> LoadAllAsync()
    {
        await EnsureCreatedAsync();

        var json = await File.ReadAllTextAsync(GetFilePath());
        return JsonSerializer.Deserialize<List<GameResultRecord>>(json) ?? new List<GameResultRecord>();
    }

    public async Task AppendAsync(GameResultRecord result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var all = (await LoadAllAsync()).ToList();
        all.Add(result);

        var json = JsonSerializer.Serialize(all, JsonOptions);
        await File.WriteAllTextAsync(GetFilePath(), json);
    }

    private static string GetFilePath()
        => Path.Combine(FileSystem.AppDataDirectory, FileName);
}
