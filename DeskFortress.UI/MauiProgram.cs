using DeskFortress.UI.Assets;
using DeskFortress.UI.Audio;
using DeskFortress.UI.CoreIntegration;
using DeskFortress.UI.Pages;
using DeskFortress.UI.Storage;
using DeskFortress.UI.ViewModels;
using DeskFortress.UI.Game;
using DeskFortress.UI.Rendering;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;

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
        // AUDIO
        // =====================================================

        builder.Services.AddSingleton(AudioManager.Current);
        builder.Services.AddSingleton<AudioService>();

        // =====================================================
        // STORAGE
        // =====================================================

        builder.Services.AddSingleton<StatsRepository>();
        builder.Services.AddSingleton<GameResultRepository>();

        // =====================================================
        // GAME SESSION + LOOP
        // =====================================================

        builder.Services.AddSingleton<GameSessionManager>();
        builder.Services.AddTransient<GameLoopService>();
        builder.Services.AddTransient<InputTranslator>();
        builder.Services.AddTransient<EventDispatcher>();

        // =====================================================
        // RENDERING
        // =====================================================

        builder.Services.AddSingleton<IVisualResolver, DefaultVisualResolver>();
        builder.Services.AddSingleton<Renderer>();
        builder.Services.AddSingleton<WorldViewportService>(); //temp test - still in construction for stable background + pan motions

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
        builder.Services.AddTransient<GamePage>();

        // =====================================================
        // SHELL + APP ROOT
        // =====================================================

        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<App>();

        return builder.Build();
    }
}