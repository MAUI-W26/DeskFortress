using DeskFortress.UI.Assets;
using DeskFortress.UI.CoreIntegration;
using DeskFortress.UI.Storage;

namespace DeskFortress.UI.ViewModels;

/// <summary>
/// Coordinates the entire startup pipeline.
///
/// Order matters:
/// 1. read packaged assets
/// 2. initialize Core from those assets
/// 3. ensure local storage exists
///
/// This is intentionally isolated from the page so splash UI remains thin.
/// </summary>
public sealed class SplashViewModel
{
    private readonly AssetPreloadService _assetPreloadService;
    private readonly CoreBootstrapper _coreBootstrapper;
    private readonly StatsRepository _statsRepository;
    private readonly GameResultRepository _gameResultRepository;

    public SplashViewModel(
        AssetPreloadService assetPreloadService,
        CoreBootstrapper coreBootstrapper,
        StatsRepository statsRepository,
        GameResultRepository gameResultRepository)
    {
        _assetPreloadService = assetPreloadService;
        _coreBootstrapper = coreBootstrapper;
        _statsRepository = statsRepository;
        _gameResultRepository = gameResultRepository;
    }

    public string StatusText { get; private set; } = "Starting...";

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        StatusText = "Loading packaged assets...";
        var preloadedAssets = await _assetPreloadService.PreloadAsync(cancellationToken);

        StatusText = "Initializing core runtime...";
        _coreBootstrapper.Initialize(preloadedAssets);

        StatusText = "Preparing local storage...";
        await _statsRepository.EnsureCreatedAsync();
        await _gameResultRepository.EnsureCreatedAsync();

        StatusText = "Ready.";
    }
}