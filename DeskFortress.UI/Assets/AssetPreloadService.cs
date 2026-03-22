namespace DeskFortress.UI.Assets;

/// <summary>
/// Reads packaged raw assets once during splash.
/// 
/// Important:
/// This service only touches MAUI packaged raw assets.
/// It does not attempt to validate images here because MAUI image resources
/// are resolved differently at runtime.
/// </summary>
public sealed class AssetPreloadService
{
    private readonly AssetRegistry _registry;

    public AssetPreloadService(AssetRegistry registry)
    {
        _registry = registry;
    }

    public async Task<PreloadedAssets> PreloadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var backgroundJson = await ReadPackageTextAsync(_registry.BackgroundCollisionPath, cancellationToken);

        var characterJsonByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in _registry.CharacterCollisionPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            characterJsonByKey[pair.Key] = await ReadPackageTextAsync(pair.Value, cancellationToken);
        }

        var projectileJsonByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in _registry.ProjectileCollisionPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            projectileJsonByKey[pair.Key] = await ReadPackageTextAsync(pair.Value, cancellationToken);
        }

        return new PreloadedAssets
        {
            BackgroundJson = backgroundJson,
            CharacterJsonByKey = characterJsonByKey,
            ProjectileJsonByKey = projectileJsonByKey,
            CharacterImagePaths = new Dictionary<string, string>(_registry.CharacterImagePaths),
            ProjectileImagePaths = new Dictionary<string, string>(_registry.ProjectileImagePaths),
            MenuIllustrationPaths = new Dictionary<string, string>(_registry.MenuIllustrationPaths),
            MusicPaths = new Dictionary<string, string>(_registry.MusicPaths),
            SfxPaths = new Dictionary<string, string>(_registry.SfxPaths)
        };
    }

    private static async Task<string> ReadPackageTextAsync(string logicalPath, CancellationToken cancellationToken)
    {
        await using var stream = await FileSystem.OpenAppPackageFileAsync(logicalPath);
        using var reader = new StreamReader(stream);

        cancellationToken.ThrowIfCancellationRequested();
        return await reader.ReadToEndAsync(cancellationToken);
    }
}