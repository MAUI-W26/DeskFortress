using DeskFortress.Core.Assets;
using DeskFortress.Core.Entities;

namespace DeskFortress.Core.World;

// Converts a raw character asset into a runtime coworker entity.
// This is where JSON body-part geometry becomes normalized hit zones.
public static class CharacterFactory
{
    public static CoworkerEntity Create(CharacterAsset asset)
    {
        var normalizedMeasure = AssetMeasureResolver.ResolveNormalizedMeasure(asset);
        var scaleProfile = new AssetScaleProfile(
            asset.Metadata.RealMeasure.Type,
            asset.Metadata.RealMeasure.Value,
            normalizedMeasure);

        var entity = new CoworkerEntity(scaleProfile);

        foreach (var head in asset.Head)
        {
            entity.LocalShapes.Add(
                new BodyPartShape(
                    HitZoneType.Head,
                    AssetNormalizer.NormalizeEllipse(head, asset.OriginalSize)));
        }

        AddPolygons(entity, asset.Chest, HitZoneType.Chest, asset.OriginalSize);
        AddPolygons(entity, asset.LeftArm, HitZoneType.LeftArm, asset.OriginalSize);
        AddPolygons(entity, asset.RightArm, HitZoneType.RightArm, asset.OriginalSize);
        AddPolygons(entity, asset.LeftLeg, HitZoneType.LeftLeg, asset.OriginalSize);
        AddPolygons(entity, asset.RightLeg, HitZoneType.RightLeg, asset.OriginalSize);
        AddPolygons(entity, asset.ExtraObjects, HitZoneType.ExtraObject, asset.OriginalSize);

        return entity;
    }

    // Shared helper because every polygon hit-zone group is normalized the same way.
    private static void AddPolygons(
        CoworkerEntity entity,
        IEnumerable<JsonPolygon> polygons,
        HitZoneType zoneType,
        AssetSize size)
    {
        foreach (var polygon in polygons)
        {
            entity.LocalShapes.Add(
                new BodyPartShape(
                    zoneType,
                    AssetNormalizer.NormalizePolygon(polygon, size)));
        }
    }
}