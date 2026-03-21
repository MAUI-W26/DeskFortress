namespace DeskFortress.Core.Geometry;

// Central place for "how do we measure this shape" logic.
// Keeps size extraction consistent across metadata resolution and runtime systems.
public static class ShapeMetrics
{
    public static float GetHeight(Polygon polygon) => polygon.GetBounds().Height;
    public static float GetWidth(Polygon polygon) => polygon.GetBounds().Width;

    public static float GetHeight(EllipseShape ellipse) => ellipse.RadiusY * 2f;
    public static float GetWidth(EllipseShape ellipse) => ellipse.RadiusX * 2f;
}