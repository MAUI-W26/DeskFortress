using DeskFortress.UI.ViewModels;
using DeskFortress.UI.Audio;

namespace DeskFortress.UI.Pages;

/// <summary>
/// Main menu page.
/// </summary>
public partial class MainMenuPage : ContentPage
{
    private readonly MainMenuViewModel _vm;
    private readonly AudioService _audio;

    private bool _isRunning;

    public MainMenuPage(MainMenuViewModel vm, AudioService audio)
    {
        InitializeComponent();

        _vm = vm;
        _audio = audio;

        // UI  VM
        StartButton.Clicked += OnStartClicked;
        StatsButton.Clicked += OnStatsClicked;

        // VM  UI
        _vm.StartGameRequested += OnStartGameRequested;
        _vm.StatsRequested += OnStatsRequested;
    }

    // ----------------------------
    // Lifecycle
    // ----------------------------

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _audio.PlayMusic("menu");

        UpdateIllustration();

        _isRunning = true;
        _ = RunIllustrationLoop();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _isRunning = false;

        _audio.StopMusic();
    }

    // ----------------------------
    // UI events  VM
    // ----------------------------

    private void OnStartClicked(object? sender, EventArgs e)
    {
        _vm.StartGame();
    }

    private void OnStatsClicked(object? sender, EventArgs e)
    {
        _vm.OpenStats();
    }

    // ----------------------------
    // VM events  UI
    // ----------------------------

    private async void OnStartGameRequested()
    {
        await Shell.Current.GoToAsync("//GamePage");
    }

    private async void OnStatsRequested()
    {
        await DisplayAlert("Info", "Stats page not implemented yet", "OK");
    }

    // ----------------------------
    // Illustration loop
    // ----------------------------

    private async Task RunIllustrationLoop()
    {
        while (_isRunning)
        {
            await Task.Delay(2000);

            _vm.NextIllustration();

            MainThread.BeginInvokeOnMainThread(UpdateIllustration);
        }
    }

    private void UpdateIllustration()
    {
        var path = _vm.CurrentIllustration;

        IllustrationImage.Source = ImageSource.FromFile(path);
    }
}