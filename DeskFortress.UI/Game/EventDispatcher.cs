using DeskFortress.UI.Audio;

namespace DeskFortress.UI.Game;

public sealed class EventDispatcher
{
    private readonly AudioService _audio;

    public EventDispatcher(AudioService audio)
    {
        _audio = audio;
    }

    public void Handle(GameSession session)
    {
        foreach (var e in session.World.Events)
        {
            switch (e.Type)
            {
                case "projectile_hit_coworker":
                    session.Hits++;
                    _audio.PlaySfx("impact_high");
                    break;

                case "projectile_hit_wall":
                    session.WallHits++;
                    _audio.PlaySfx("impact_object");
                    break;

                case "projectile_hit_floor":
                    _audio.PlaySfx("missed_shot_floor");
                    break;
                    
                case "projectile_hit_property":
                    _audio.PlaySfx("impact_low");
                    break;
            }
        }
    }
}