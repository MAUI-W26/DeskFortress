using DeskFortress.Core.Entities;
using DeskFortress.Core.World;

namespace DeskFortress.Core.Simulation;

// What kind of thing the projectile impacted.
public enum ProjectileImpactType
{
    None,
    Coworker,
    DecorObject,
    Wall,
    Floor,
    OutOfBounds
}

// Full collision/impact result for projectile resolution.
// This unifies target hits and world impacts into one model.
public sealed class ProjectileImpactResult
{
    public ProjectileImpactType ImpactType { get; init; }
    public CoworkerEntity? Coworker { get; init; }
    public HitZoneType? ZoneType { get; init; }
    public WorldObject? WorldObject { get; init; }
    public int ScoreDelta { get; init; }
}

// Small event payload exposed by the world after update.
// This lets gameplay/UI react without duplicating collision logic.
public sealed class WorldEvent
{
    public string Type { get; init; } = string.Empty;
    public Guid? EntityId { get; init; }
    public string? TargetName { get; init; }
    public int ScoreDelta { get; init; }
}