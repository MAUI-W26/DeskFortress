using DeskFortress.UI.Rendering;

namespace DeskFortress.UI.Game;

public sealed class GameLoopService
{
    private readonly Renderer _renderer;
    private readonly EventDispatcher _events;

    public GameLoopService(Renderer renderer, EventDispatcher events)
    {
        _renderer = renderer;
        _events = events;
    }

    public void Tick(GameSession? session, float dt)
    {
        if (session is null || !session.IsRunning) return;

        session.Update(dt);

        _events.Handle(session);
        _renderer.Render(session.World);
    }
}