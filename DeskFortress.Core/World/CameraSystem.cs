namespace DeskFortress.Core.World;

// Small helper for camera pan over a normalized world.
// This keeps viewport math out of rendering and gameplay systems.
public sealed class CameraSystem
{
    public float WorldWidth { get; } = 1f;
    public float WorldHeight { get; } = 1f;

    public float ViewportWidth { get; private set; }
    public float ViewportHeight { get; private set; }

    public float PanX { get; private set; }
    public float MinPanX { get; private set; }
    public float MaxPanX { get; private set; }

    public void Configure(float viewportWidth, float viewportHeight)
    {
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;

        if (viewportWidth <= 0f || viewportHeight <= 0f)
        {
            MinPanX = 0f;
            MaxPanX = 0f;
            PanX = 0f;
            return;
        }

        var scaleToFitHeight = viewportHeight / WorldHeight;
        var visibleWorldWidth = viewportWidth / scaleToFitHeight;

        MinPanX = 0f;
        MaxPanX = Math.Max(0f, WorldWidth - visibleWorldWidth);

        PanX = Math.Clamp(PanX, MinPanX, MaxPanX);
    }

    // Lets UI or gameplay place the camera using a simple normalized value.
    public void SetPanNormalized(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        PanX = MinPanX + ((MaxPanX - MinPanX) * t);
    }

    // Used for subtle follow effects without exposing internal range math.
    public void ApplyTiltOffset(float normalizedOffset, float factor = 0.2f)
    {
        var range = MaxPanX - MinPanX;
        var delta = normalizedOffset * range * factor;
        PanX = Math.Clamp(PanX + delta, MinPanX, MaxPanX);
    }
}