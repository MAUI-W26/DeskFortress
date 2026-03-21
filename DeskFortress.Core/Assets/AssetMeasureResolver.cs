using DeskFortress.Core.Geometry;

namespace DeskFortress.Core.Assets;

// Resolves the local normalized measure that corresponds to a real-world value.
// This is the bridge between JSON metadata and actual world scaling.
public static class AssetMeasureResolver
{
    // Background uses a measurable polygon, typically a wall, as the scene scale reference.
    public static float ResolveNormalizedMeasure(BackgroundAsset asset)
    {
        Validate(asset.Metadata);

        var measure = asset.Metadata.RealMeasure;

        if (measure.Source.Kind != "shape")
        {
            throw new InvalidOperationException("Background must use a shape-based real measure source.");
        }

        var polygon = ResolveBackgroundPolygonGroup(asset, measure.Source.Group, measure.Source.Index);
        var normalizedPolygon = AssetNormalizer.NormalizePolygon(polygon, asset.OriginalSize);

        return measure.Source.Axis switch
        {
            "x" or "width" => ShapeMetrics.GetWidth(normalizedPolygon),
            "y" or "height" => ShapeMetrics.GetHeight(normalizedPolygon),
            _ => throw new InvalidOperationException($"Unsupported background axis '{measure.Source.Axis}'.")
        };
    }

    // Characters are always cropped from top of head to feet.
    // That makes normalized height constant and removes the need for extra resolution logic.
    public static float ResolveNormalizedMeasure(CharacterAsset asset)
    {
        Validate(asset.Metadata);
        return 1f;
    }

    // Projectiles use their collision ellipse as the physical measure source.
    public static float ResolveNormalizedMeasure(ProjectileAsset asset)
    {
        Validate(asset.Metadata);

        var measure = asset.Metadata.RealMeasure;

        if (measure.Source.Kind != "shape" || measure.Source.Group != "collision_shapes")
        {
            throw new InvalidOperationException("Projectile must use 'collision_shapes' as real measure source.");
        }

        if (measure.Source.Index is null || measure.Source.Index.Value < 0 || measure.Source.Index.Value >= asset.CollisionShapes.Count)
        {
            throw new InvalidOperationException("Projectile shape measure index is invalid.");
        }

        var ellipse = asset.CollisionShapes[measure.Source.Index.Value];
        var normalizedEllipse = AssetNormalizer.NormalizeEllipse(ellipse, asset.OriginalSize);

        return measure.Source.Axis switch
        {
            "x" or "width" or "diameter" => ShapeMetrics.GetWidth(normalizedEllipse),
            "y" or "height" => ShapeMetrics.GetHeight(normalizedEllipse),
            _ => throw new InvalidOperationException($"Unsupported projectile axis '{measure.Source.Axis}'.")
        };
    }

    public static Vec2 ResolveCharacterAnchor(CharacterAsset asset)
    {
        Validate(asset.Metadata);

        // Explicit metadata anchor wins.
        if (asset.Metadata.Anchor is not null)
        {
            return AssetNormalizer.NormalizeAnchor(asset.Metadata.Anchor, asset.OriginalSize);
        }

        // Default fallback keeps older assets working.
        // This represents a simple bottom-center feet anchor.
        return new Vec2(0.5f, 1f);
    }

    private static JsonPolygon ResolveBackgroundPolygonGroup(BackgroundAsset asset, string? group, int? index)
    {
        if (group is null || index is null)
        {
            throw new InvalidOperationException("Background real measure source must define group and index.");
        }

        var list = group switch
        {
            "spawn_zones" => asset.SpawnZones,
            "floor" => asset.Floor,
            "front_walls" => asset.FrontWalls,
            "back_walls" => asset.BackWalls,
            "decor_objects" => asset.DecorObjects,
            _ => throw new InvalidOperationException($"Unknown background group '{group}'.")
        };

        if (index.Value < 0 || index.Value >= list.Count)
        {
            throw new InvalidOperationException($"Index {index.Value} is out of range for group '{group}'.");
        }

        return list[index.Value];
    }

    // Minimal guard to fail fast on invalid metadata before scale math starts.
    private static void Validate(AssetMetadata metadata)
    {
        if (!string.Equals(metadata.Units.Space, "px", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Asset metadata units.space must be 'px'.");
        }

        if (metadata.RealMeasure.Value <= 0f)
        {
            throw new InvalidOperationException("Asset real measure value must be positive.");
        }
    }
}