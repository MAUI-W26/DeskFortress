using DeskFortress.Core.Entities;
using DeskFortress.Core.Geometry;
using DeskFortress.Core.World;

namespace DeskFortress.Core.Simulation;

public readonly record struct MoveResult(bool BlockedX, bool BlockedY);

// Handles entity-vs-map occupancy checks.
// Floor allows movement, while blocking decor objects prevent occupancy.
public sealed class MapCollisionSystem
{
    private readonly BackgroundMap _map;

    public MapCollisionSystem(BackgroundMap map)
    {
        _map = map;
    }

    public bool CanOccupy(Vec2 point)
    {
        var insideFloor = _map.Floor.Any(poly => CollisionHelper.PointInPolygon(point, poly));
        if (!insideFloor)
        {
            return false;
        }

        var insideBlockingDecor = _map.DecorObjects
            .Where(obj => obj.BlocksMovement && !obj.IsDestroyed) // tracking destroyed decor objects to allow passthrough
            .Any(obj => CollisionHelper.PointInPolygon(point, obj.Polygon));

        if (insideBlockingDecor)
        {
            return false;
        }

        return true;
    }

    // Resolves movement axis-by-axis so blocked movement can still slide along free axes.
    public MoveResult ResolveMove(Entity entity, float dt)
    {
        var full = new Vec2(entity.X + (entity.VX * dt), entity.Y + (entity.VY * dt));
        if (CanOccupy(full))
        {
            entity.X = full.X;
            entity.Y = full.Y;
            return new MoveResult(BlockedX: false, BlockedY: false);
        }

        var xOnly = new Vec2(entity.X + (entity.VX * dt), entity.Y);
        var yOnly = new Vec2(entity.X, entity.Y + (entity.VY * dt));

        var movedX = false;
        var movedY = false;

        if (CanOccupy(xOnly))
        {
            entity.X = xOnly.X;
            movedX = true;
        }

        if (CanOccupy(yOnly))
        {
            entity.Y = yOnly.Y;
            movedY = true;
        }

        // Simple bounce-back damping when an axis is blocked.
        if (!movedX)
        {
            entity.VX *= -0.85f;
        }

        if (!movedY)
        {
            entity.VY *= -0.20f;
        }

        return new MoveResult(BlockedX: !movedX, BlockedY: !movedY);
    }
}