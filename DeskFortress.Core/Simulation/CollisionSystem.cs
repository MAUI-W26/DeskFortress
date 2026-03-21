using DeskFortress.Core.Entities;
using DeskFortress.Core.Geometry;

namespace DeskFortress.Core.Simulation;

// Result object returned when a projectile hits a coworker.
// It contains both the target and the specific hit zone.
public sealed class ProjectileHitResult //TODO: Add background objects collision cases
{
    public CoworkerEntity Target { get; init; } = null!;
    public HitZoneType ZoneType { get; init; }
}

// Handles projectile-vs-coworker hit detection.
// All tests happen in world space after local shapes are transformed with final runtime scale.
public sealed class CollisionSystem
{
    public ProjectileHitResult? CheckProjectileHit(
        ProjectileEntity projectile,
        IEnumerable<CoworkerEntity> coworkers)
    {
        var projectileWorldShape = GetProjectileWorldShape(projectile);
        var projectileCenter = projectileWorldShape.Center;
        var projectileRadius = MathF.Max(projectileWorldShape.RadiusX, projectileWorldShape.RadiusY);

        foreach (var coworker in coworkers.Where(c => c.IsAlive))
        {
            foreach (var localShape in coworker.LocalShapes)
            {
                var hit = false;

                if (localShape.Polygon is not null)
                {
                    var worldPolygon = ShapeTransform.ToWorldPolygon(coworker, localShape.Polygon);
                    hit = CollisionHelper.CircleIntersectsPolygon(projectileCenter, projectileRadius, worldPolygon);
                }
                else if (localShape.Ellipse is not null)
                {
                    var worldEllipse = ShapeTransform.ToWorldEllipse(coworker, localShape.Ellipse);
                    hit = CollisionHelper.CircleIntersectsEllipse(projectileCenter, projectileRadius, worldEllipse);
                }

                if (!hit)
                {
                    continue;
                }

                projectile.HasHit = true;

                return new ProjectileHitResult
                {
                    Target = coworker,
                    ZoneType = localShape.ZoneType
                };
            }
        }

        return null;
    }

    // Current projectile collision uses the first ellipse as the active collision body.
    private static EllipseShape GetProjectileWorldShape(ProjectileEntity projectile)
    {
        if (projectile.LocalShapes.Count > 0)
        {
            return ShapeTransform.ToWorldEllipse(projectile, projectile.LocalShapes[0]);
        }

        // Fallback keeps the system usable even if the asset is incomplete.
        return new EllipseShape(
            new Vec2(projectile.X, projectile.Y),
            projectile.Scale * 0.5f,
            projectile.Scale * 0.5f);
    }
}