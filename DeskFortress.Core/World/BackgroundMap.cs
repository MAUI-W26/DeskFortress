using DeskFortress.Core.Geometry;

namespace DeskFortress.Core.World;

// Runtime-ready background model.
// This is the normalized and validated scene geometry used by gameplay systems.
public sealed class BackgroundMap
{
    public IReadOnlyList<Polygon> SpawnZones { get; init; } = [];
    public IReadOnlyList<Polygon> Floor { get; init; } = [];
    public IReadOnlyList<WorldObject> FrontWalls { get; init; } = [];
    public IReadOnlyList<WorldObject> BackWalls { get; init; } = [];
    public IReadOnlyList<WorldObject> DecorObjects { get; init; } = [];

    public AssetScaleProfile ScaleProfile { get; init; } = null!;

    // Depth anchors used by the perspective system.
    public float BackDepthY { get; init; }
    public float FrontDepthY { get; init; }
}