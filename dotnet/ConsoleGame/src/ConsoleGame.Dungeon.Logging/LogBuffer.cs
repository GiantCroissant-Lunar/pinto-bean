using System.Collections.Concurrent;
using System.Text;

namespace ConsoleGame.Dungeon.Logging;

internal sealed class LogBuffer : ILogService, IDisposable
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();
    private readonly object _sync = new();
    private string? _logFilePath;
    private string? _overrideBaseDirectory;
    private bool _logFilePathInitialized;

    public string? LogPath
    {
        get
        {
            lock (_sync)
            {
                EnsureLogPathInitialized();
                return _logFilePath;
            }
        }
    }

    public bool HasEntries => !_entries.IsEmpty;

    public void Info(string message) => Add(LogLevel.Info, message, null);
    public void Warn(string message) => Add(LogLevel.Warning, message, null);
    public void Error(string message, Exception? exception = null) => Add(LogLevel.Error, message, exception);

    public IReadOnlyList<LogEntry> Snapshot()
    {
        lock (_sync)
        {
            return _entries.ToArray();
        }
    }

    public IReadOnlyList<LogEntry> Flush()
    {
        lock (_sync)
        {
            var list = new List<LogEntry>();
            while (_entries.TryDequeue(out var entry))
            {
                list.Add(entry);
            }
            return list;
        }
    }

    public void SetBaseDirectory(string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            return;
        }

        lock (_sync)
        {
            _overrideBaseDirectory = baseDirectory;
            _logFilePathInitialized = false;
            _logFilePath = null;
        }
    }

    public void Dispose()
    {
        try
        {
            FlushToFile(Flush());
        }
        catch
        {
            // ignore dispose flush errors
        }
    }

    private void Add(LogLevel level, string message, Exception? exception)
    {
        lock (_sync)
        {
            var entry = new LogEntry(level, message, exception, DateTime.Now);
            _entries.Enqueue(entry);

            EnsureLogPathInitialized();
            if (_logFilePath is not null)
            {
                TryAppendToFile(entry);
            }
        }
    }

    private void EnsureLogPathInitialized()
    {
        if (_logFilePathInitialized)
        {
            return;
        }

        _logFilePathInitialized = true;
        try
        {
            var baseDir = _overrideBaseDirectory ?? Path.GetDirectoryName(typeof(LogBuffer).Assembly.Location) ?? AppContext.BaseDirectory;
            if (string.IsNullOrWhiteSpace(baseDir))
            {
                _logFilePath = null;
                return;
            }

            var assetsDir = Path.Combine(baseDir, "assets");
            Directory.CreateDirectory(assetsDir);
            _logFilePath = Path.Combine(assetsDir, "dungeon-plugin.log");
            if (!File.Exists(_logFilePath))
            {
                File.WriteAllText(_logFilePath, $"# Dungeon plugin log created {DateTime.Now:u}{Environment.NewLine}");
            }
        }
        catch
        {
            _logFilePath = null;
        }
    }

    private void TryAppendToFile(LogEntry entry)
    {
        if (_logFilePath is null)
        {
            return;
        }

        try
        {
            var sb = new StringBuilder();
            sb.AppendLine(entry.ToString());
            if (entry.Exception is { } ex)
            {
                sb.AppendLine(ex.ToString());
            }
            File.AppendAllText(_logFilePath, sb.ToString());
        }
        catch
        {
            // ignore file IO issues
        }
    }

    private void FlushToFile(IReadOnlyList<LogEntry> entries)
    {
        if (_logFilePath is null || entries.Count == 0)
        {
            return;
        }

        try
        {
            var sb = new StringBuilder();
            foreach (var entry in entries)
            {
                sb.AppendLine(entry.ToString());
                if (entry.Exception is { } ex)
                {
                    sb.AppendLine(ex.ToString());
                }
            }
            File.AppendAllText(_logFilePath, sb.ToString());
        }
        catch
        {
            // ignore flush errors
        }
    }
}
