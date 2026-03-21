using System.Text.Json.Serialization;

namespace DeskFortress.Core.Assets;

// Raw JSON DTO for source texture size in pixels.
// This is used as the base reference for normalization.
public sealed class AssetSize
{
    public float Width { get; set; }
    public float Height { get; set; }
}

// Raw JSON metadata wrapper.
// This holds unit information, real-world measurement reference and local anchor data.
public sealed class AssetMetadata
{
    [JsonPropertyName("units")]
    public AssetUnits Units { get; set; } = new();

    [JsonPropertyName("real_measure")]
    public AssetRealMeasure RealMeasure { get; set; } = new();

    [JsonPropertyName("anchor")]
    public AssetAnchor? Anchor { get; set; } // Optional, as not all assets may define an anchor point.
}

// Declares the source space unit and the real-world comparison unit.
public sealed class AssetUnits
{
    [JsonPropertyName("space")]
    public string Space { get; set; } = string.Empty;

    [JsonPropertyName("real_world")]
    public string RealWorld { get; set; } = string.Empty;
}

// Declares what real-world measure is known for the asset.
// Example: wall height = 8ft, coworker height = 5.5ft, projectile diameter = 0.25ft.
public sealed class AssetRealMeasure
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public AssetMeasureSource Source { get; set; } = new();

    [JsonPropertyName("value")]
    public float Value { get; set; }
}

// Describes where the measurable size comes from inside the asset.
// Background/projectile use shape references. Character uses bounds by convention.
public sealed class AssetMeasureSource
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("group")]
    public string? Group { get; set; }

    [JsonPropertyName("index")]
    public int? Index { get; set; }

    [JsonPropertyName("axis")]
    public string Axis { get; set; } = string.Empty;
}

// Explicit asset anchor in normalized local sprite space.
// Coworkers should define the foot contact point here.
public sealed class AssetAnchor
{
    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }
}

// Raw JSON point in pixel coordinates.
public sealed class JsonPoint
{
    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }
}

// Raw JSON ellipse definition in pixel coordinates.
public sealed class JsonEllipse
{
    [JsonPropertyName("shape")]
    public string Shape { get; set; } = string.Empty;

    [JsonPropertyName("center")]
    public JsonPoint Center { get; set; } = new();

    [JsonPropertyName("radius_x")]
    public float RadiusX { get; set; }

    [JsonPropertyName("radius_y")]
    public float RadiusY { get; set; }
}

// Raw JSON polygon definition in pixel coordinates.
public sealed class JsonPolygon
{
    [JsonPropertyName("shape")]
    public string Shape { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("points")]
    public List<JsonPoint> Points { get; set; } = [];

    // Optional gameplay flags for decor/world objects.
    // Defaults are applied by the factory when absent.
    [JsonPropertyName("hittable")]
    public bool? Hittable { get; set; }

    [JsonPropertyName("blocks_movement")]
    public bool? BlocksMovement { get; set; }

    [JsonPropertyName("property_penalty")]
    public int? PropertyPenalty { get; set; }

    [JsonPropertyName("durability")]
    public int? Durability { get; set; }
}