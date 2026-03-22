using Microsoft.Extensions.Logging;
using DeskFortress.Core.Assets;
using DeskFortress.Core.Entities;
using DeskFortress.Core.Simulation;
using DeskFortress.Core.World;
using DeskFortress.UI.Assets;

namespace DeskFortress.UI.CoreIntegration;

public sealed class CoreBootstrapper
{
    private readonly ILogger<CoreBootstrapper> _logger;

    private bool _isInitialized;

    private BackgroundAsset? _backgroundAsset;
    private BackgroundMap? _backgroundMap;

    private readonly Dictionary<string, CharacterAsset> _characterAssets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ProjectileAsset> _projectileAssets = new(StringComparer.OrdinalIgnoreCase);

    private IReadOnlyDictionary<string, string> _characterImagePaths = new Dictionary<string, string>();
    private IReadOnlyDictionary<string, string> _projectileImagePaths = new Dictionary<string, string>();

    public CoreBootstrapper(ILogger<CoreBootstrapper> logger)
    {
        _logger = logger;
    }

    public bool IsInitialized => _isInitialized;

    public IReadOnlyDictionary<string, string> CharacterImagePaths => _characterImagePaths;
    public IReadOnlyDictionary<string, string> ProjectileImagePaths => _projectileImagePaths;

    public IReadOnlyCollection<string> CharacterKeys => _characterAssets.Keys.ToArray();
    public IReadOnlyCollection<string> ProjectileKeys => _projectileAssets.Keys.ToArray();

    public void Initialize(PreloadedAssets preloadedAssets)
    {
        ArgumentNullException.ThrowIfNull(preloadedAssets);

        _logger.LogInformation("=== Core Initialization START ===");

        _characterAssets.Clear();
        _projectileAssets.Clear();

        // -----------------------------
        // BACKGROUND
        // -----------------------------
        _backgroundAsset = AssetLoader.LoadFromJson<BackgroundAsset>(preloadedAssets.BackgroundJson);
        _backgroundMap = BackgroundFactory.Create(_backgroundAsset);

        _logger.LogInformation("Background loaded and map created");

        // -----------------------------
        // CHARACTERS
        // -----------------------------
        foreach (var pair in preloadedAssets.CharacterJsonByKey)
        {
            var asset = AssetLoader.LoadFromJson<CharacterAsset>(pair.Value);
            _characterAssets[pair.Key] = asset;

            _logger.LogInformation("Character loaded: {Key}", pair.Key);
        }

        // -----------------------------
        // PROJECTILES
        // -----------------------------
        foreach (var pair in preloadedAssets.ProjectileJsonByKey)
        {
            var asset = AssetLoader.LoadFromJson<ProjectileAsset>(pair.Value);
            _projectileAssets[pair.Key] = asset;

            _logger.LogInformation("Projectile loaded: {Key}", pair.Key);
        }

        _characterImagePaths = new Dictionary<string, string>(preloadedAssets.CharacterImagePaths);
        _projectileImagePaths = new Dictionary<string, string>(preloadedAssets.ProjectileImagePaths);

        _isInitialized = true;

        _logger.LogInformation(
            "Core ready: {CharacterCount} characters, {ProjectileCount} projectiles",
            _characterAssets.Count,
            _projectileAssets.Count);

        // -----------------------------
        // SANITY CHECK (CRITICAL)
        // -----------------------------
        var test = CreateRandomCoworker(new Random());
        _logger.LogInformation("Sanity check: created test coworker instance: {Type}", test.GetType().Name);

        _logger.LogInformation("=== Core Initialization END ===");
    }

    public GameWorld CreateWorld()
    {
        EnsureInitialized();
        return new GameWorld(_backgroundMap!);
    }

    public CoworkerEntity CreateCoworker(string key)
    {
        EnsureInitialized();

        if (!_characterAssets.TryGetValue(key, out var asset))
            throw new KeyNotFoundException($"Unknown character asset key: {key}");

        return CharacterFactory.Create(asset);
    }

    public ProjectileEntity CreateProjectile(string key)
    {
        EnsureInitialized();

        if (!_projectileAssets.TryGetValue(key, out var asset))
            throw new KeyNotFoundException($"Unknown projectile asset key: {key}");

        return ProjectileFactory.Create(asset);
    }

    public CoworkerEntity CreateRandomCoworker(Random random)
    {
        EnsureInitialized();
        ArgumentNullException.ThrowIfNull(random);

        var key = CharacterKeys.ElementAt(random.Next(CharacterKeys.Count));
        return CreateCoworker(key);
    }

    public ProjectileEntity CreateRandomProjectile(Random random)
    {
        EnsureInitialized();
        ArgumentNullException.ThrowIfNull(random);

        var key = ProjectileKeys.ElementAt(random.Next(ProjectileKeys.Count));
        return CreateProjectile(key);
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized || _backgroundMap is null || _backgroundAsset is null)
        {
            throw new InvalidOperationException(
                "CoreBootstrapper is not initialized. Call Initialize(...) during splash before using it.");
        }
    }
}