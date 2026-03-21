using DeskFortress.Core.Geometry;
using DeskFortress.Core.World;

namespace DeskFortress.Core.Entities;

// High-level projectile lifecycle.
// This replaces the old single HasHit flag.
public enum ProjectileState
{
    Flying,
    Landed,
    Embedded,
    Expired
}

// What the projectile is currently attached to or resting against.
public enum ProjectileRestType
{
    None,
    Floor,
    Coworker,
    Decor,
    Wall,
    OutOfBounds
}

// Runtime projectile entity.
// Collision uses one or more local ellipses, though the current model uses the first one.
public sealed class ProjectileEntity : Entity
{
    public ProjectileEntity(AssetScaleProfile scaleProfile) : base(scaleProfile)
    {
    }

    public ProjectileState State { get; set; } = ProjectileState.Flying;
    public ProjectileRestType RestType { get; set; } = ProjectileRestType.None;

    public float LandedTimeRemaining { get; set; }
    public float EmbeddedTimeRemaining { get; set; }

    public List<EllipseShape> LocalShapes { get; } = [];

    // Basic throw-physics integration.
    // Horizontal motion stays on the floor plane while Z handles altitude.
    public override void Integrate(float dt)
    {
        base.Integrate(dt);
        // add gravity effects
        VZ -= Gravity * dt;
        Z += VZ * dt;
    }

    public void TransitionToLanded(ProjectileRestType restType, float lingerTime)
    {
        State = ProjectileState.Landed;
        RestType = restType;

        Z = 0f;
        VZ = 0f;
        VX = 0f;
        VY = 0f;

        LandedTimeRemaining = Math.Max(0f, lingerTime);
    }

    public void TransitionToEmbedded(ProjectileRestType restType, float lingerTime)
    {
        State = ProjectileState.Embedded;
        RestType = restType;

        VX = 0f;
        VY = 0f;
        VZ = 0f;

        EmbeddedTimeRemaining = Math.Max(0f, lingerTime);
    }
}