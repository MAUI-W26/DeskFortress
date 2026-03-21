namespace DeskFortress.Core.Geometry;

// Basic 2D value used for positions, offsets, directions and shape points.
// This is the common coordinate type across the whole core.
public readonly record struct Vec2(float X, float Y)
{
    public static readonly Vec2 Zero = new(0f, 0f);

    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2 operator *(Vec2 v, float s) => new(v.X * s, v.Y * s);
    public static Vec2 operator /(Vec2 v, float s) => new(v.X / s, v.Y / s);

    // Used by movement and collision helpers when a distance is needed.
    public float Length() => MathF.Sqrt((X * X) + (Y * Y));

    // Used when only direction matters and scale must be removed.
    public Vec2 Normalized()
    {
        var len = Length();
        return len <= 0.0001f ? Zero : this / len;
    }

    // Used for projection and closest-point math.
    public static float Dot(Vec2 a, Vec2 b) => (a.X * b.X) + (a.Y * b.Y);
}