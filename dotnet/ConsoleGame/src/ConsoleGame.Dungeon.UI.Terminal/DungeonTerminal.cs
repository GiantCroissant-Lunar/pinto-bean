using ConsoleGame.Contracts;
using ConsoleGame.Dungeon.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleGame.Dungeon.UI.Terminal;

public static class DungeonTerminalServiceCollectionExtensions
{
    public static IServiceCollection AddDungeonTerminalUi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<DungeonViewModel>();
        services.AddSingleton<BareAppLoop>();
        services.AddSingleton<AppLoop>(sp =>
        {
            var viewModel = sp.GetRequiredService<DungeonViewModel>();
            var audio = sp.GetRequiredService<IAudioService>();
            var log = sp.GetRequiredService<ILogService>();
            return new AppLoop(viewModel, audio, log);
        });

        return services;
    }
}
