using DeskFortress.Core.Entities;

namespace DeskFortress.Core.Simulation;

// Runs a short-lived ballistic simulation for deferred throw workflows.
// It reuses existing movement/depth and collision systems so behavior matches live projectiles.
public sealed class ThrowPhysicsSystem
{
    private readonly MovementSystem _movementSystem;
    private readonly CollisionSystem _collisionSystem;

    public ThrowPhysicsSystem(MovementSystem movementSystem, CollisionSystem collisionSystem)
    {
        _movementSystem = movementSystem;
        _collisionSystem = collisionSystem;
    }

    public ThrowSimulationResult Simulate(
        ProjectileEntity projectile,
        IEnumerable<CoworkerEntity> coworkers,
        ThrowLaunchInput launch)
    {
        var (dirX, dirY) = NormalizeDirection(launch.DirectionX, launch.DirectionY);
        var power = Math.Clamp(launch.Power, 0.18f, 2.60f);
        var loft = Math.Clamp(launch.Loft, 0.10f, 1.20f);

        // Tuned values for normalized-world gameplay space.
        // Increased ranges: horizontal throws now travel across desk, vertical throws arc over obstacles.
        var clampedLoft = Math.Clamp(loft, 0.10f, 1.2f);
        
        // Horizontal: significantly increased to allow side-to-side throws across full desk
        var horizontalSpeed = 0.18f + (power * 0.48f);
        
        // Vertical: much more impactful, heavily boosted by loft for arcing shots
        var verticalSpeed = 0.22f + (power * (0.30f + (clampedLoft * 0.42f)));
        
        // Gravity: reduced when lofting to allow longer flight arcs
        var gravity = 2.20f + ((1.0f - clampedLoft) * 0.70f);

        // Spawn well ahead and elevated so projectile clears obstacles
        // Throws now start from 0.44Y (off-screen), so they have 0.35+ units to travel before the visible area
        projectile.X = launch.StartX + (dirX * 0.08f);
        projectile.Y = launch.StartY + (dirY * 0.06f);
        projectile.Z = 0.06f;  // Start high to arc over obstacles
        projectile.VX = dirX * horizontalSpeed;
        projectile.VY = dirY * horizontalSpeed;
        projectile.VZ = verticalSpeed;
        projectile.Gravity = gravity;
        projectile.State = ProjectileState.Flying;
        projectile.RestType = ProjectileRestType.None;
        projectile.LandedTimeRemaining = 0f;
        projectile.EmbeddedTimeRemaining = 0f;

        _movementSystem.RefreshDepthAndScale(projectile);

        const float dt = 1f / 120f;
        const float sampleEvery = 1f / 90f;  // More samples for smoother animation
        const float maxFlightTime = 4.5f;  // Longer max flight time to reach farther targets
        // Keep a short grace period so near-front targets still show a meaningful arc segment.
        const float collisionStartTime = 0.15f;

        var samples = new List<ThrowTrajectorySample>(128)
        {
            ToSample(projectile, 0f)
        };

        var time = 0f;
        var nextSampleTime = sampleEvery;

        while (time < maxFlightTime)
        {
            projectile.Integrate(dt);
            _movementSystem.RefreshDepthAndScale(projectile);

            time += dt;
            if (time >= nextSampleTime)
            {
                samples.Add(ToSample(projectile, time));
                nextSampleTime += sampleEvery;
            }

            // Skip collision checks until the projectile has traveled enough to clear the player
            if (time >= collisionStartTime)
            {
                var impact = _collisionSystem.CheckImpact(projectile, coworkers);
                if (impact is not null)
                {
                    samples.Add(ToSample(projectile, time));
                    return new ThrowSimulationResult
                    {
                        Impact = impact,
                        FlightTime = time,
                        ImpactX = projectile.X,
                        ImpactY = projectile.Y,
                        ImpactZ = projectile.Z,
                        Samples = samples
                    };
                }
            }
        }

        var fallbackImpact = new ProjectileImpactResult
        {
            ImpactType = ProjectileImpactType.OutOfBounds
        };

        return new ThrowSimulationResult
        {
            Impact = fallbackImpact,
            FlightTime = time,
            ImpactX = projectile.X,
            ImpactY = projectile.Y,
            ImpactZ = projectile.Z,
            Samples = samples
        };
    }

    private static (float X, float Y) NormalizeDirection(float x, float y)
    {
        var len = MathF.Sqrt((x * x) + (y * y));
        if (len < 0.001f)
        {
            return (0f, -1f);
        }

        return (x / len, y / len);
    }

    private static ThrowTrajectorySample ToSample(ProjectileEntity projectile, float time)
    {
        return new ThrowTrajectorySample(
            Time: time,
            X: projectile.X,
            Y: projectile.Y,
            Z: projectile.Z,
            Scale: projectile.Scale);
    }
}
