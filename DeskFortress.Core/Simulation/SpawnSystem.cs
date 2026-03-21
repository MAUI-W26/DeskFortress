using DeskFortress.Core.Entities;
using DeskFortress.Core.Geometry;
using DeskFortress.Core.World;

namespace DeskFortress.Core.Simulation;

// Handles coworker spawn placement inside background-defined spawn zones.
// Spawned entities also receive an initial movement direction and depth state.
public sealed class SpawnSystem
{
    private readonly BackgroundMap _map;
    private readonly DepthSystem _depthSystem;
    private readonly Random _random = new();

    public SpawnSystem(BackgroundMap map, DepthSystem depthSystem)
    {
        _map = map;
        _depthSystem = depthSystem;
    }

    public void SpawnCoworker(CoworkerEntity entity)
    {
        if (_map.SpawnZones.Count == 0)
        {
            throw new InvalidOperationException("No spawn zones are defined in the background map.");
        }

        var zone = _map.SpawnZones[_random.Next(_map.SpawnZones.Count)];
        var point = RandomPointInsidePolygon(zone);

        entity.X = point.X;
        entity.Y = point.Y;

        // Initial motion values can later be replaced by a behavior system.
        entity.VX = RandomRange(-0.05f, 0.05f);
        entity.VY = RandomRange(0.03f, 0.08f);

        entity.Depth = _depthSystem.GetDepth(entity.Y);
        entity.DepthScale = _depthSystem.GetCharacterDepthScale(entity.Y);
    }

    // Random sampling is retried until a valid point is found in the polygon.
    private Vec2 RandomPointInsidePolygon(Polygon polygon)
    {
        var bounds = polygon.GetBounds();

        for (var i = 0; i < 64; i++)
        {
            var point = new Vec2(
                RandomRange(bounds.MinX, bounds.MaxX),
                RandomRange(bounds.MinY, bounds.MaxY));

            if (CollisionHelper.PointInPolygon(point, polygon))
            {
                return point;
            }
        }

        // Safe fallback to avoid hard failure if sampling misses too many times.
        return polygon.Points[0];
    }

    private float RandomRange(float min, float max)
        => min + ((float)_random.NextDouble() * (max - min));
}