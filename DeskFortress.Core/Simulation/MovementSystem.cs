using DeskFortress.Core.Entities;
using DeskFortress.Core.World;

namespace DeskFortress.Core.Simulation;

// Updates entity motion and refreshes perspective-related state.
// Depth and depth-scale must stay current because Y position changes over time.
public sealed class MovementSystem
{
    private readonly DepthSystem _depthSystem;

    public MovementSystem(DepthSystem depthSystem)
    {
        _depthSystem = depthSystem;
    }

    public void Update(Entity entity, float dt)
    {
        entity.Integrate(dt);
        RefreshDepthAndScale(entity);
    }

    // Called after movement and also after direct position changes.
    public void RefreshDepthAndScale(Entity entity)
    {
        entity.Depth = _depthSystem.GetDepth(entity.Y);

        entity.DepthScale = entity switch
        {
            ProjectileEntity => _depthSystem.GetProjectileDepthScale(entity.Y),
            _ => _depthSystem.GetCharacterDepthScale(entity.Y)
        };
    }
}