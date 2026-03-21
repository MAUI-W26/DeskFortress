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
            X = entity.X,
            Y = entity.Y,
            Scale = entity.Scale,
            Depth = entity.Depth
        };
    }
}

// Minimal render payload for the frontend.
// Position and final scale are enough for sprite placement.
public sealed class RenderEntityDto
{
    public Guid Id { get; init; }
    public float X { get; init; }
    public float Y { get; init; }
    public float Scale { get; init; }
    public float Depth { get; init; }
}