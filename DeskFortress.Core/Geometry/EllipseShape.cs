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
        // Normalize all invalid inputs at the boundary (single choke point)

        if (float.IsNaN(radiusX) || float.IsInfinity(radiusX))
            throw new InvalidOperationException("Ellipse radiusX is invalid (NaN/Infinity).");

        if (float.IsNaN(radiusY) || float.IsInfinity(radiusY))
            throw new InvalidOperationException("Ellipse radiusY is invalid (NaN/Infinity).");

        // Enforce positivity globally
        radiusX = MathF.Abs(radiusX);
        radiusY = MathF.Abs(radiusY);

        // Prevent zero-radius degenerates (breaks collision math)
        const float min = 0.0001f;

        if (radiusX < min) radiusX = min;
        if (radiusY < min) radiusY = min;

        Center = center;
        RadiusX = radiusX;
        RadiusY = radiusY;
    }

    public Bounds GetBounds()
        => new(
            Center.X - RadiusX,
            Center.Y - RadiusY,
            Center.X + RadiusX,
            Center.Y + RadiusY);
}