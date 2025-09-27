using System.Diagnostics;
using System.Threading;
using ConsoleGame.Contracts;
using Microsoft.Extensions.Logging;
using Pure.DI;
using static Pure.DI.Lifetime;

namespace ConsoleGame.Plugin.Audio;

internal partial class AudioPureDiRoot
{
    [Conditional("DI")]
    private static void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "Off")
        .Bind<IAudioService>().To(_ => CreateAudio())
        .Root<IAudioService>(nameof(CreateAudioService));

    private static IAudioService CreateAudio()
    {
        var context = AudioPureDiRootScope.Current;
        var options = context.Options;
        var loggerFactory = context.LoggerFactory;

        if (!options.Enabled)
        {
            var reason = string.IsNullOrWhiteSpace(options.DisableReason)
                ? "Audio disabled via configuration"
                : options.DisableReason!;
            var disabledLogger = loggerFactory.CreateLogger<NoAudioService>();
            return new NoAudioService(disabledLogger, reason);
        }

        var audioLogger = loggerFactory.CreateLogger<LibVlcAudioService>();
        return new LibVlcAudioService(audioLogger);
    }
}

internal static class AudioPureDiAdapter
{
    public static IAudioService CreateAudioService(AudioOptions options, ILoggerFactory loggerFactory)
    {
        using var scope = AudioPureDiRootScope.Enter(options, loggerFactory);
        return new AudioPureDiRoot().CreateAudioService;
    }
}

internal static class AudioPureDiRootScope
{
    internal sealed record Context(AudioOptions Options, ILoggerFactory LoggerFactory);

    private sealed class Scope(Context? previous) : IDisposable
    {
        private readonly Context? _previous = previous;
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            State.Value = _previous;
        }
    }

    private static readonly AsyncLocal<Context?> State = new();

    public static IDisposable Enter(AudioOptions options, ILoggerFactory loggerFactory)
    {
        var scope = new Scope(State.Value);
        State.Value = new Context(options, loggerFactory);
        return scope;
    }

    internal static Context Current => State.Value ?? throw new InvalidOperationException("AudioPureDiRootScope was not configured for the current context.");
}
