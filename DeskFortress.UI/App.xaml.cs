namespace DeskFortress.UI;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Resolve from DI so dependencies work
        var shell = _services.GetRequiredService<AppShell>();

        return new Window(shell);
    }
}