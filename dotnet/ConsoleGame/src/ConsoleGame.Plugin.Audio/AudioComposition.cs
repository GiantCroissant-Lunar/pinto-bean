using System.Collections.Concurrent;
using ConsoleGame.Contracts;
using ConsoleGame.SplatSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Splat;

namespace ConsoleGame.Plugin.Audio;

[SplatComposition]
internal static partial class AudioComposition
{
    private static readonly ConcurrentBag<IDisposable> TrackedDisposables = new();

    static partial void Register(IMutableDependencyResolver resolver, IServiceProvider? services)
    {
        resolver.RegisterLazySingleton(() => Track(CreateAudioService(services)), typeof(IAudioService));
    }

    private static IAudioService CreateAudioService(IServiceProvider? services)
    {
        var options = services?.GetService<IOptions<AudioOptions>>()?.Value ?? CreateDefaultAudioOptions();
        var loggerFactory = services?.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
        var audio = AudioPureDiAdapter.CreateAudioService(options, loggerFactory);
        return Track(audio);
    }

    private static AudioOptions CreateDefaultAudioOptions()
    {
        var options = new AudioOptions();
        var disableAudioEnv = Environment.GetEnvironmentVariable("CONSOLEGAME_DISABLE_AUDIO");
        if (!string.IsNullOrWhiteSpace(disableAudioEnv) && disableAudioEnv.Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            options.Enabled = false;
            options.DisableReason = "Disabled via CONSOLEGAME_DISABLE_AUDIO=1";
        }
        else if (!string.IsNullOrWhiteSpace(disableAudioEnv) && disableAudioEnv.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            options.Enabled = false;
            options.DisableReason = "Disabled via CONSOLEGAME_DISABLE_AUDIO=true";
        }

        return options;
    }

    private static T Track<T>(T instance) where T : IDisposable
    {
        TrackedDisposables.Add(instance);
        return instance;
    }

    static partial void ResetInternal()
    {
        while (TrackedDisposables.TryTake(out var disposable))
        {
            try
            {
                disposable.Dispose();
            }
            catch
            {
                // Best effort cleanup; suppress exceptions during unload.
            }
        }
    }
}
