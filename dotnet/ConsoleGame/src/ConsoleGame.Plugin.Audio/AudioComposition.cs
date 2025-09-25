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
        var options = services?.GetService<IOptions<AudioOptions>>()?.Value ?? new AudioOptions();
        var loggerFactory = services?.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;

        if (!options.Enabled)
        {
            var logger = loggerFactory.CreateLogger<NoAudioService>();
            var reason = string.IsNullOrWhiteSpace(options.DisableReason)
                ? "Audio disabled via configuration"
                : options.DisableReason!;
            return Track(new NoAudioService(logger, reason));
        }

        var libLogger = loggerFactory.CreateLogger<LibVlcAudioService>();
        return Track(new LibVlcAudioService(libLogger));
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
