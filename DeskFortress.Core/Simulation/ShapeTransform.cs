using DeskFortress.Core.Entities;
using DeskFortress.Core.Geometry;

namespace DeskFortress.Core.Simulation;

// Converts local normalized shapes into world-space shapes.
// Rendering and collision stay aligned because both use the same final entity scale.
public static class ShapeTransform
{
    public static Polygon ToWorldPolygon(CoworkerEntity entity, Polygon localPolygon)
    {
        var s = entity.Scale;

        return new Polygon(localPolygon.Points.Select(p =>
            new Vec2(
                entity.X + (p.X * s),
                entity.Y + (p.Y * s))));
    }

    public static EllipseShape ToWorldEllipse(CoworkerEntity entity, EllipseShape localEllipse)
    {
        var s = entity.Scale;

        return new EllipseShape(
            new Vec2(
                entity.X + (localEllipse.Center.X * s),
                entity.Y + (localEllipse.Center.Y * s)),
            localEllipse.RadiusX * s,
            localEllipse.RadiusY * s);
    }

    public static EllipseShape ToWorldEllipse(ProjectileEntity entity, EllipseShape localEllipse)
    {
        var s = entity.Scale;

        return new EllipseShape(
            new Vec2(
                entity.X + (localEllipse.Center.X * s),
                entity.Y + (localEllipse.Center.Y * s)),
            localEllipse.RadiusX * s,
            localEllipse.RadiusY * s);
    }
}