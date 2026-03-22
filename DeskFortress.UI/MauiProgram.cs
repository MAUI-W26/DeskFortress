using Microsoft.Extensions.Logging;

using DeskFortress.UI.Assets;
using DeskFortress.UI.CoreIntegration;
using DeskFortress.UI.Storage;
using DeskFortress.UI.ViewModels;
using DeskFortress.UI.Pages;

namespace DeskFortress.UI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // =====================================================
        // CORE / BOOTSTRAP
        // =====================================================

        builder.Services.AddSingleton<AssetRegistry>();
        builder.Services.AddSingleton<AssetPreloadService>();
        builder.Services.AddSingleton<CoreBootstrapper>();

        // =====================================================
        // STORAGE
        // =====================================================

        builder.Services.AddSingleton<StatsRepository>();
        builder.Services.AddSingleton<GameResultRepository>();

        // =====================================================
        // VIEWMODELS
        // =====================================================

        builder.Services.AddSingleton<SplashViewModel>();
        builder.Services.AddSingleton<MainMenuViewModel>();

        // =====================================================
        // PAGES
        // =====================================================

        builder.Services.AddSingleton<SplashPage>();
        builder.Services.AddSingleton<MainMenuPage>();

        // =====================================================
        // SHELL + APP ROOT
        // =====================================================

        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<App>();

        return builder.Build();
    }
}