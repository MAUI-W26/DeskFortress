using DeskFortress.Core.Geometry;
using DeskFortress.Core.World;

namespace DeskFortress.Core.Entities;

// Logical hit zones returned by collision checks.
// This lets gameplay react differently based on where the projectile hit.
public enum HitZoneType
{
    Head,
    Chest,
    LeftArm,
    RightArm,
    LeftLeg,
    RightLeg,
    ExtraObject
}

// One body-part collision shape in local normalized asset space.
// A zone can be defined by a polygon or an ellipse.
public sealed class BodyPartShape
{
    public HitZoneType ZoneType { get; }
    public Polygon? Polygon { get; }
    public EllipseShape? Ellipse { get; }

    public BodyPartShape(HitZoneType zoneType, Polygon polygon)
    {
        ZoneType = zoneType;
        Polygon = polygon;
    }

    public BodyPartShape(HitZoneType zoneType, EllipseShape ellipse)
    {
        ZoneType = zoneType;
        Ellipse = ellipse;
    }
}

// Runtime coworker entity.
// Keeps normalized hit zones and inherits shared transform/motion state from Entity.
public sealed class CoworkerEntity : Entity
{
    public CoworkerEntity(AssetScaleProfile scaleProfile) : base(scaleProfile)
    {
    }

    // These shapes remain local until a collision check needs world-space versions.
    public List<BodyPartShape> LocalShapes { get; } = [];
}