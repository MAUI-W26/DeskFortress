using DeskFortress.Core.Entities;
using DeskFortress.Core.World;

namespace DeskFortress.Core.Simulation;

// Main composition root for runtime world systems.
// This class wires the background, scale model, movement, collisions and entity collections together.
public sealed class GameWorld
{
    public BackgroundMap Map { get; }
    public DepthSystem DepthSystem { get; }
    public WorldScaleCalculator WorldScaleCalculator { get; }

    public List<CoworkerEntity> Coworkers { get; } = [];
    public List<ProjectileEntity> Projectiles { get; } = [];
    public List<WorldEvent> Events { get; } = [];

    public int Score { get; private set; }

    public SpawnSystem SpawnSystem { get; }
    public MovementSystem MovementSystem { get; }
    public MapCollisionSystem MapCollisionSystem { get; }
    public CollisionSystem CollisionSystem { get; }

    public GameWorld(BackgroundMap map)
    {
        Map = map;

        DepthSystem = new DepthSystem(
            map.BackDepthY,
            map.FrontDepthY,
            minCharacterScale: 0.45f,
            frontWallCharacterScale: 1.00f,
            minProjectileScale: 0.50f,
            frontWallProjectileScale: 1.00f);

        WorldScaleCalculator = new WorldScaleCalculator(map.ScaleProfile);
        SpawnSystem = new SpawnSystem(map, DepthSystem);
        MovementSystem = new MovementSystem(DepthSystem);
        MapCollisionSystem = new MapCollisionSystem(map);
        CollisionSystem = new CollisionSystem(map);
    }

    // Applies real-world base scale before the entity enters the simulation.
    public void SpawnCoworker(CoworkerEntity entity)
    {
        entity.BaseScale = WorldScaleCalculator.GetEntityWorldScale(entity);
        SpawnSystem.SpawnCoworker(entity);
        Coworkers.Add(entity);
    }

    // Applies real-world base scale and current depth state before storing the projectile.
    public void AddProjectile(ProjectileEntity projectile)
    {
        projectile.BaseScale = WorldScaleCalculator.GetEntityWorldScale(projectile);
        projectile.State = ProjectileState.Flying;
        projectile.RestType = ProjectileRestType.None;

        MovementSystem.RefreshDepthAndScale(projectile);
        Projectiles.Add(projectile);
    }

    // Utility helper for creating a throw in one place.
    // X/Y is the ground-plane release point and Z is the release altitude.
    public void ThrowProjectile(
        ProjectileEntity projectile,
        float x,
        float y,
        float z,
        float vx,
        float vy,
        float vz,
        float gravity = 2.5f)
    {
        projectile.X = x;
        projectile.Y = y;
        projectile.Z = z;

        projectile.VX = vx;
        projectile.VY = vy;
        projectile.VZ = vz;
        projectile.Gravity = gravity;

        projectile.LandedTimeRemaining = 0f;
        projectile.EmbeddedTimeRemaining = 0f;

        AddProjectile(projectile);
    }

    // Central simulation tick.
    public void Update(float dt)
    {
        Events.Clear();

        UpdateCoworkers(dt);
        UpdateProjectiles(dt);

        // Cleanup expired entities.
        Coworkers.RemoveAll(c => !c.IsAlive);
        Projectiles.RemoveAll(p => p.State == ProjectileState.Expired || !p.IsAlive);
    }

    private void UpdateCoworkers(float dt)
    {
        foreach (var coworker in Coworkers.Where(c => c.IsAlive))
        {
            MapCollisionSystem.ResolveMove(coworker, dt);
            MovementSystem.RefreshDepthAndScale(coworker);
        }
    }

    private void UpdateProjectiles(float dt)
    {
        foreach (var projectile in Projectiles.Where(p => p.IsAlive))
        {
            switch (projectile.State)
            {
                case ProjectileState.Flying:
                    UpdateFlyingProjectile(projectile, dt);
                    break;

                case ProjectileState.Landed:
                    projectile.LandedTimeRemaining -= dt;
                    if (projectile.LandedTimeRemaining <= 0f)
                    {
                        projectile.State = ProjectileState.Expired;
                    }
                    break;

                case ProjectileState.Embedded:
                    projectile.EmbeddedTimeRemaining -= dt;
                    if (projectile.EmbeddedTimeRemaining <= 0f)
                    {
                        projectile.State = ProjectileState.Expired;
                    }
                    break;

                case ProjectileState.Expired:
                    break;
            }
        }
    }

    private void UpdateFlyingProjectile(ProjectileEntity projectile, float dt)
    {
        projectile.Integrate(dt);
        MovementSystem.RefreshDepthAndScale(projectile);

        var impact = CollisionSystem.CheckImpact(projectile, Coworkers);
        if (impact is null)
        {
            return;
        }

        ResolveProjectileImpact(projectile, impact);
    }

    private void ResolveProjectileImpact(ProjectileEntity projectile, ProjectileImpactResult impact)
    {
        Score += impact.ScoreDelta;

        switch (impact.ImpactType)
        {
            case ProjectileImpactType.Coworker:
                projectile.TransitionToEmbedded(ProjectileRestType.Coworker, lingerTime: 0.15f);
                Events.Add(new WorldEvent
                {
                    Type = "projectile_hit_coworker",
                    EntityId = impact.Coworker?.Id,
                    ScoreDelta = impact.ScoreDelta
                });
                break;

            case ProjectileImpactType.DecorObject:
                impact.WorldObject?.ApplyHit();
                projectile.TransitionToLanded(ProjectileRestType.Decor, lingerTime: 0.75f);
                Events.Add(new WorldEvent
                {
                    Type = "projectile_hit_property",
                    TargetName = impact.WorldObject?.Name,
                    ScoreDelta = impact.ScoreDelta
                });
                break;

            case ProjectileImpactType.Wall:
                projectile.TransitionToLanded(ProjectileRestType.Wall, lingerTime: 0.50f);
                Events.Add(new WorldEvent
                {
                    Type = "projectile_hit_wall",
                    TargetName = impact.WorldObject?.Name,
                    ScoreDelta = 0
                });
                break;

            case ProjectileImpactType.Floor:
                projectile.TransitionToLanded(ProjectileRestType.Floor, lingerTime: 1.25f);
                Events.Add(new WorldEvent
                {
                    Type = "projectile_hit_floor",
                    ScoreDelta = 0
                });
                break;

            case ProjectileImpactType.OutOfBounds:
                projectile.State = ProjectileState.Expired;
                projectile.RestType = ProjectileRestType.OutOfBounds;
                Events.Add(new WorldEvent
                {
                    Type = "projectile_out_of_bounds",
                    ScoreDelta = 0
                });
                break;
        }
    }
}