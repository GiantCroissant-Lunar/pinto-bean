using System.Collections.Concurrent;
using ConsoleGame.SplatSupport;
using Splat;

namespace ConsoleGame.Dungeon.Logging;

[SplatComposition]
public static partial class DungeonLoggingComposition
{
    private static readonly ConcurrentBag<ILogService> TrackedLoggers = new();

    static partial void Register(IMutableDependencyResolver resolver, IServiceProvider? services)
    {
        resolver.RegisterLazySingleton(() => Track(new LogBuffer()), typeof(ILogService));
    }

    static partial void ResetInternal()
    {
        while (TrackedLoggers.TryTake(out var logger))
        {
            try
            {
                if (logger is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch
            {
                // Swallow logging disposal exceptions.
            }
        }
    }

    private static ILogService Track(ILogService service)
    {
        TrackedLoggers.Add(service);
        return service;
    }
}
