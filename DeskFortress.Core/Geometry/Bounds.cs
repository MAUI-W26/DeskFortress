namespace DeskFortress.Core.Geometry;

// Simple axis-aligned bounds extracted from shapes.
// Mainly used to derive widths, heights and random sampling ranges.
public readonly record struct Bounds(float MinX, float MinY, float MaxX, float MaxY)
{
    public float Width => MaxX - MinX;
    public float Height => MaxY - MinY;

    public Vec2 Center => new((MinX + MaxX) * 0.5f, (MinY + MaxY) * 0.5f);
}