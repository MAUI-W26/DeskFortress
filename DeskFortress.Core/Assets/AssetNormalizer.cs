using DeskFortress.Core.Geometry;

namespace DeskFortress.Core.Assets;

// Converts pixel-based asset coordinates into normalized local space.
// This lets every asset be handled with one consistent scale-independent coordinate system.
public static class AssetNormalizer
{
    public static Vec2 NormalizePoint(JsonPoint point, AssetSize size)
        => new(point.X / size.Width, point.Y / size.Height);

    public static Polygon NormalizePolygon(JsonPolygon polygon, AssetSize size)
        => new(polygon.Points.Select(p => NormalizePoint(p, size)));

    public static EllipseShape NormalizeEllipse(JsonEllipse ellipse, AssetSize size)
        => new(
            NormalizePoint(ellipse.Center, size),
            ellipse.RadiusX / size.Width,
            ellipse.RadiusY / size.Height);
}