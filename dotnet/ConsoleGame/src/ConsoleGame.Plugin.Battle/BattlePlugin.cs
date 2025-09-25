using ConsoleGame.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ConsoleGame.Plugin.Battle;

[Plugin("consolegame.battle", "Battle", "0.1.0", Description = "Battle loop orchestration and combat systems")]
public sealed class BattlePlugin : IPlugin, IRuntimePlugin, IDisposable
{
    private ILogger<BattlePlugin>? _logger;

    public BattlePlugin(ILogger<BattlePlugin>? logger = null)
    {
        _logger = logger;
    }

    public string Name => "Battle";

    public IPluginContext? Context { get; set; }

    public string Describe() => "Battle systems and orchestration";

    public Task ConfigureAsync(CancellationToken cancellationToken = default)
    {
        var logger = _logger ??= ResolveLogger(Context?.Services);
        logger.LogInformation("Battle plugin configured for directory {Directory}", Context?.PluginDirectory ?? "(unknown)");
        return Task.CompletedTask;
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        var logger = _logger ??= ResolveLogger(Context?.Services);
        logger.LogDebug("Battle plugin RunAsync invoked");
        return Task.CompletedTask;
    }

    private static ILogger<BattlePlugin> ResolveLogger(IServiceProvider? services)
    {
        return services?.GetService<ILogger<BattlePlugin>>()
            ?? BattleComposition.ResolveOrDefault<ILogger<BattlePlugin>>(services)
            ?? NullLogger<BattlePlugin>.Instance;
    }

    public void Dispose()
    {
        BattleComposition.Reset();
    }
}
