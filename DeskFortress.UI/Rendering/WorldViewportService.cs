namespace DeskFortress.UI.Rendering;

/// <summary>
/// Manages the viewport configuration and coordinate transformation from Core's normalized space to UI screen space.
/// The Core simulation works in normalized coordinates [0..1] where entities are positioned relative to background dimensions.
/// This service scales that normalized space to fit the device viewport.
/// </summary>
/// <remarks>
/// COORDINATE SYSTEM ARCHITECTURE:
/// 
/// Core (Normalized Space):
///   - Background is [0..1] in both X and Y
///   - Entities use normalized coordinates (e.g., X=0.5 is center, Y=0.8 is near bottom)
///   - All geometry is normalized relative to background pixel dimensions
/// 
/// UI (Screen Space):
///   - Viewport has actual pixel dimensions (e.g., 1920x1080)
///   - Must scale normalized [0..1] to screen pixels
///   - Preserves aspect ratio by fitting background to viewport height
///   - Allows horizontal scrolling if world is wider than viewport
/// 
/// Transformation:
///   screenX = normalizedX * worldWidth
///   screenY = normalizedY * worldHeight
/// </remarks>
public sealed class WorldViewportService
{
    private double _viewportWidth;
    private double _viewportHeight;

    /// <summary>
    /// Scale factor that converts normalized coordinates to screen pixels.
    /// Calculated to fit the normalized background height [0..1] to the viewport height.
    /// </summary>
    public double Scale { get; private set; }

    /// <summary>
    /// Total world width in screen pixels (may exceed viewport width, enabling scrolling).
    /// </summary>
    public double WorldWidth { get; private set; }

    /// <summary>
    /// Total world height in screen pixels (matches viewport height).
    /// </summary>
    public double WorldHeight { get; private set; }

    /// <summary>
    /// Current horizontal camera offset in screen pixels (for scrolling).
    /// </summary>
    public double CameraX { get; private set; }

    /// <summary>
    /// Configures the viewport and calculates the coordinate transformation scale.
    /// </summary>
    /// <param name="viewportWidth">Device viewport width in pixels</param>
    /// <param name="viewportHeight">Device viewport height in pixels</param>
    /// <param name="backgroundAspectRatio">
    /// The aspect ratio of the normalized background (width/height in normalized space).
    /// Typically this is originalPixelWidth / originalPixelHeight from the background asset.
    /// </param>
    public void Configure(
        double viewportWidth,
        double viewportHeight,
        double backgroundAspectRatio)
    {
        if (viewportWidth <= 0 || viewportHeight <= 0)
            return;

        if (backgroundAspectRatio <= 0)
            throw new InvalidOperationException("Background aspect ratio must be greater than zero.");

        _viewportWidth = viewportWidth;
        _viewportHeight = viewportHeight;

        // The normalized background is 1.0 units tall
        // Scale to make the full height fit the viewport
        Scale = viewportHeight;

        // World height matches viewport (normalized 1.0 = viewport height)
        WorldHeight = viewportHeight;

        // World width based on background aspect ratio
        // If background is 2.73:1 (314.56/115.2), world width = viewport height * 2.73
        WorldWidth = viewportHeight * backgroundAspectRatio;

        // Clamp camera to valid range
        var maxCameraX = GetMaxCameraX();
        CameraX = Math.Clamp(CameraX, 0, maxCameraX);
    }

    /// <summary>
    /// Resets the camera to the leftmost position.
    /// </summary>
    public void ResetCamera()
    {
        CameraX = 0;
    }

    /// <summary>
    /// Pans the camera by a screen-space delta (e.g., from drag gestures).
    /// </summary>
    /// <param name="deltaX">Screen pixel delta (positive = drag right = camera moves left)</param>
    public void PanByScreenDelta(double deltaX)
    {
        CameraX -= deltaX;
        CameraX = Math.Clamp(CameraX, 0, GetMaxCameraX());
    }

    /// <summary>
    /// Converts normalized world coordinates to screen layout coordinates.
    /// </summary>
    /// <param name="normalizedX">Entity X in normalized space [0..1]</param>
    /// <param name="normalizedY">Entity Y in normalized space [0..1]</param>
    /// <param name="normalizedWidth">Entity width in normalized space (after scale is applied)</param>
    /// <param name="normalizedHeight">Entity height in normalized space (after scale is applied)</param>
    /// <returns>Screen-space rectangle for UI layout</returns>
    public Rect NormalizedToScreenRect(
        float normalizedX,
        float normalizedY,
        float normalizedWidth,
        float normalizedHeight)
    {
        // Convert normalized coordinates to screen pixels
        var screenX = normalizedX * WorldWidth;
        var screenY = normalizedY * WorldHeight;
        var screenWidth = normalizedWidth * WorldWidth;
        var screenHeight = normalizedHeight * WorldHeight;

        // UI expects anchor-centered layout:
        // Core gives us the ground contact point (foot position for characters)
        // We need to center horizontally and anchor to bottom
        return new Rect(
            screenX - (screenWidth / 2.0),  // Center horizontally on anchor point
            screenY - screenHeight,          // Anchor to bottom (ground contact)
            screenWidth,
            screenHeight);
    }

    /// <summary>
    /// Maximum horizontal camera offset (enables scrolling to see entire world).
    /// </summary>
    public double GetMaxCameraX()
    {
        return Math.Max(0, WorldWidth - _viewportWidth);
    }

    /// <summary>
    /// Returns the normalized X coordinate (0..1) that corresponds to the horizontal
    /// center of the current viewport window.  Use this to spawn a projectile at the
    /// position the player is actually looking at after camera panning.
    /// </summary>
    public float GetCenterNormalizedX()
    {
        if (WorldWidth <= 0) return 0.5f;
        return (float)Math.Clamp((CameraX + _viewportWidth / 2.0) / WorldWidth, 0.02, 0.98);
    }

    /// <summary>
    /// Converts a screen-space point (in viewport pixels, accounting for camera offset)
    /// into normalized world coordinates [0..1].
    /// </summary>
    public (float nx, float ny) ScreenToNormalized(double viewportX, double viewportY)
    {
        if (WorldWidth <= 0 || WorldHeight <= 0) return (0.5f, 0.5f);
        var worldX = viewportX + CameraX;
        return ((float)(worldX / WorldWidth), (float)(viewportY / WorldHeight));
    }

    /// <summary>
    /// Current viewport width in pixels (available after Configure has been called).
    /// </summary>
    public double ViewportWidth => _viewportWidth;

    /// <summary>
    /// Current viewport height in pixels (available after Configure has been called).
    /// </summary>
    public double ViewportHeight => _viewportHeight;
}