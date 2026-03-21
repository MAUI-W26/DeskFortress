using DeskFortress.Core.Geometry;
using DeskFortress.Core.World;

namespace DeskFortress.Core.Entities;

// Runtime projectile entity.
// Collision uses one or more local ellipses, though the current model uses the first one.
public sealed class ProjectileEntity : Entity
{
    public ProjectileEntity(AssetScaleProfile scaleProfile) : base(scaleProfile)
    {
    }

    public bool HasHit { get; set; }

    public List<EllipseShape> LocalShapes { get; } = [];
}