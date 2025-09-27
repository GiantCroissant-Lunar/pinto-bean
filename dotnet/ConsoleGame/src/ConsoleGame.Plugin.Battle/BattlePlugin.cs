using ConsoleGame.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ConsoleGame.Plugin.Battle;

[Plugin("consolegame.battle", "Battle", "0.1.0", Description = "Battle loop orchestration and combat systems")]
public sealed class BattlePlugin : IPlugin, IRuntimePlugin, IDisposable
{
    private ILogger<BattlePlugin>? _logger;
    private IServiceProvider? _serviceProvider;
    private IServiceScope? _serviceScope;
    private ServiceProvider? _fallbackProvider;
    private bool _disposed;

    public BattlePlugin(ILogger<BattlePlugin>? logger = null)
    {
        _logger = logger;
    }

    public string Name => "Battle";

    public IPluginContext? Context { get; set; }

    public string Describe() => "Battle systems and orchestration";

    public Task ConfigureAsync(CancellationToken cancellationToken = default)
    {
        var logger = GetLogger();
        logger.LogInformation("Battle plugin configured for directory {Directory}", Context?.PluginDirectory ?? "(unknown)");
        return Task.CompletedTask;
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        var logger = GetLogger();
        logger.LogDebug("Battle plugin RunAsync invoked");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _serviceScope?.Dispose();
        _serviceScope = null;

        _fallbackProvider?.Dispose();
        _fallbackProvider = null;

        _serviceProvider = null;
        _logger = null;

        BattleComposition.Reset();
    }

    private ILogger<BattlePlugin> GetLogger()
    {
        if (_logger is not null)
        {
            return _logger;
        }

        var provider = EnsureServiceProvider();
        _logger = provider.GetRequiredService<ILogger<BattlePlugin>>();
        return _logger;
    }

    private IServiceProvider EnsureServiceProvider()
    {
        if (_serviceProvider is not null)
        {
            return _serviceProvider;
        }

        var services = Context?.Services;
        if (services is not null)
        {
            var scopeFactory = services.GetService<IServiceScopeFactory>();
            if (scopeFactory is not null)
            {
                _serviceScope = scopeFactory.CreateScope();
                _serviceProvider = _serviceScope.ServiceProvider;
                return _serviceProvider;
            }

            _serviceProvider = services;
            return _serviceProvider;
        }

        _fallbackProvider = BuildFallbackProvider();
        _serviceProvider = _fallbackProvider;
        return _serviceProvider;
    }

    private static ServiceProvider BuildFallbackProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddBattleSubsystem();
        return services.BuildServiceProvider();
    }
}
