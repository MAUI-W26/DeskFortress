using System.Numerics;

namespace DeskFortress.UI.Game;

public sealed class InputTranslator
{
    public (float vx, float vy, float vz) Translate(Vector2 delta)
    {
        return (
            delta.X * 2f,
            -Math.Abs(delta.Y) * 3f,
            Math.Abs(delta.Y) * 2f
        );
    }
}