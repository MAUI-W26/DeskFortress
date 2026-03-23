using DeskFortress.Core.Entities;
using DeskFortress.Core.Geometry;
using DeskFortress.Core.World;

namespace DeskFortress.Core.Simulation;

// Handles projectile-vs-coworker and projectile-vs-world impact detection.
// All tests happen in world space after local shapes are transformed with final runtime scale.
public sealed class CollisionSystem
{
    private readonly BackgroundMap _map;

    public CollisionSystem(BackgroundMap map)
    {
        _map = map;
    }

    public ProjectileImpactResult? CheckImpact(
        ProjectileEntity projectile,
        IEnumerable<CoworkerEntity> coworkers)
    {
        if (IsOutOfBounds(projectile))
        {
            return new ProjectileImpactResult
            {
                ImpactType = ProjectileImpactType.OutOfBounds
            };
        }

        var projectileWorldShape = GetProjectileWorldShape(projectile);
        var projectileCenter = projectileWorldShape.Center;
        var projectileRadius = MathF.Max(projectileWorldShape.RadiusX, projectileWorldShape.RadiusY);

        // Flying projectiles can strike coworkers before floor impact resolution.
        if (projectile.State == ProjectileState.Flying)
        {
            var coworkerHit = CheckCoworkerHit(projectileCenter, projectileRadius, coworkers);
            if (coworkerHit is not null)
            {
                return coworkerHit;
            }

            var decorHit = CheckDecorImpact(projectileCenter, projectileRadius);
            if (decorHit is not null)
            {
                return decorHit;
            }

            var wallHit = CheckWallImpact(projectileCenter, projectileRadius);
            if (wallHit is not null)
            {
                return wallHit;
            }

            if (projectile.Z <= 0f)
            {
                return new ProjectileImpactResult
                {
                    ImpactType = ProjectileImpactType.Floor
                };
            }
        }

        return null;
    }

    // Resolves an impact at a final ground position without simulating in-flight steps.
    // This is used by the UI-driven fake arc flow.
    public ProjectileImpactResult CheckImpactAtPoint(
        ProjectileEntity projectile,
        IEnumerable<CoworkerEntity> coworkers,
        float impactX,
        float impactY)
    {
        projectile.X = impactX;
        projectile.Y = impactY;
        projectile.Z = 0f;

        if (IsOutOfBounds(projectile))
        {
            return new ProjectileImpactResult
            {
                ImpactType = ProjectileImpactType.OutOfBounds
            };
        }

        var projectileWorldShape = GetProjectileWorldShape(projectile);
        var projectileCenter = projectileWorldShape.Center;
        var projectileRadius = MathF.Max(projectileWorldShape.RadiusX, projectileWorldShape.RadiusY);

        var coworkerHit = CheckCoworkerHit(projectileCenter, projectileRadius, coworkers);
        if (coworkerHit is not null)
        {
            return coworkerHit;
        }

        var decorHit = CheckDecorImpact(projectileCenter, projectileRadius);
        if (decorHit is not null)
        {
            return decorHit;
        }

        var wallHit = CheckWallImpact(projectileCenter, projectileRadius);
        if (wallHit is not null)
        {
            return wallHit;
        }

        return new ProjectileImpactResult
        {
            ImpactType = ProjectileImpactType.Floor
        };
    }

    private ProjectileImpactResult? CheckCoworkerHit(
        Vec2 projectileCenter,
        float projectileRadius,
        IEnumerable<CoworkerEntity> coworkers)
    {
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

                if (coworker.IsCrowdingFront)
                {
                    return new ProjectileImpactResult
                    {
                        ImpactType = ProjectileImpactType.CrowdBlocker,
                        Coworker = coworker,
                        ZoneType = localShape.ZoneType,
                        ScoreDelta = 0
                    };
                }

                return new ProjectileImpactResult
                {
                    ImpactType = ProjectileImpactType.Coworker,
                    Coworker = coworker,
                    ZoneType = localShape.ZoneType,
                    ScoreDelta = GetCoworkerHitScore(localShape.ZoneType)
                };
            }
        }

        return null;
    }

    private ProjectileImpactResult? CheckDecorImpact(Vec2 projectileCenter, float projectileRadius)
    {
        foreach (var decor in _map.DecorObjects.Where(d => d.IsHittable && !d.IsDestroyed))
        {
            if (!CollisionHelper.CircleIntersectsPolygon(projectileCenter, projectileRadius, decor.Polygon))
            {
                continue;
            }

            return new ProjectileImpactResult
            {
                ImpactType = ProjectileImpactType.DecorObject,
                WorldObject = decor,
                ScoreDelta = decor.PropertyPenalty
            };
        }

        return null;
    }

    private ProjectileImpactResult? CheckWallImpact(Vec2 projectileCenter, float projectileRadius)
    {
        foreach (var wall in _map.FrontWalls)
        {
            if (!CollisionHelper.CircleIntersectsPolygon(projectileCenter, projectileRadius, wall.Polygon))
            {
                continue;
            }

            return new ProjectileImpactResult
            {
                ImpactType = ProjectileImpactType.Wall,
                WorldObject = wall,
                ScoreDelta = 0
            };
        }

        return null;
    }

    private static EllipseShape GetProjectileWorldShape(ProjectileEntity projectile)
    {
        if (projectile.LocalShapes.Count > 0)
        {
            return ShapeTransform.ToWorldEllipse(projectile, projectile.LocalShapes[0]);
        }

        // Fallback keeps the system usable even if the asset is incomplete.
        return new EllipseShape(
            new Vec2(projectile.RenderX, projectile.RenderY),
            projectile.Scale * 0.5f,
            projectile.Scale * 0.5f);
    }

    private static int GetCoworkerHitScore(HitZoneType zoneType) // TODO: migrate to config file
        => zoneType switch
        {
            HitZoneType.Head => 3,
            HitZoneType.Chest => 2,
            _ => 1
        };

    private static bool IsOutOfBounds(Entity entity)
    {
        return entity.X < -0.25f ||
               entity.X > 1.25f ||
               entity.Y < -0.25f ||
               entity.Y > 1.25f ||
               entity.RenderY < -0.50f;
    }
}