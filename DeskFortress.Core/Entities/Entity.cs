using DeskFortress.Core.Geometry;
using DeskFortress.Core.World;

namespace DeskFortress.Core.Entities;

// Base runtime entity shared by coworkers and projectiles.
// Keeps common transform, motion and scale state in one place.
public abstract class Entity
{
    protected Entity(AssetScaleProfile scaleProfile)
    {
        ScaleProfile = scaleProfile;
    }

    public Guid Id { get; } = Guid.NewGuid();

    // X/Y are ground-plane anchor coordinates.
    // For coworkers this should be the feet contact point.
    public float X { get; set; }
    public float Y { get; set; }

    public float VX { get; set; }
    public float VY { get; set; }

    // Z is vertical altitude above the ground plane.
    // Coworkers keep this at zero while projectiles use it for throw arcs.
    public float Z { get; set; }
    public float VZ { get; set; }
    public float Gravity { get; set; }

    public float Depth { get; set; }

    // BaseScale is the real-world scale derived from metadata.
    public float BaseScale { get; set; } = 1f;

    // DepthScale is the perspective multiplier based on scene Y position.
    public float DepthScale { get; set; } = 1f;

    // Final runtime scale used by rendering and collision transforms.
    public float Scale => BaseScale * DepthScale;

    public AssetScaleProfile ScaleProfile { get; }

    // Local anchor in normalized asset space.
    // This is the pivot that X/Y refers to.
    public Vec2 AnchorLocal { get; set; } = Vec2.Zero;

    public bool IsAlive { get; set; } = true;

    // Airborne visuals keep ground depth perspective but shift the rendered Y upward.
    public float RenderX => X;
    public float RenderY => Y - Z;

    // Converts the local anchor into a scaled world offset.
    public Vec2 GetScaledAnchorOffset() => AnchorLocal * Scale;

    // Basic Euler integration for ground-plane movement.
    public virtual void Integrate(float dt)
    {
        X += VX * dt;
        Y += VY * dt;
    }
}