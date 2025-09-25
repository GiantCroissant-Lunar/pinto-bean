using ConsoleGame.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ConsoleGame.Plugin.Inventory;

[Plugin("consolegame.inventory", "Inventory", "0.1.0", Description = "Inventory systems and persistence hooks")]
public sealed class InventoryPlugin : IPlugin, IRuntimePlugin, IDisposable
{
    private ILogger<InventoryPlugin>? _logger;

    public InventoryPlugin(ILogger<InventoryPlugin>? logger = null)
    {
        _logger = logger;
    }

    public string Name => "Inventory";

    public IPluginContext? Context { get; set; }

    public string Describe() => "Inventory management subsystem";

    public Task ConfigureAsync(CancellationToken cancellationToken = default)
    {
        var logger = _logger ??= ResolveLogger(Context?.Services);
        logger.LogInformation("Inventory plugin configured for directory {Directory}", Context?.PluginDirectory ?? "(unknown)");
        return Task.CompletedTask;
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        var logger = _logger ??= ResolveLogger(Context?.Services);
        logger.LogDebug("Inventory plugin RunAsync invoked");
        return Task.CompletedTask;
    }

    private static ILogger<InventoryPlugin> ResolveLogger(IServiceProvider? services)
    {
        return services?.GetService<ILogger<InventoryPlugin>>()
            ?? InventoryComposition.ResolveOrDefault<ILogger<InventoryPlugin>>(services)
            ?? NullLogger<InventoryPlugin>.Instance;
    }

    public void Dispose()
    {
        InventoryComposition.Reset();
    }
}
