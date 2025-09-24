using System.Threading;
using System.Threading.Tasks;

namespace ConsoleGame.Contracts;

public interface IPlugin
{
    string Name { get; }
    string Describe();

    IPluginMetadata? Metadata => null;
    IPluginContext? Context { get; set; }

    Task ConfigureAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    Task<bool> CanUnloadAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
    Task PrepareUnloadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
