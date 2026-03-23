using DeskFortress.Core.Simulation;

namespace DeskFortress.UI.Game;

public sealed class GameSession
{
    public GameWorld World { get; }

    public bool IsRunning { get; set; }

    public int Throws { get; set; }
    public int Hits { get; set; }
    public int WallHits { get; set; }

    public GameSession(GameWorld world)
    {
        World = world;
    }

    public void Update(float dt)
    {
        if (!IsRunning) return;
        World.Update(dt);
    }
}