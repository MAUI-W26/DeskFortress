using System.Text.Json.Serialization;

namespace DeskFortress.Core.Assets;

// Raw background JSON model.
// This contains map geometry and the scene scale reference.
public sealed class BackgroundAsset
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("metadata")]
    public AssetMetadata Metadata { get; set; } = new();

    [JsonPropertyName("original_size")]
    public AssetSize OriginalSize { get; set; } = new();

    [JsonPropertyName("spawn_zones")]
    public List<JsonPolygon> SpawnZones { get; set; } = [];

    [JsonPropertyName("floor")]
    public List<JsonPolygon> Floor { get; set; } = [];

    [JsonPropertyName("front_walls")]
    public List<JsonPolygon> FrontWalls { get; set; } = [];

    [JsonPropertyName("back_walls")]
    public List<JsonPolygon> BackWalls { get; set; } = [];

    [JsonPropertyName("decor_objects")]
    public List<JsonPolygon> DecorObjects { get; set; } = [];
}