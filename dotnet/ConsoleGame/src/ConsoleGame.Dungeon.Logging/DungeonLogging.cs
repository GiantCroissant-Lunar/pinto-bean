namespace ConsoleGame.Dungeon.Logging;

public static class DungeonLogging
{
    public static ILogService Resolve(IServiceProvider? services = null)
        => DungeonLoggingComposition.Resolve<ILogService>(services);

    public static void Reset()
        => DungeonLoggingComposition.Reset();
}
