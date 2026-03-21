namespace DeskFortress.Core.World;

// Stores the relationship between a real-world measure and its normalized local counterpart.
// This is what allows the engine to convert asset-local size into world size.
public sealed class AssetScaleProfile
{
    public string MeasureType { get; }
    public float RealValue { get; }
    public float NormalizedMeasure { get; }

    public AssetScaleProfile(string measureType, float realValue, float normalizedMeasure)
    {
        if (string.IsNullOrWhiteSpace(measureType))
        {
            throw new ArgumentException("Measure type is required.", nameof(measureType));
        }

        if (realValue <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(realValue), "Real value must be positive.");
        }

        if (normalizedMeasure <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(normalizedMeasure), "Normalized measure must be positive.");
        }

        MeasureType = measureType;
        RealValue = realValue;
        NormalizedMeasure = normalizedMeasure;
    }

    // Returns world units represented by one local normalized unit.
    public float GetWorldUnitsPerNormalizedUnit() => RealValue / NormalizedMeasure;
}