namespace DeskFortress.Core.Simulation;

// Input for deferred throw simulation.
// Direction is control intent from UI, while power/loft shape the launch profile.
public readonly record struct ThrowLaunchInput(
    float StartX,
    float StartY,
    float DirectionX,
    float DirectionY,
    float Power,
    float Loft);

// A sampled projectile state used by the UI to animate the fake arc.
public readonly record struct ThrowTrajectorySample(
    float Time,
    float X,
    float Y,
    float Z,
    float Scale);

// Full simulation output: sampled path and resolved impact.
public sealed class ThrowSimulationResult
{
    public required ProjectileImpactResult Impact { get; init; }
    public float FlightTime { get; init; }
    public float ImpactX { get; init; }
    public float ImpactY { get; init; }
    public float ImpactZ { get; init; }
    public IReadOnlyList<ThrowTrajectorySample> Samples { get; init; } = [];
}
