namespace ConsoleGame.Contracts;

public interface IPluginContext
{
    IServiceProvider Services { get; }
    string PluginDirectory { get; }
    IReadOnlyDictionary<string, object> Properties { get; }
    CancellationToken ShutdownToken { get; }
}
