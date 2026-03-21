namespace DeskFortress.Core.Geometry;

// Convex or concave polygon used for map regions and hit zones.
// Points are stored in local normalized space until transformed into world space.
public sealed class Polygon
{
    public IReadOnlyList<Vec2> Points { get; }

    public Polygon(IEnumerable<Vec2> points)
    {
        Points = points.ToList();

        if (Points.Count < 3)
        {
            throw new ArgumentException("A polygon must contain at least three points.", nameof(points));
        }
    }

    // Used by metric extraction, collision sampling and utility logic.
    public Bounds GetBounds()
    {
        var minX = Points.Min(p => p.X);
        var minY = Points.Min(p => p.Y);
        var maxX = Points.Max(p => p.X);
        var maxY = Points.Max(p => p.Y);

        return new Bounds(minX, minY, maxX, maxY);
    }
}