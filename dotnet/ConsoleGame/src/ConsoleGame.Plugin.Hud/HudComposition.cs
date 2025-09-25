using ConsoleGame.SplatSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Splat;

namespace ConsoleGame.Plugin.Hud;

[SplatComposition]
internal static partial class HudComposition
{
    static partial void Register(IMutableDependencyResolver resolver, IServiceProvider? services)
    {
        resolver.RegisterLazySingleton(CreateLogger, typeof(ILogger<HudPlugin>));

        ILogger<HudPlugin> CreateLogger()
        {
            var factory = services?.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            return factory.CreateLogger<HudPlugin>();
        }
    }

    static partial void ResetInternal()
    {
        // No scoped resources to dispose for this composition.
    }
}
