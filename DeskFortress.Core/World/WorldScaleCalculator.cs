using DeskFortress.Core.Entities;

namespace DeskFortress.Core.World;

// Converts asset-local normalized size into world scale using the background reference.
// This is the step that makes different assets agree on real-world proportions.
public sealed class WorldScaleCalculator
{
    private readonly float _backgroundUnitsPerNormalized;

    public WorldScaleCalculator(AssetScaleProfile backgroundScaleProfile)
    {
        _backgroundUnitsPerNormalized = backgroundScaleProfile.GetWorldUnitsPerNormalizedUnit();
    }

    // Returns the base world scale for an entity before depth perspective is applied.
    public float GetEntityWorldScale(Entity entity)
    {
        var entityUnits = entity.ScaleProfile.GetWorldUnitsPerNormalizedUnit();
        return entityUnits / _backgroundUnitsPerNormalized;
    }
}