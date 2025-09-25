using ConsoleGame.Contracts;
using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleGame.Plugin.Audio;

internal sealed class LibVlcAudioService : IAudioService
{
    private static readonly (string Name, string[] Options)[] BackendOrder =
    {
        ("waveout", new[]{"--quiet", "--aout=waveout"}),
        ("wasapi", new[]{"--quiet", "--aout=wasapi"}),
        ("directsound", new[]{"--quiet", "--aout=directsound"}),
        ("default", new[]{"--quiet"})
    };

    private readonly ILogger<LibVlcAudioService> _logger;
    private readonly object _gate = new();
    private LibVLC _vlc;
    private MediaPlayer? _player;
    private Media? _currentMedia;
    private bool _audioEnabled;
    private string _outputModule = "unknown";
    private bool _logHooked;
    private int _fallbackScheduled;

    public LibVlcAudioService(ILogger<LibVlcAudioService> logger)
    {
        _logger = logger;
        Core.Initialize();
        _vlc = InitializeBackend();
    }

    public bool IsEnabled => _audioEnabled;

    public void Play(string pathOrUrl)
    {
        if (!_audioEnabled)
        {
            _logger.LogWarning("Audio playback skipped because module {Module} is unavailable. Requested media: {Media}", _outputModule, Path.GetFileName(pathOrUrl));
            return;
        }

        try
        {
            lock (_gate)
            {
                _player ??= new MediaPlayer(_vlc);
                if (_player.IsPlaying)
                {
                    _player.Stop();
                }

                _currentMedia?.Dispose();
                _currentMedia = new Media(_vlc, pathOrUrl, FromType.FromPath);
                _player.Play(_currentMedia);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audio playback failed for {Media}", pathOrUrl);
        }
    }

    public void Dispose()
    {
        try
        {
            lock (_gate)
            {
                if (_player is { IsPlaying: true })
                {
                    _player.Stop();
                }

                _currentMedia?.Dispose();
                _player?.Dispose();
            }
        }
        finally
        {
            DetachLogHook(_vlc);
            _vlc.Dispose();
        }
    }

    private LibVLC InitializeBackend()
    {
        LibVLC? created = null;
        Exception? lastError = null;

        foreach (var attempt in BackendOrder)
        {
            try
            {
                created = attempt.Options.Length == 0 ? new LibVLC() : new LibVLC(attempt.Options);
                _audioEnabled = true;
                _outputModule = attempt.Name;
                HookLibVlcLogs(created);
                _logger.LogInformation("LibVLC audio initialized using module {Module}", _outputModule);
                break;
            }
            catch (Exception ex)
            {
                lastError = ex;
                _logger.LogWarning(ex, "LibVLC audio init failed for module {Module}", attempt.Name);
                created?.Dispose();
                created = null;
            }
        }

        if (created is null)
        {
            try
            {
                created = new LibVLC("--quiet", "--no-audio");
                _audioEnabled = false;
                _outputModule = "disabled";
                HookLibVlcLogs(created);
                if (lastError is not null)
                {
                    _logger.LogError(lastError, "Audio disabled; all LibVLC audio backends failed to initialize");
                }
                else
                {
                    _logger.LogError("Audio disabled; LibVLC audio backends unavailable");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize LibVLC for audio playback", ex);
            }
        }

        return created;
    }

    private void HookLibVlcLogs(LibVLC vlc)
    {
        if (_logHooked)
        {
            return;
        }

        vlc.Log += HandleVlcLog;
        _logHooked = true;
    }

    private void DetachLogHook(LibVLC vlc)
    {
        if (!_logHooked)
        {
            return;
        }

        try
        {
            vlc.Log -= HandleVlcLog;
        }
        catch
        {
            // ignored
        }

        _logHooked = false;
    }

    private void HandleVlcLog(object? sender, LogEventArgs e)
    {
        var message = e.FormattedLog ?? e.Message ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(message) && message.Contains("directsound output error", StringComparison.OrdinalIgnoreCase))
        {
            ScheduleDirectSoundFallback();
        }

        switch (e.Level)
        {
            case LibVLCSharp.Shared.LogLevel.Error:
                _logger.LogError("LibVLC: {Message}", message);
                break;
            case LibVLCSharp.Shared.LogLevel.Warning:
                _logger.LogWarning("LibVLC: {Message}", message);
                break;
            default:
                _logger.LogInformation("LibVLC: {Message}", message);
                break;
        }
    }

    private void ScheduleDirectSoundFallback()
    {
        if (!_audioEnabled)
        {
            return;
        }

        if (!string.Equals(_outputModule, "directsound", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _fallbackScheduled, 1, 0) != 0)
        {
            return;
        }

        _logger.LogWarning("DirectSound reported playback errors; attempting to switch audio backends");
        _ = Task.Run(AttemptFallbackAfterDirectSoundFailure);
    }

    private async Task AttemptFallbackAfterDirectSoundFailure()
    {
        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

        lock (_gate)
        {
            try
            {
                _player?.Stop();
                _currentMedia?.Dispose();
                _player?.Dispose();
                _vlc.Dispose();

                _vlc = InitializeBackend();
                _fallbackScheduled = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to switch LibVLC backend after DirectSound errors");
            }
        }
    }
}
