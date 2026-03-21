namespace DeskFortress.Core.Geometry;

// Shared collision helpers used by world, map and projectile hit detection.
// Keeping them here avoids scattering geometry math across gameplay classes.
public static class CollisionHelper
{
    // Standard point-in-polygon test used for floor checks and spawn sampling.
    public static bool PointInPolygon(Vec2 point, Polygon polygon)
    {
        var inside = false;
        var pts = polygon.Points;

        for (int i = 0, j = pts.Count - 1; i < pts.Count; j = i++)
        {
            var pi = pts[i];
            var pj = pts[j];

            var intersect =
                ((pi.Y > point.Y) != (pj.Y > point.Y)) &&
                (point.X < ((pj.X - pi.X) * (point.Y - pi.Y) / ((pj.Y - pi.Y) + 0.000001f)) + pi.X);

            if (intersect)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    // Used by circle-vs-polygon tests to evaluate edge distance.
    public static float DistancePointToSegment(Vec2 point, Vec2 a, Vec2 b)
    {
        var ab = b - a;
        var ap = point - a;

        var abLenSq = Vec2.Dot(ab, ab);
        if (abLenSq <= 0.000001f)
        {
            return (point - a).Length();
        }

        var t = Math.Clamp(Vec2.Dot(ap, ab) / abLenSq, 0f, 1f);
        var closest = a + (ab * t);

        return (point - closest).Length();
    }

    // Used for projectile collisions against polygon hit zones.
    public static bool CircleIntersectsPolygon(Vec2 center, float radius, Polygon polygon)
    {
        if (PointInPolygon(center, polygon))
        {
            return true;
        }

        var pts = polygon.Points;

        for (int i = 0; i < pts.Count; i++)
        {
            var a = pts[i];
            var b = pts[(i + 1) % pts.Count];

            if (DistancePointToSegment(center, a, b) <= radius)
            {
                return true;
            }
        }

        return false;
    }

    // Used for projectile collisions against ellipse hit zones.
    // This approximation is sufficient for the current collision model.
    public static bool CircleIntersectsEllipse(Vec2 circleCenter, float circleRadius, EllipseShape ellipse)
    {
        var dx = (circleCenter.X - ellipse.Center.X) / (ellipse.RadiusX + circleRadius);
        var dy = (circleCenter.Y - ellipse.Center.Y) / (ellipse.RadiusY + circleRadius);

        return ((dx * dx) + (dy * dy)) <= 1f;
    }
}