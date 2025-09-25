using ConsoleGame.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ConsoleGame.Plugin.Audio;

[Plugin("consolegame.audio", "Audio", "0.1.0", Description = "Provides audio capabilities via LibVLC")]
public sealed class AudioPlugin : IPlugin, IRuntimePlugin
{
    private ILogger<AudioPlugin>? _logger;
    private IAudioService? _audioService;

    // Constructor intended for Pure.DI; optional parameters keep backward compatibility while we migrate.
    public AudioPlugin(ILogger<AudioPlugin>? logger = null, IAudioService? audioService = null)
    {
        _logger = logger;
        _audioService = audioService;
    }

    public string Name => "Audio";

    public IPluginContext? Context { get; set; }

    public string Describe() => "Audio subsystem plugin";

    public Task ConfigureAsync(CancellationToken cancellationToken = default)
    {
        var services = Context?.Services;

        _logger ??= services?.GetService<ILogger<AudioPlugin>>() ?? NullLogger<AudioPlugin>.Instance;
        _audioService ??= services?.GetService<IAudioService>() ?? AudioComposition.ResolveOrDefault<IAudioService>(services);

        if (_audioService is not null)
        {
            _logger.LogInformation("Audio plugin configured. Enabled={Enabled}", _audioService.IsEnabled);
        }
        else
        {
            _logger.LogWarning("Audio plugin configured but no IAudioService is registered in the current scope");
        }

        return Task.CompletedTask;
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        (_logger ?? NullLogger<AudioPlugin>.Instance).LogDebug("Audio plugin RunAsync invoked");
        return Task.CompletedTask;
    }
}
