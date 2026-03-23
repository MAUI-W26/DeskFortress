using DeskFortress.UI.CoreIntegration;

namespace DeskFortress.UI.Game;

/// <summary>
/// Manages game session lifecycle.
/// Core's WaveSpawnManager handles all spawning logic.
/// </summary>
public sealed class GameSessionManager
{
    private readonly CoreBootstrapper _core;

    public GameSession? Current { get; private set; }

    public GameSessionManager(CoreBootstrapper core)
    {
        _core = core;
    }

    public void StartNewGame()
    {
        var world = _core.CreateWorld();

        Current = new GameSession(world)
        {
            IsRunning = true
        };

        // Core's WaveSpawnManager will handle all spawning automatically
        // No manual initial spawns needed
    }

    public void Stop()
    {
        if (Current == null) return;

        Current.IsRunning = false;
        Current = null;
    }
}