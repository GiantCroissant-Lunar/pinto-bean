namespace ConsoleGame.Dungeon.Logging;

public readonly record struct LogEntry(LogLevel Level, string Message, Exception? Exception, DateTime Timestamp)
{
    public override string ToString()
    {
        var prefix = Level switch
        {
            LogLevel.Info => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            _ => Level.ToString().ToUpperInvariant()
        };
        var baseMessage = $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{prefix}] {Message}";
        return Exception is null ? baseMessage : $"{baseMessage} ({Exception.GetType().Name}: {Exception.Message})";
    }
}
