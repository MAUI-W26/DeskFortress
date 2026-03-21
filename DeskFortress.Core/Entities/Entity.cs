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

    public float X { get; set; }
    public float Y { get; set; }

    public float VX { get; set; }
    public float VY { get; set; }

    public float Depth { get; set; }

    // BaseScale is the real-world scale derived from metadata.
    public float BaseScale { get; set; } = 1f;

    // DepthScale is the perspective multiplier based on scene Y position.
    public float DepthScale { get; set; } = 1f;

    // Final runtime scale used by rendering and collision transforms.
    public float Scale => BaseScale * DepthScale;

    public AssetScaleProfile ScaleProfile { get; }

    public bool IsAlive { get; set; } = true;

    // Basic Euler integration for movement. (compute new position based on velocity and delta time)
    public void Integrate(float dt)
    {
        X += VX * dt;
        Y += VY * dt;
    }
}