using System.Collections.Concurrent;
using ConsoleGame.SplatSupport;
using Splat;

namespace ConsoleGame.Dungeon.Plugin;

[SplatComposition]
internal static partial class DungeonComposition
{
    private static readonly ConcurrentBag<IDisposable> TrackedDisposables = new();

    static partial void Register(IMutableDependencyResolver resolver, IServiceProvider? services)
    {
        resolver.RegisterLazySingleton(CreateCompositionRoot, typeof(CompositionRoot));
        resolver.Register(() => Resolve<CompositionRoot>(services).Loop, typeof(IAppLoop));
        resolver.Register(() => Resolve<CompositionRoot>(services).LogService, typeof(ILogService));
    }

    private static CompositionRoot CreateCompositionRoot()
        => Track(new CompositionRoot());

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
    }
}
