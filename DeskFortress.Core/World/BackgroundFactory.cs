using DeskFortress.Core.Assets;

namespace DeskFortress.Core.World;

// Converts a raw background asset into a runtime-ready background map.
// This is where JSON geometry becomes normalized gameplay geometry.
public static class BackgroundFactory
{
    public static BackgroundMap Create(BackgroundAsset asset)
    {
        var spawnZones = asset.SpawnZones
            .Select(p => AssetNormalizer.NormalizePolygon(p, asset.OriginalSize))
            .ToList();

        var floor = asset.Floor
            .Select(p => AssetNormalizer.NormalizePolygon(p, asset.OriginalSize))
            .ToList();

        var frontWalls = asset.FrontWalls
            .Select((p, index) => new WorldObject(
                name: p.Name ?? $"front_wall_{index}",
                objectType: WorldObjectType.FrontWall,
                polygon: AssetNormalizer.NormalizePolygon(p, asset.OriginalSize),
                blocksMovement: true,
                isHittable: true,
                propertyPenalty: 0,
                durability: 999_999))
            .ToList();

        var backWalls = asset.BackWalls
            .Select((p, index) => new WorldObject(
                name: p.Name ?? $"back_wall_{index}",
                objectType: WorldObjectType.BackWall,
                polygon: AssetNormalizer.NormalizePolygon(p, asset.OriginalSize),
                blocksMovement: false,
                isHittable: false,
                propertyPenalty: 0,
                durability: 999_999))
            .ToList();

        var decorObjects = asset.DecorObjects
            .Select((p, index) => new WorldObject(
                name: p.Name ?? $"decor_{index}",
                objectType: WorldObjectType.Decor,
                polygon: AssetNormalizer.NormalizePolygon(p, asset.OriginalSize),
                blocksMovement: p.BlocksMovement ?? true,
                isHittable: p.Hittable ?? true,
                propertyPenalty: p.PropertyPenalty ?? -1,
                durability: p.Durability ?? 1))
            .ToList();

        if (frontWalls.Count == 0)
        {
            throw new InvalidOperationException("Background asset must contain at least one front wall polygon.");
        }

        if (backWalls.Count == 0)
        {
            throw new InvalidOperationException("Background asset must contain at least one back wall polygon.");
        }

        // Max Y is used as the depth anchor because Y drives perspective in this scene.
        var frontDepthY = frontWalls
            .SelectMany(p => p.Polygon.Points)
            .Max(p => p.Y);

        var backDepthY = backWalls
            .SelectMany(p => p.Polygon.Points)
            .Max(p => p.Y);

        var normalizedMeasure = AssetMeasureResolver.ResolveNormalizedMeasure(asset);
        var scaleProfile = new AssetScaleProfile(
            asset.Metadata.RealMeasure.Type,
            asset.Metadata.RealMeasure.Value,
            normalizedMeasure);

        return new BackgroundMap
        {
            SpawnZones = spawnZones,
            Floor = floor,
            FrontWalls = frontWalls,
            BackWalls = backWalls,
            DecorObjects = decorObjects,
            ScaleProfile = scaleProfile,
            BackDepthY = backDepthY,
            FrontDepthY = frontDepthY
        };
    }
}