using DeskFortress.Core.World;

namespace DeskFortress.UI.Rendering;

/// <summary>
/// Represents a single renderable item in a frame.
/// Pairs Core's normalized render data (DTO) with the UI-specific image path.
/// </summary>
public sealed class RenderFrameItem
{
    /// <summary>
    /// Render data from Core containing normalized position, scale, and depth.
    /// </summary>
    public required RenderEntityDto Dto { get; init; }

    /// <summary>
    /// File path to the visual asset for this entity.
    /// </summary>
    public required string ImagePath { get; init; }
}