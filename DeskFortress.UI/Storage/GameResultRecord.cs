namespace DeskFortress.UI.Storage;

public sealed class GameResultRecord
{
    public DateTimeOffset PlayedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public int Score { get; set; }
    public int Throws { get; set; }
    public int Hits { get; set; }
    public int WallHits { get; set; }
    public int PropertyHits { get; set; }
}