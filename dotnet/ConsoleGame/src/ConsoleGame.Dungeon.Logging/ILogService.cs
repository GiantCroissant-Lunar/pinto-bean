namespace ConsoleGame.Dungeon.Logging;

public interface ILogService
{
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? exception = null);
    bool HasEntries { get; }
    IReadOnlyList<LogEntry> Snapshot();
    IReadOnlyList<LogEntry> Flush();
    string? LogPath { get; }
    void SetBaseDirectory(string baseDirectory);
}
