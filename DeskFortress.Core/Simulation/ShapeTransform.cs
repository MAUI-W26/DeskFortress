using DeskFortress.Core.Entities;
using DeskFortress.Core.Geometry;

namespace DeskFortress.Core.Simulation;

// Converts local normalized shapes into world-space shapes.
// Rendering and collision stay aligned because both use the same final entity scale and anchor.
public static class ShapeTransform
{
    public static Polygon ToWorldPolygon(CoworkerEntity entity, Polygon localPolygon)
    {
        var s = entity.Scale;
        var anchor = entity.AnchorLocal;

        return new Polygon(localPolygon.Points.Select(p =>
            new Vec2(
                entity.RenderX + ((p.X - anchor.X) * s),
                entity.RenderY + ((p.Y - anchor.Y) * s))));
    }

    public static EllipseShape ToWorldEllipse(CoworkerEntity entity, EllipseShape localEllipse)
    {
        var s = entity.Scale;
        var anchor = entity.AnchorLocal;

        return new EllipseShape(
            new Vec2(
                entity.RenderX + ((localEllipse.Center.X - anchor.X) * s),
                entity.RenderY + ((localEllipse.Center.Y - anchor.Y) * s)),
            localEllipse.RadiusX * s,
            localEllipse.RadiusY * s);
    }

    public static EllipseShape ToWorldEllipse(ProjectileEntity entity, EllipseShape localEllipse)
    {
        var s = entity.Scale;
        var anchor = entity.AnchorLocal;

        return new EllipseShape(
            new Vec2(
                entity.RenderX + ((localEllipse.Center.X - anchor.X) * s),
                entity.RenderY + ((localEllipse.Center.Y - anchor.Y) * s)),
            localEllipse.RadiusX * s,
            localEllipse.RadiusY * s);
    }
}