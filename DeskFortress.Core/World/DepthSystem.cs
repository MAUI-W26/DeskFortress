namespace DeskFortress.Core.World;

// Converts Y position into a depth factor and perspective scale.
// This is what makes entities appear smaller in the back and larger in the front.
public sealed class DepthSystem
{
    public float BackDepthY { get; }
    public float FrontDepthY { get; }

    public float MinCharacterScale { get; }
    public float FrontWallCharacterScale { get; }

    public float MinProjectileScale { get; }
    public float FrontWallProjectileScale { get; }

    public DepthSystem(
        float backDepthY,
        float frontDepthY,
        float minCharacterScale,
        float frontWallCharacterScale,
        float minProjectileScale,
        float frontWallProjectileScale)
    {
        if (frontDepthY <= backDepthY)
        {
            throw new ArgumentException("Front depth Y must be greater than back depth Y.");
        }

        BackDepthY = backDepthY;
        FrontDepthY = frontDepthY;

        MinCharacterScale = minCharacterScale;
        FrontWallCharacterScale = frontWallCharacterScale;

        MinProjectileScale = minProjectileScale;
        FrontWallProjectileScale = frontWallProjectileScale;
    }

    // Returns a normalized depth ratio in the scene.
    public float GetDepth(float y)
    {
        var t = (y - BackDepthY) / (FrontDepthY - BackDepthY);
        return Math.Clamp(t, 0f, 1f);
    }

    // Used by characters so their size follows scene perspective.
    public float GetCharacterDepthScale(float y)
    {
        var depth = GetDepth(y);
        return MinCharacterScale + ((FrontWallCharacterScale - MinCharacterScale) * depth);
    }

    // Used by projectiles so thrown objects also follow the same perspective model.
    public float GetProjectileDepthScale(float y)
    {
        var depth = GetDepth(y);
        return MinProjectileScale + ((FrontWallProjectileScale - MinProjectileScale) * depth);
    }
}