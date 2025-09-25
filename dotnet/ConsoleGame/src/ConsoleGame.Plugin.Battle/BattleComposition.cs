using ConsoleGame.SplatSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Splat;

namespace ConsoleGame.Plugin.Battle;

[SplatComposition]
internal static partial class BattleComposition
{
    static partial void Register(IMutableDependencyResolver resolver, IServiceProvider? services)
    {
        resolver.RegisterLazySingleton(CreateLogger, typeof(ILogger<BattlePlugin>));

        ILogger<BattlePlugin> CreateLogger()
        {
            var factory = services?.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            return factory.CreateLogger<BattlePlugin>();
        }
    }

    static partial void ResetInternal()
    {
        // No scoped resources to dispose for this composition.
    }
}
