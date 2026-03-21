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
        CollisionSystem = new CollisionSystem();
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
        MovementSystem.RefreshDepthAndScale(projectile);
        Projectiles.Add(projectile);
    }

    // Central simulation tick.
    public void Update(float dt)
    {
        UpdateCoworkers(dt);
        UpdateProjectiles(dt);

        Coworkers.RemoveAll(c => !c.IsAlive);
        Projectiles.RemoveAll(p => p.HasHit || IsOutOfBounds(p));
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
        foreach (var projectile in Projectiles.Where(p => !p.HasHit))
        {
            projectile.Integrate(dt);
            MovementSystem.RefreshDepthAndScale(projectile);

            var hit = CollisionSystem.CheckProjectileHit(projectile, Coworkers);
            if (hit is not null)
            {
                projectile.HasHit = true;
            }
        }
    }

    // Simple cleanup bounds outside the normalized play space.
    private static bool IsOutOfBounds(Entity entity)
    {
        return entity.X < -0.25f ||
               entity.X > 1.25f ||
               entity.Y < -0.25f ||
               entity.Y > 1.25f;
    }
}