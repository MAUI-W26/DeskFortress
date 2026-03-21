using DeskFortress.Core.Geometry;

namespace DeskFortress.Core.World;

// Simple name + polygon pair used for decor objects.
// Name is useful for filtering, debugging or future interaction rules.
public sealed class NamedPolygon
{
    public string Name { get; }
    public Polygon Polygon { get; }

    public NamedPolygon(string name, Polygon polygon)
    {
        Name = name;
        Polygon = polygon;
    }
}

// Runtime-ready background model.
// This is the normalized and validated scene geometry used by gameplay systems.
public sealed class BackgroundMap
{
    public IReadOnlyList<Polygon> SpawnZones { get; init; } = [];
    public IReadOnlyList<Polygon> Floor { get; init; } = [];
    public IReadOnlyList<Polygon> FrontWalls { get; init; } = [];
    public IReadOnlyList<Polygon> BackWalls { get; init; } = [];
    public IReadOnlyList<NamedPolygon> DecorObjects { get; init; } = [];

    public AssetScaleProfile ScaleProfile { get; init; } = null!;

    // Depth anchors used by the perspective system.
    public float BackDepthY { get; init; }
    public float FrontDepthY { get; init; }
}