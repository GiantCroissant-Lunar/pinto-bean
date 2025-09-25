using ConsoleGame.Contracts;
using Microsoft.Extensions.Logging;

namespace ConsoleGame.Plugin.Audio;

internal sealed class NoAudioService : IAudioService
{
    private readonly ILogger<NoAudioService> _logger;
    private readonly string _reason;

    public NoAudioService(ILogger<NoAudioService> logger, string reason)
    {
        _logger = logger;
        _reason = reason;
        _logger.LogWarning("Audio disabled: {Reason}", reason);
    }

    public bool IsEnabled => false;

    public void Play(string pathOrUrl)
    {
        _logger.LogDebug("Audio playback skipped for {Media} because audio is disabled", pathOrUrl);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}
