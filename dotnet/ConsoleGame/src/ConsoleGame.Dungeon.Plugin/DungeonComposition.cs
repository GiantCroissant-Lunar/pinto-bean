using System;
using System.Collections.Concurrent;
using ConsoleGame.Contracts;
using ConsoleGame.Dungeon.Core;
using ConsoleGame.Dungeon.Logging;
using ConsoleGame.Dungeon.UI.Terminal;
using ConsoleGame.Plugin.Audio;
using ConsoleGame.SplatSupport;
using Splat;

namespace ConsoleGame.Dungeon.Plugin;

[SplatComposition]
internal static partial class DungeonComposition
{
    private static readonly ConcurrentBag<IDisposable> TrackedDisposables = new();
    private static string? _audioDisableReason;

    static partial void Register(IMutableDependencyResolver resolver, IServiceProvider? services)
    {
        resolver.RegisterLazySingleton(() => Track(CreateAudioService(services)), typeof(IAudioService));
        resolver.RegisterLazySingleton(() => DungeonLogging.Resolve(services), typeof(ILogService));
        resolver.RegisterLazySingleton(() => Track(CreateCompositionRoot(services)), typeof(CompositionRoot));
    }

    private static CompositionRoot CreateCompositionRoot(IServiceProvider? services)
    {
        var audio = Resolve<IAudioService>(services);
        var logService = DungeonLogging.Resolve(services);
        var viewModel = new DungeonViewModel();
        var bareLoop = new BareAppLoop(logService);
        var appLoop = new AppLoop(viewModel, audio, logService);
        return new CompositionRoot(viewModel, appLoop, bareLoop, audio, logService);
    }

    private static IAudioService CreateAudioService(IServiceProvider? services)
    {
        if (IsAudioDisabledByEnvironment(out var reason))
        {
            _audioDisableReason = reason;
            return new NoOpAudioService();
        }

        try
        {
            var audio = AudioComposition.Resolve<IAudioService>(services);
            _audioDisableReason = audio.IsEnabled ? null : "Audio service reported disabled.";
            return audio;
        }
        catch (Exception ex)
        {
            _audioDisableReason = $"Audio subsystem unavailable: {ex.Message}";
            return new NoOpAudioService();
        }
    }

    private static bool IsAudioDisabledByEnvironment(out string? reason)
    {
        var disableEnv = Environment.GetEnvironmentVariable("CONSOLEGAME_DISABLE_AUDIO");
        if (!string.IsNullOrWhiteSpace(disableEnv) &&
            (string.Equals(disableEnv, "1", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(disableEnv, "true", StringComparison.OrdinalIgnoreCase)))
        {
            reason = $"Audio disabled via CONSOLEGAME_DISABLE_AUDIO={disableEnv}";
            return true;
        }

        reason = null;
        return false;
    }

    internal static string? GetAudioDisableReason() => _audioDisableReason;

    private static T Track<T>(T disposable) where T : IDisposable
    {
        TrackedDisposables.Add(disposable);
        return disposable;
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
                // Suppress dispose errors during unload to avoid preventing ALC collection.
            }
        }

        AudioComposition.Reset();
        DungeonLogging.Reset();
        _audioDisableReason = null;
    }

    private sealed class NoOpAudioService : IAudioService
    {
        public bool IsEnabled => false;

        public void Play(string pathOrUrl)
        {
        }

        public void Dispose()
        {
        }
    }
}
