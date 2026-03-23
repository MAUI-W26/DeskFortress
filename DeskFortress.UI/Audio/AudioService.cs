using DeskFortress.UI.Assets;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;

namespace DeskFortress.UI.Audio;

/// <summary>
/// Production audio service using Plugin.Maui.Audio.
/// 
/// Responsibilities:
/// - Resolve paths from AssetRegistry
/// - Manage SFX playback
/// - Manage single active music track (looped)
/// </summary>
public sealed class AudioService
{
    private readonly AssetRegistry _registry;
    private readonly IAudioManager _audioManager;
    private readonly ILogger<AudioService> _logger;

    private IAudioPlayer? _musicPlayer;

    public AudioService(
        AssetRegistry registry,
        IAudioManager audioManager,
        ILogger<AudioService> logger)
    {
        _registry = registry;
        _audioManager = audioManager;
        _logger = logger;
    }

    // ----------------------------
    // SFX (fire-and-forget)
    // ----------------------------

    public async void PlaySfx(string key)
    {
        if (!_registry.SfxPaths.TryGetValue(key, out var path))
        {
            _logger.LogWarning("SFX key not found: {Key}", key);
            return;
        }

        try
        {
            var stream = await FileSystem.OpenAppPackageFileAsync(path);
            var player = _audioManager.CreatePlayer(stream);

            player.Play();

            _logger.LogDebug("SFX played: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play SFX: {Key}", key);
        }
    }

    // ----------------------------
    // Music (single active loop)
    // ----------------------------

    public async void PlayMusic(string key)
    {
        if (!_registry.MusicPaths.TryGetValue(key, out var path))
        {
            _logger.LogWarning("Music key not found: {Key}", key);
            return;
        }

        try
        {
            StopMusic();

            var stream = await FileSystem.OpenAppPackageFileAsync(path);
            var player = _audioManager.CreatePlayer(stream);

            player.Loop = true;
            player.Volume = 0.5;

            player.Play();

            _musicPlayer = player;

            _logger.LogInformation("Music started: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play music: {Key}", key);
        }
    }

    public void StopMusic()
    {
        if (_musicPlayer is null)
            return;

        try
        {
            _musicPlayer.Stop();
            _musicPlayer.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping music");
        }

        _musicPlayer = null;
    }
}