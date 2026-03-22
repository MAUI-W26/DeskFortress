using DeskFortress.UI.ViewModels;

namespace DeskFortress.UI.Pages;

public partial class SplashPage : ContentPage
{
    private readonly SplashViewModel _viewModel;
    private bool _hasStarted;

    public SplashPage(SplashViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_hasStarted)
            return;

        _hasStarted = true;

        try
        {
            StatusLabel.Text = "Loading packaged assets...";

            await _viewModel.InitializeAsync();

            // Absolute navigation -> clears splash from stack
            await Shell.Current.GoToAsync($"//{nameof(MainMenuPage)}");
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Startup failed: {ex.Message}";
        }
    }
}