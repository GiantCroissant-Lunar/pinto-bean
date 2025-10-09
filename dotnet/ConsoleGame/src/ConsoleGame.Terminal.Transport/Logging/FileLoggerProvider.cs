using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ConsoleGame.Terminal.Transport.Logging;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly StreamWriter _writer;
    private readonly object _lock = new();
    private bool _disposed;

    public FileLoggerProvider(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path must be provided", nameof(filePath));
        }

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _writer = new StreamWriter(
            new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite),
            Encoding.UTF8,
            bufferSize: 4096,
            leaveOpen: false)
        {
            AutoFlush = true
        };
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, _writer, _lock, () => _disposed);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _writer.Dispose();
    }

    private sealed class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly TextWriter _writer;
        private readonly object _lock;
        private readonly Func<bool> _isDisposed;

        public FileLogger(string categoryName, TextWriter writer, object syncRoot, Func<bool> isDisposed)
        {
            _categoryName = categoryName;
            _writer = writer;
            _lock = syncRoot;
            _isDisposed = isDisposed;
        }

        IDisposable ILogger.BeginScope<TState>(TState state)
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => !_isDisposed();

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) where TState : notnull
        {
            if (_isDisposed())
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message) && exception is null)
            {
                return;
            }

            var line = $"{DateTimeOffset.UtcNow:O} [{logLevel}] {_categoryName}: {message}";

            lock (_lock)
            {
                _writer.WriteLine(line);
                if (exception is not null)
                {
                    _writer.WriteLine(exception);
                }
            }
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
