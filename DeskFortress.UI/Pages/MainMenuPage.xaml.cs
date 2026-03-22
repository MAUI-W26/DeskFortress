using DeskFortress.UI.ViewModels;

namespace DeskFortress.UI.Pages;

/// <summary>
/// Main menu page.
/// 
/// Responsibilities:
/// - bind UI to ViewModel
/// - handle navigation only
/// - run lightweight animation (illustration rotation)
/// 
/// Does NOT:
/// - create GameWorld
/// - touch Core logic
/// </summary>
public partial class MainMenuPage : ContentPage
{
    private readonly MainMenuViewModel _vm;

    private bool _isRunning;

    public MainMenuPage(MainMenuViewModel vm)
    {
        InitializeComponent();
        _vm = vm;

        // Wire UI events to ViewModel actions
        StartButton.Clicked += (_, _) => _vm.StartGame();
        StatsButton.Clicked += (_, _) => _vm.OpenStats();

        // Subscribe to ViewModel events
        _vm.StartGameRequested += OnStartGameRequested;
        _vm.StatsRequested += OnStatsRequested;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Initial image render
        UpdateIllustration();

        // Start rotation loop
        _isRunning = true;
        _ = RunIllustrationLoop();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Stop loop cleanly to avoid background execution
        _isRunning = false;
    }

    /// <summary>
    /// Simple loop to rotate menu images.
    /// No timers from Core — UI concern only.
    /// </summary>
    private async Task RunIllustrationLoop()
    {
        while (_isRunning)
        {
            await Task.Delay(2000);

            _vm.NextIllustration();

            // Ensure UI thread update
            MainThread.BeginInvokeOnMainThread(UpdateIllustration);
        }
    }

    /// <summary>
    /// Applies the current illustration to the Image control.
    /// 
    /// IMPORTANT:
    /// We explicitly use ImageSource.FromFile(...) because MAUI
    /// resource resolution is not reliable with plain strings
    /// when assets are in nested folders.
    /// </summary>
    private void UpdateIllustration()
    {
        var path = _vm.CurrentIllustration;

        // Debug (optional, remove later)
        System.Diagnostics.Debug.WriteLine($"[MainMenu] Loading image: {path}");

        IllustrationImage.Source = ImageSource.FromFile(path);
    }

    private async void OnStartGameRequested()
    {
        // Route not implemented yet -> placeholder
        await DisplayAlert("Info", "Game start not implemented yet", "OK");
    }

    private async void OnStatsRequested()
    {
        await DisplayAlert("Info", "Stats page not implemented yet", "OK");
    }
}