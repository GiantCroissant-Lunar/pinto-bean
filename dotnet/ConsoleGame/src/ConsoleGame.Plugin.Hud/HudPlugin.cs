using ConsoleGame.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ConsoleGame.Plugin.Hud;

[Plugin("consolegame.hud", "HUD", "0.1.0", Description = "Head-up display widgets powered by Terminal.Gui")]
public sealed class HudPlugin : IPlugin, IRuntimePlugin, IDisposable
{
    private ILogger<HudPlugin>? _logger;

    public HudPlugin(ILogger<HudPlugin>? logger = null)
    {
        _logger = logger;
    }

    public string Name => "HUD";

    public IPluginContext? Context { get; set; }

    public string Describe() => "HUD composition primitives";

    public Task ConfigureAsync(CancellationToken cancellationToken = default)
    {
        var logger = _logger ??= ResolveLogger(Context?.Services);
        logger.LogInformation("HUD plugin configured for directory {Directory}", Context?.PluginDirectory ?? "(unknown)");
        return Task.CompletedTask;
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        var logger = _logger ??= ResolveLogger(Context?.Services);
        logger.LogDebug("HUD plugin RunAsync invoked");
        return Task.CompletedTask;
    }

    private static ILogger<HudPlugin> ResolveLogger(IServiceProvider? services)
    {
        return services?.GetService<ILogger<HudPlugin>>()
            ?? HudComposition.ResolveOrDefault<ILogger<HudPlugin>>(services)
            ?? NullLogger<HudPlugin>.Instance;
    }

    public void Dispose()
    {
        HudComposition.Reset();
    }
}
