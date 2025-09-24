using ConsoleGame.Contracts;

namespace ConsoleGame.App;

internal sealed class HostPluginContext : IPluginContext
{
    public HostPluginContext(IServiceProvider services, string pluginDirectory, IReadOnlyDictionary<string, object>? properties, CancellationToken shutdownToken)
    {
        Services = services;
        PluginDirectory = pluginDirectory;
        Properties = properties ?? new Dictionary<string, object>();
        ShutdownToken = shutdownToken;
    }

    public IServiceProvider Services { get; }
    public string PluginDirectory { get; }
    public IReadOnlyDictionary<string, object> Properties { get; }
    public CancellationToken ShutdownToken { get; }
}
