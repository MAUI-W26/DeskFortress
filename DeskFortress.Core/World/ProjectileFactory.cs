using DeskFortress.Core.Assets;
using DeskFortress.Core.Entities;

namespace DeskFortress.Core.World;

// Converts a raw projectile asset into a runtime projectile entity.
// The entity keeps its collision ellipse in normalized local space.
public static class ProjectileFactory
{
    public static ProjectileEntity Create(ProjectileAsset asset)
    {
        var normalizedMeasure = AssetMeasureResolver.ResolveNormalizedMeasure(asset);
        var scaleProfile = new AssetScaleProfile(
            asset.Metadata.RealMeasure.Type,
            asset.Metadata.RealMeasure.Value,
            normalizedMeasure);

        var entity = new ProjectileEntity(scaleProfile);

        foreach (var shape in asset.CollisionShapes)
        {
            entity.LocalShapes.Add(AssetNormalizer.NormalizeEllipse(shape, asset.OriginalSize));
        }

        return entity;
    }
}