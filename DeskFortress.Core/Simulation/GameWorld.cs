using DeskFortress.Core.Entities;
using DeskFortress.Core.World;

namespace DeskFortress.Core.Simulation;

/// <summary>
/// Main composition root for runtime world systems.
/// Orchestrates all game simulation including spawning, movement, collisions, and game state.
/// </summary>
public sealed class GameWorld
{
    public BackgroundMap Map { get; }
    public DepthSystem DepthSystem { get; }
    public WorldScaleCalculator WorldScaleCalculator { get; }

    public List<CoworkerEntity> Coworkers { get; } = [];
    public List<ProjectileEntity> Projectiles { get; } = [];
    public List<WorldEvent> Events { get; } = [];

    public int Score { get; private set; }
    public int CoworkersReachedFront { get; private set; }
    public bool IsGameOver { get; private set; }

    // Systems
    public SpawnSystem SpawnSystem { get; }
    public MovementSystem MovementSystem { get; }
    public MapCollisionSystem MapCollisionSystem { get; }
    public CollisionSystem CollisionSystem { get; }
    public ThrowPhysicsSystem ThrowPhysicsSystem { get; }
    public CoworkerCollisionSystem CoworkerCollisionSystem { get; }
    public CoworkerAI CoworkerAI { get; }
    public WaveSpawnManager WaveSpawnManager { get; }

    // Game rules
    private const int GameOverThreshold = 15;      // Game over when this many reach front
    private const float FrontDangerZone = 0.88f;   // Y threshold for "reached front"

    public GameWorld(BackgroundMap map)
    {
        Map = map;

        DepthSystem = new DepthSystem(
            map.BackDepthY,
            map.FrontDepthY,
            minCharacterScale: 0.30f,
            frontWallCharacterScale: 1.00f,
            minProjectileScale: 0.40f,
            frontWallProjectileScale: 1.00f);

        WorldScaleCalculator = new WorldScaleCalculator(map.ScaleProfile);
        MapCollisionSystem = new MapCollisionSystem(map);
        
        // IMPORTANT: Pass MapCollisionSystem to SpawnSystem for validation
        SpawnSystem = new SpawnSystem(map, DepthSystem, MapCollisionSystem);
        
        MovementSystem = new MovementSystem(DepthSystem);
        CollisionSystem = new CollisionSystem(map);
        ThrowPhysicsSystem = new ThrowPhysicsSystem(MovementSystem, CollisionSystem);
        CoworkerCollisionSystem = new CoworkerCollisionSystem();
        CoworkerAI = new CoworkerAI();
        WaveSpawnManager = new WaveSpawnManager();
    }

    /// <summary>
    /// Spawns a coworker at a random spawn zone with proper initialization.
    /// Immediately applies AI to ensure movement starts.
    /// </summary>
    public void SpawnCoworker(CoworkerEntity entity)
    {
        entity.BaseScale = WorldScaleCalculator.GetEntityWorldScale(entity);
        SpawnSystem.SpawnCoworker(entity);  // Places entity, sets depth

        // Initialize AI movement immediately to prevent standing still
        var speedMultiplier = WaveSpawnManager.GetCurrentSpeedMultiplier();
        CoworkerAI.UpdateMovement(entity, speedMultiplier);

        Coworkers.Add(entity);
    }

    /// <summary>
    /// Adds a projectile to the world with proper scale and state initialization.
    /// </summary>
    public void AddProjectile(ProjectileEntity projectile)
    {
        projectile.BaseScale = WorldScaleCalculator.GetEntityWorldScale(projectile);
        projectile.State = ProjectileState.Flying;
        projectile.RestType = ProjectileRestType.None;

        MovementSystem.RefreshDepthAndScale(projectile);
        Projectiles.Add(projectile);
    }

    /// <summary>
    /// Creates and throws a projectile with initial position and velocity.
    /// </summary>
    public void ThrowProjectile(
        ProjectileEntity projectile,
        float x, float y, float z,
        float vx, float vy, float vz,
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

    /// <summary>
    /// Simulates a throw arc in core space while keeping normal world simulation separate.
    /// The returned samples are intended for a UI-only fake flight animation.
    /// </summary>
    public ThrowSimulationResult SimulateDeferredThrow(
        ProjectileEntity projectile,
        ThrowLaunchInput launch)
    {
        projectile.BaseScale = WorldScaleCalculator.GetEntityWorldScale(projectile);
        return ThrowPhysicsSystem.Simulate(projectile, Coworkers, launch);
    }

    /// <summary>
    /// Applies a previously simulated throw impact into score/events/decor state.
    /// </summary>
    public ProjectileImpactResult ResolveDeferredThrowSimulation(
        ProjectileEntity projectile,
        ThrowSimulationResult simulation)
    {
        projectile.X = simulation.ImpactX;
        projectile.Y = simulation.ImpactY;
        projectile.Z = simulation.ImpactZ;
        projectile.VX = 0f;
        projectile.VY = 0f;
        projectile.VZ = 0f;
        projectile.Gravity = 0f;

        MovementSystem.RefreshDepthAndScale(projectile);
        ResolveProjectileImpact(projectile, simulation.Impact);

        projectile.State = ProjectileState.Expired;
        projectile.IsAlive = false;
        return simulation.Impact;
    }

    /// <summary>
    /// Main simulation update loop.
    /// </summary>
    public void Update(float dt)
    {
        Events.Clear();

        // Handle wave spawning
        if (WaveSpawnManager.ShouldSpawn(dt, Coworkers.Count))
        {
            Events.Add(new WorldEvent { Type = "spawn_requested" });
        }

        UpdateCoworkers(dt);
        UpdateProjectiles(dt);

        // Cleanup dead entities
        Coworkers.RemoveAll(c => !c.IsAlive);
        Projectiles.RemoveAll(p => p.State == ProjectileState.Expired || !p.IsAlive);

        // Check game over condition
        if (CoworkersReachedFront >= GameOverThreshold && !IsGameOver)
        {
            IsGameOver = true;
            Events.Add(new WorldEvent
            {
                Type = "game_over",
                TargetName = "Desk overwhelmed!"
            });
        }
    }

    private void UpdateCoworkers(float dt)
    {
        var speedMultiplier = WaveSpawnManager.GetCurrentSpeedMultiplier();

        foreach (var coworker in Coworkers.Where(c => c.IsAlive).ToList())
        {
            // Update AI with deltaTime for smooth angle changes
            CoworkerAI.UpdateMovement(coworker, speedMultiplier, dt);

            // Apply movement with map collision resolution
            var moveResult = MapCollisionSystem.ResolveMove(coworker, dt);

            if (moveResult.BlockedX)
            {
                CoworkerAI.NotifyWallBounce(coworker.Id, coworker.VX);
            }

            if (moveResult.BlockedY)
            {
                CoworkerAI.NotifyWallBounce(coworker.Id, coworker.VX);
            }

            // Refresh depth-based scaling
            MovementSystem.RefreshDepthAndScale(coworker);

            // Check if reached front
            if (CoworkerAI.HasReachedFront(coworker, FrontDangerZone))
            {
                CoworkerAI.RemoveCoworkerTracking(coworker.Id); // Clean up tracking
                coworker.IsAlive = false;
                CoworkersReachedFront++;
                Events.Add(new WorldEvent
                {
                    Type = "coworker_reached_front",
                    EntityId = coworker.Id
                });
            }
        }

        // Resolve coworker-to-coworker collisions
        CoworkerCollisionSystem.ResolveCollisions(Coworkers, CoworkerAI);
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
                        projectile.State = ProjectileState.Expired;
                    break;

                case ProjectileState.Embedded:
                    projectile.EmbeddedTimeRemaining -= dt;
                    if (projectile.EmbeddedTimeRemaining <= 0f)
                        projectile.State = ProjectileState.Expired;
                    break;
            }
        }
    }

    private void UpdateFlyingProjectile(ProjectileEntity projectile, float dt)
    {
        projectile.Integrate(dt);
        MovementSystem.RefreshDepthAndScale(projectile);

        var impact = CollisionSystem.CheckImpact(projectile, Coworkers);
        if (impact != null)
        {
            ResolveProjectileImpact(projectile, impact);
        }
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