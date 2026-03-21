using DeskFortress.Core.Entities;

namespace DeskFortress.Core.World;

// Produces the minimal data the UI needs to draw entities.
// UI should not receive asset metadata or geometry-resolution logic.
public static class RenderDtoFactory
{
    public static RenderEntityDto ToDto(Entity entity)
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
            Altitude = entity.Z
        };
    }
}

// Minimal render payload for the frontend.
// Ground position is useful for gameplay overlays while render position is what the UI should draw.
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
}