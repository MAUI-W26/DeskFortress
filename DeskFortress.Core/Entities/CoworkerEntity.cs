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
        Health = MaxHealth;
    }

    // These shapes remain local until a collision check needs world-space versions.
    public List<BodyPartShape> LocalShapes { get; } = [];

    public int MaxHealth { get; } = 3;
    public int Health { get; private set; }

    // Frontline blockers stay alive at the front edge and obstruct throws.
    public bool IsCrowdingFront { get; set; }

    // Applies hit-zone damage rules and returns true if this hit killed the coworker.
    public bool ApplyHit(HitZoneType zoneType)
    {
        if (!IsAlive)
            return false;

        var damage = zoneType switch
        {
            HitZoneType.Head => MaxHealth, // headshot instant kill
            HitZoneType.Chest => 2,
            _ => 1
        };

        Health = Math.Max(0, Health - damage);
        if (Health > 0)
            return false;

        IsAlive = false;
        IsCrowdingFront = false;
        return true;
    }
}