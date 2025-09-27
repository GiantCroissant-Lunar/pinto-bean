using System.Threading;
using ConsoleGame.Dungeon.Logging;

namespace ConsoleGame.Dungeon.UI.Terminal;

public sealed class BareAppLoop : IAppLoop
{
    private readonly ILogService _log;

    public BareAppLoop(ILogService log)
    {
        _log = log;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        _log.Info("Bare loop active.");
        try
        {
            await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _log.Info("Bare loop cancelled.");
        }
        finally
        {
            _log.Info("Bare loop exiting.");
        }
    }
}
