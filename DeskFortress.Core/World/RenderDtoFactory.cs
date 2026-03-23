using DeskFortress.Core.Entities;

namespace DeskFortress.Core.World;

/// <summary>
/// Minimal render payload for the frontend.
/// </summary>
public sealed class RenderEntityDto
{
    public Guid Id { get; init; }
    public float GroundX { get; init; }
    public float GroundY { get; init; }
    public float RenderX { get; init; }
    public float RenderY { get; init; }
    public float Scale { get; init; }
    public float Depth { get; init; }
    public float Altitude { get; init; }
    
    /// <summary>
    /// True if entity should be hidden by front wall occlusion.
    /// </summary>
    public bool ShouldBeOccluded { get; init; }
}

/// <summary>
/// Produces minimal render data for UI.
/// Includes occlusion information for proper depth masking.
/// </summary>
public static class RenderDtoFactory
{
    /// <summary>
    /// Creates a render DTO from an entity.
    /// </summary>
    /// <param name="entity">The entity to convert</param>
    /// <param name="occlusionThresholdY">Y threshold below which entities are occluded</param>
    public static RenderEntityDto ToDto(Entity entity, float occlusionThresholdY = 0.55f)
    {
        return new RenderEntityDto
        {
            Id = entity.Id,
            GroundX = entity.X,
            GroundY = entity.Y,
            RenderX = entity.RenderX,
            RenderY = entity.RenderY,
            Scale = entity.Scale,
            Depth = entity.Depth,
            Altitude = entity.Z,
            // Occlude entities behind the center wall structure
            ShouldBeOccluded = entity.Y < occlusionThresholdY
        };
    }
}