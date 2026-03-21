using System.Text.Json;

namespace DeskFortress.Core.Assets;

// JSON loader for raw asset definitions.
// Centralizing deserialization keeps options and error handling consistent.
public static class AssetLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static T LoadFromJson<T>(string json) where T : class
    {
        var result = JsonSerializer.Deserialize<T>(json, Options);

        if (result is null)
        {
            throw new InvalidOperationException($"Failed to deserialize asset as {typeof(T).Name}.");
        }

        return result;
    }
}