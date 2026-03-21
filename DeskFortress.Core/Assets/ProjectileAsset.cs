using System.Text.Json.Serialization;

namespace DeskFortress.Core.Assets;

// Raw projectile JSON model.
// The collision shape is also the physical size reference.
public sealed class ProjectileAsset
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("metadata")]
    public AssetMetadata Metadata { get; set; } = new();

    [JsonPropertyName("original_size")]
    public AssetSize OriginalSize { get; set; } = new();

    [JsonPropertyName("collision_shapes")]
    public List<JsonEllipse> CollisionShapes { get; set; } = [];
}