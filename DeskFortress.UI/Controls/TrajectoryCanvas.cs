using Microsoft.Maui.Graphics;

namespace DeskFortress.UI.Controls;

/// <summary>
/// Draws the throw-preview arc while the player is dragging the paperball.
///
/// Coordinate convention (matches screen space used by the GraphicsView):
///   • BallCenter  – screen position of the paperball at the moment of the last drag update.
///   • SwipeDeltaX / SwipeDeltaY – cumulative finger displacement from drag start (pixels).
///     Negative SwipeDeltaY = finger moved upward = throw directed toward the back of the desk.
///   • Power – normalised throw strength [0..2.5].
///
/// This control intentionally does NOT predict impact location.
/// It only visualizes angle and strength near the bottom of the screen.
/// </summary>
public sealed class ThrowTrajectoryDrawable : IDrawable
{
    // ── State set by GamePage each Running frame ──────────────────────────────

    /// <summary>Screen position of the paperball centre (within the GraphicsView).</summary>
    public PointF BallCenter { get; set; }

    /// <summary>Cumulative horizontal drag displacement in screen pixels.</summary>
    public float SwipeDeltaX { get; set; }

    /// <summary>Cumulative vertical drag displacement in screen pixels (negative = up).</summary>
    public float SwipeDeltaY { get; set; }

    /// <summary>Throw power in [0..2.5] – controls arc height and reach.</summary>
    public float Power { get; set; }

    // ── IDrawable ────────────────────────────────────────────────────────────

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Power <= 0.01f)
            return;

        float dx = SwipeDeltaX;
        float dy = SwipeDeltaY;
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        if (dist < 5f) return;

        float nx = dx / dist;
        float ny = dy / dist;

        // Compact control length so this UI remains a directional input guide,
        // not a fake landing indicator.
        float controlLength = Math.Clamp((dist * 0.55f) + (Power * 10f), 24f, 130f);
        var endX = BallCenter.X + (nx * controlLength);
        var endY = BallCenter.Y + (ny * controlLength);

        float perpX = -ny;   // perpendicular direction
        float perpY =  nx;

        if (perpY > 0) { perpX = -perpX; perpY = -perpY; }

        float arcHeight = Math.Clamp(12f + (Power * 12f), 10f, 42f);

        const int Steps = 10;
        var dots = new PointF[Steps + 1];

        for (int i = 0; i <= Steps; i++)
        {
            float t = i / (float)Steps;

            // Linear interpolation along throw direction
            float px = BallCenter.X + (endX - BallCenter.X) * t;
            float py = BallCenter.Y + (endY - BallCenter.Y) * t;

            // Parabolic lift: peaks at t=0.5, zero at t=0 and t=1
            float lift = arcHeight * 4f * t * (1f - t);

            dots[i] = new PointF(px + perpX * lift, py + perpY * lift);
        }

        for (int i = 0; i <= Steps; i++)
        {
            float t = i / (float)Steps;
            float alpha = 0.85f - (t * 0.45f);
            float dotR = 2.5f + (t * 2.2f);

            canvas.FillColor = Color.FromRgba(1f, 0.95f, 0.2f, alpha);
            canvas.FillCircle(dots[i], dotR);
        }

        // Power bar alongside the drag line (thin coloured stripe)
        var barColor = Power < 1f
            ? Color.FromRgba(0.2f, 0.8f, 0.2f, 0.7f)
            : Power < 2f
                ? Color.FromRgba(1f, 0.7f, 0f, 0.7f)
                : Color.FromRgba(1f, 0.15f, 0.1f, 0.7f);

        canvas.StrokeColor = barColor;
        canvas.StrokeSize = 3f;
        canvas.DrawLine(BallCenter.X, BallCenter.Y, endX, endY);

        // Small directional arrow at line tip.
        float tipSize = 8f;
        float leftX = endX - (nx * tipSize) + (perpX * (tipSize * 0.7f));
        float leftY = endY - (ny * tipSize) + (perpY * (tipSize * 0.7f));
        float rightX = endX - (nx * tipSize) - (perpX * (tipSize * 0.7f));
        float rightY = endY - (ny * tipSize) - (perpY * (tipSize * 0.7f));
        canvas.DrawLine(endX, endY, leftX, leftY);
        canvas.DrawLine(endX, endY, rightX, rightY);
    }
}