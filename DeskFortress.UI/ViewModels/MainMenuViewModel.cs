using DeskFortress.UI.Assets;
using DeskFortress.UI.CoreIntegration;

namespace DeskFortress.UI.ViewModels;

/// <summary>
/// Main menu logic layer.
/// 
/// Responsibilities:
/// - expose commands (StartGame, Stats)
/// - provide rotating background illustrations
/// - keep UI completely dumb
/// 
/// No gameplay logic here.
/// </summary>
public sealed class MainMenuViewModel
{
    private readonly AssetRegistry _assets;
    private readonly CoreBootstrapper _core;

    private readonly Random _random = new();

    private readonly string[] _illustrationKeys;

    private int _currentIndex;

    public MainMenuViewModel(AssetRegistry assets, CoreBootstrapper core)
    {
        _assets = assets;
        _core = core;

        _illustrationKeys = _assets.MenuIllustrationPaths.Keys.ToArray();
        _currentIndex = _random.Next(_illustrationKeys.Length);
    }

    /// <summary>
    /// Returns current illustration image path.
    /// </summary>
    public string CurrentIllustration =>
        _assets.MenuIllustrationPaths[_illustrationKeys[_currentIndex]];

    /// <summary>
    /// Rotate to next random illustration.
    /// </summary>
    public void NextIllustration()
    {
        _currentIndex = _random.Next(_illustrationKeys.Length);
    }

    /// <summary>
    /// Sanity check before allowing game start.
    /// </summary>
    public bool CanStartGame => _core.IsInitialized;

    public event Action? StartGameRequested;
    public event Action? StatsRequested;

    public void StartGame()
    {
        if (!CanStartGame)
            return;

        StartGameRequested?.Invoke();
    }

    public void OpenStats()
    {
        StatsRequested?.Invoke();
    }
}