namespace DeskFortress.UI.Assets;

/// <summary>
/// Immutable-ish container returned by the preload service.
/// 
/// It carries the already-read raw JSON blobs plus the UI path registries
/// validated during splash. CoreBootstrapper then converts the raw JSON
/// into typed Core assets and runtime templates.
/// </summary>
public sealed class PreloadedAssets
{
    public required string BackgroundJson { get; init; }

    public required IReadOnlyDictionary<string, string> CharacterJsonByKey { get; init; }

    public required IReadOnlyDictionary<string, string> ProjectileJsonByKey { get; init; }

    public required IReadOnlyDictionary<string, string> CharacterImagePaths { get; init; }

    public required IReadOnlyDictionary<string, string> ProjectileImagePaths { get; init; }

    public required IReadOnlyDictionary<string, string> MenuIllustrationPaths { get; init; }

    public required IReadOnlyDictionary<string, string> MusicPaths { get; init; }

    public required IReadOnlyDictionary<string, string> SfxPaths { get; init; }
}