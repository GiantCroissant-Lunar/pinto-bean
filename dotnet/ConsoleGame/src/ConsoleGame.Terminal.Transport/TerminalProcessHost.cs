using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ConsoleGame.Terminal.Transport;

public sealed class TerminalProcessOptions
{
    public string FileName { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public IDictionary<string, string?> EnvironmentVariables { get; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    public TerminalSessionOptions SessionOptions { get; set; } = new();
}

public sealed class TerminalProcessHost : IAsyncDisposable
{
    private readonly Process _process;
    private readonly ILogger? _logger;
    private readonly ILogger? _sessionLogger;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    private TerminalProcessHost(Process process, TerminalSession session, ILogger? logger, ILogger? sessionLogger)
    {
        _process = process;
        Session = session;
        _logger = logger;
        _sessionLogger = sessionLogger;
    }

    public TerminalSession Session { get; }

    public int ProcessId => _process.Id;

    public static TerminalProcessHost Start(TerminalProcessOptions options, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.FileName))
        {
            throw new ArgumentException("Executable file name is required", nameof(options));
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = options.FileName,
            Arguments = options.Arguments,
            WorkingDirectory = string.IsNullOrWhiteSpace(options.WorkingDirectory) ? Environment.CurrentDirectory : options.WorkingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var kvp in options.EnvironmentVariables)
        {
            if (kvp.Value is null)
            {
                processStartInfo.Environment.Remove(kvp.Key);
            }
            else
            {
                processStartInfo.Environment[kvp.Key] = kvp.Value;
            }
        }

        var logger = loggerFactory?.CreateLogger<TerminalProcessHost>();
        var sessionLogger = loggerFactory?.CreateLogger<TerminalSession>();

        logger?.LogInformation("Launching terminal process: {FileName} {Arguments} (cwd: {WorkingDirectory})", processStartInfo.FileName, processStartInfo.Arguments, processStartInfo.WorkingDirectory);

        var process = new Process { StartInfo = processStartInfo, EnableRaisingEvents = true };
        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to start terminal process {FileName}", processStartInfo.FileName);
            process.Dispose();
            throw;
        }

        var session = new TerminalSession(
            process.StandardInput.BaseStream,
            process.StandardOutput.BaseStream,
            process.StandardError.BaseStream,
            options.SessionOptions,
            sessionLogger);

        var host = new TerminalProcessHost(process, session, logger, sessionLogger);
        host.Start();
        return host;
    }

    private void Start()
    {
        Session.Start();
        _process.Exited += OnProcessExited;
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        _logger?.LogInformation("Terminal process exited with code {ExitCode}", _process.ExitCode);

        _ = Task.Run(async () =>
        {
            try
            {
                await Session.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _sessionLogger?.LogError(ex, "Failed to dispose terminal session after process exit");
            }
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _process.Exited -= OnProcessExited;

        if (!_process.HasExited)
        {
            try
            {
                _logger?.LogInformation("Stopping terminal process (PID {Pid})", _process.Id);
                _process.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to kill terminal process cleanly");
            }
        }

        try
        {
            await Session.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _sessionLogger?.LogWarning(ex, "Error disposing terminal session");
        }

        _process.Dispose();
        _cts.Cancel();
        _cts.Dispose();
    }
}
