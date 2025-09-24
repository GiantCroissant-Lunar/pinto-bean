using System.Threading;
using System.Threading.Tasks;

namespace ConsoleGame.Contracts;

public interface IRuntimePlugin
{
    Task RunAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
