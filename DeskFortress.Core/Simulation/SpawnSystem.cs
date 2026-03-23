using DeskFortress.Core.Entities;
using DeskFortress.Core.Geometry;
using DeskFortress.Core.World;

namespace DeskFortress.Core.Simulation;

/// <summary>
/// Handles coworker spawn placement inside background-defined spawn zones.
/// Ensures spawned entities are placed in walkable areas, avoiding blocked zones.
/// </summary>
public sealed class SpawnSystem
{
    private readonly BackgroundMap _map;
    private readonly DepthSystem _depthSystem;
    private readonly MapCollisionSystem _mapCollision;
    private readonly Random _random = new();

    public SpawnSystem(BackgroundMap map, DepthSystem depthSystem, MapCollisionSystem mapCollision)
    {
        _map = map;
        _depthSystem = depthSystem;
        _mapCollision = mapCollision;
    }

    /// <summary>
    /// Spawns a coworker at a valid walkable position within spawn zones.
    /// Validates against floor boundaries and blocking decor objects.
    /// </summary>
    public void SpawnCoworker(CoworkerEntity entity)
    {
        if (_map.SpawnZones.Count == 0)
        {
            throw new InvalidOperationException("No spawn zones are defined in the background map.");
        }

        // Try multiple zones if needed to find valid spawn point
        for (int zoneAttempt = 0; zoneAttempt < _map.SpawnZones.Count * 2; zoneAttempt++)
        {
            var zone = _map.SpawnZones[_random.Next(_map.SpawnZones.Count)];
            var point = FindValidSpawnPoint(zone);

            if (point.HasValue)
            {
                // Found valid spawn point
                entity.X = point.Value.X;
                entity.Y = point.Value.Y;
                entity.Z = 0f;
                entity.VZ = 0f;
                entity.VX = 0f;
                entity.VY = 0f;

                // Initialize depth-based perspective scaling
                entity.Depth = _depthSystem.GetDepth(entity.Y);
                entity.DepthScale = _depthSystem.GetCharacterDepthScale(entity.Y);
                return;
            }
        }

        // Fallback: use first spawn zone's first point (should never happen)
        var fallbackZone = _map.SpawnZones[0];
        var fallbackPoint = fallbackZone.Points[0];
        entity.X = fallbackPoint.X;
        entity.Y = fallbackPoint.Y;
        entity.Z = 0f;
        entity.VZ = 0f;
        entity.VX = 0f;
        entity.VY = 0f;
        entity.Depth = _depthSystem.GetDepth(entity.Y);
        entity.DepthScale = _depthSystem.GetCharacterDepthScale(entity.Y);
    }

    /// <summary>
    /// Finds a valid spawn point inside a polygon that's also walkable (on floor, not blocked).
    /// </summary>
    private Vec2? FindValidSpawnPoint(Polygon zone)
    {
        var bounds = zone.GetBounds();

        // Try up to 100 random points within the zone
        for (var i = 0; i < 100; i++)
        {
            var point = new Vec2(
                RandomRange(bounds.MinX, bounds.MaxX),
                RandomRange(bounds.MinY, bounds.MaxY));

            // Check if point is inside spawn zone polygon
            if (!CollisionHelper.PointInPolygon(point, zone))
                continue;

            // Check if point is on walkable floor (not blocked by decor)
            if (!_mapCollision.CanOccupy(point))
                continue;

            // Valid spawn point found!
            return point;
        }

        // No valid point found in this zone
        return null;
    }

    private float RandomRange(float min, float max)
        => min + ((float)_random.NextDouble() * (max - min));
}