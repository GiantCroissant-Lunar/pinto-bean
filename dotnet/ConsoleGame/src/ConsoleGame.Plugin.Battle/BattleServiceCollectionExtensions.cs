using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ConsoleGame.Plugin.Battle;

public static class BattleServiceCollectionExtensions
{
    public static IServiceCollection AddBattleSubsystem(this IServiceCollection services)
    {
        services.AddSingleton<ILogger<BattlePlugin>>(provider =>
        {
            var loggerFactory = provider.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            return BattlePureDiAdapter.CreateLogger(loggerFactory);
        });

        return services;
    }
}
