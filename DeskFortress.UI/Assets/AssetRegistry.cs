namespace DeskFortress.UI.Assets;

public sealed class AssetRegistry
{
    public string BackgroundCollisionPath => "Collisions/office_background.json";
    public string BackgroundImagePath => "office_background.png";

    public IReadOnlyDictionary<string, string> CharacterCollisionPaths { get; } =
        new Dictionary<string, string>
        {
            ["cw1"] = "Collisions/cw1.json",
            ["cw2"] = "Collisions/cw2.json",
            ["cw3"] = "Collisions/cw3.json",
            ["cw4"] = "Collisions/cw4.json",
            ["cw5"] = "Collisions/cw5.json",
            ["cw6"] = "Collisions/cw6.json",
            ["cw7"] = "Collisions/cw7.json",
            ["cw8"] = "Collisions/cw8.json"
        };

    public IReadOnlyDictionary<string, string> CharacterImagePaths { get; } =
        new Dictionary<string, string>
        {
            ["cw1"] = "cw1.png",
            ["cw2"] = "cw2.png",
            ["cw3"] = "cw3.png",
            ["cw4"] = "cw4.png",
            ["cw5"] = "cw5.png",
            ["cw6"] = "cw6.png",
            ["cw7"] = "cw7.png",
            ["cw8"] = "cw8.png"
        };

    public IReadOnlyDictionary<string, string> ProjectileCollisionPaths { get; } =
        new Dictionary<string, string>
        {
            ["pb1"] = "Collisions/pb1.json",
            ["pb2"] = "Collisions/pb2.json"
        };

    public IReadOnlyDictionary<string, string> ProjectileImagePaths { get; } =
        new Dictionary<string, string>
        {
            ["pb1"] = "paperball01.png",
            ["pb2"] = "paperball02.png"
        };

    public IReadOnlyDictionary<string, string> MenuIllustrationPaths { get; } =
        new Dictionary<string, string>
        {
            ["hero"] = "hero.png",
            ["busy"] = "busy.png",
            ["bored"] = "bored.png",
            ["meeting"] = "meeting.png",
            ["pondering"] = "pondering.png",
            ["smoking"] = "smoking.png",
            ["frustrated"] = "Frustrated.png",
            ["overworked"] = "overworked.png"
        };

    public IReadOnlyDictionary<string, string> MusicPaths { get; } =
        new Dictionary<string, string>
        {
            ["menu"] = "Audio/Music/bgm1.mp3",
            ["game"] = "Audio/Music/bgm2.mp3"
        };

    public IReadOnlyDictionary<string, string> SfxPaths { get; } =
        new Dictionary<string, string>
        {
            ["throw"] = "Audio/Sfx/throw01.mp3",
            ["impact_high"] = "Audio/Sfx/impact_high.wav",
            ["impact_low"] = "Audio/Sfx/impact_low.wav",
            ["impact_object"] = "Audio/Sfx/impact_object.wav",
            ["missed_shot_floor"] = "Audio/Sfx/missed_shot_floor_impact.wav"
        };
}