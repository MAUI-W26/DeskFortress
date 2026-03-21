using System.Text.Json.Serialization;

namespace DeskFortress.Core.Assets;

// Raw coworker JSON model.
// Geometry is split by body regions so hit detection can identify zones.
public sealed class CharacterAsset
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("metadata")]
    public AssetMetadata Metadata { get; set; } = new();

    [JsonPropertyName("original_size")]
    public AssetSize OriginalSize { get; set; } = new();

    [JsonPropertyName("head")]
    public List<JsonEllipse> Head { get; set; } = [];

    [JsonPropertyName("chest")]
    public List<JsonPolygon> Chest { get; set; } = [];

    [JsonPropertyName("left_arm")]
    public List<JsonPolygon> LeftArm { get; set; } = [];

    [JsonPropertyName("right_arm")]
    public List<JsonPolygon> RightArm { get; set; } = [];

    [JsonPropertyName("left_leg")]
    public List<JsonPolygon> LeftLeg { get; set; } = [];

    [JsonPropertyName("right_leg")]
    public List<JsonPolygon> RightLeg { get; set; } = [];

    [JsonPropertyName("extra_objects")]
    public List<JsonPolygon> ExtraObjects { get; set; } = [];
}