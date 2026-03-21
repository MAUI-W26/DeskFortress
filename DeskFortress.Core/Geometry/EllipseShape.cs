namespace DeskFortress.Core.Geometry;

// Ellipse used for rounded collision areas such as heads and projectiles.
// Stored in local normalized space until transformed into world space.
public sealed class EllipseShape
{
    public Vec2 Center { get; }
    public float RadiusX { get; }
    public float RadiusY { get; }

    public EllipseShape(Vec2 center, float radiusX, float radiusY)
    {
        if (radiusX <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusX), "Ellipse radius X must be positive.");
        }

        if (radiusY <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusY), "Ellipse radius Y must be positive.");
        }

        Center = center;
        RadiusX = radiusX;
        RadiusY = radiusY;
    }

    // Used when width, height or extents are needed.
    public Bounds GetBounds()
        => new(
            Center.X - RadiusX,
            Center.Y - RadiusY,
            Center.X + RadiusX,
            Center.Y + RadiusY);
}