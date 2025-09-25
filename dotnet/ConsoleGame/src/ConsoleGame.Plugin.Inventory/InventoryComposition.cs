using ConsoleGame.SplatSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Splat;

namespace ConsoleGame.Plugin.Inventory;

[SplatComposition]
internal static partial class InventoryComposition
{
    static partial void Register(IMutableDependencyResolver resolver, IServiceProvider? services)
    {
        resolver.RegisterLazySingleton(CreateLogger, typeof(ILogger<InventoryPlugin>));

        ILogger<InventoryPlugin> CreateLogger()
        {
            var factory = services?.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            return factory.CreateLogger<InventoryPlugin>();
        }
    }

    static partial void ResetInternal()
    {
        // No disposable resources to release.
    }
}
