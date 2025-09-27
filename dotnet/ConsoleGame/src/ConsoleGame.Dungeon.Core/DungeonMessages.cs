using System.Threading;
using System.Threading.Tasks;

namespace ConsoleGame.Dungeon.Core;

public readonly record struct MoveInput(int Dx, int Dy);
public readonly record struct PlayerMoved((int X, int Y) Position);

public interface IAppLoop
{
    Task RunAsync(CancellationToken cancellationToken);
}
