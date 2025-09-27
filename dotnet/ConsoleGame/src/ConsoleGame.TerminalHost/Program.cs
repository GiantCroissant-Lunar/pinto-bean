using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuperSocket;
using SuperSocket.Connection;
using SuperSocket.Server;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Session;
using SuperSocket.Server.Host;
using SuperSocket.WebSocket;
using SuperSocket.WebSocket.Server;
using CloseReason = SuperSocket.Connection.CloseReason;

namespace ConsoleGame.TerminalHost;

public static class Program
{
    private static readonly ConcurrentDictionary<string, ProcessEntry> ProcessRegistry = new();

    public static async Task Main(string[] args)
    {
        var host = WebSocketHostBuilder.Create(args)
            .UseWebSocketMessageHandler(HandleMessageAsync)
            .UseSessionHandler(OnSessionConnectedAsync, OnSessionClosedAsync)
            .ConfigureSuperSocket(options =>
            {
                options.Name = "ConsoleGameTerminal";
                options.Listeners =
                [
                    new ListenOptions
                    {
                        Ip = "Any",
                        Port = ResolvePort()
                    }
                ];
            })
            .ConfigureLogging((_, logging) =>
            {
                logging.AddConsole();
            })
            // .UseInMemorySessionContainer()
            .Build();

        await host.RunAsync();
    }

    private static async ValueTask HandleMessageAsync(WebSocketSession session, WebSocketPackage package)
    {
        if (!ProcessRegistry.TryGetValue(session.SessionID, out var entry))
        {
            return;
        }

        await entry.ForwardInputAsync(package.Message, session);
    }

    private static async ValueTask OnSessionConnectedAsync(IAppSession session)
    {
        if (session is not WebSocketSession wsSession)
        {
            await session.CloseAsync(CloseReason.InternalError);
            return;
        }

        LogInformation(wsSession, "Session {SessionID} connected", session.SessionID);

        var entry = await CreateProcessEntryAsync(wsSession);
        if (entry is null)
        {
            return;
        }

        if (!ProcessRegistry.TryAdd(session.SessionID, entry))
        {
            await wsSession.SendAsync("Internal error: session registry conflict.\n");
            await wsSession.CloseAsync(CloseReason.InternalError);
            await entry.DisposeAsync();
            return;
        }

        entry.StartPumps(wsSession);
    }

    private static async ValueTask OnSessionClosedAsync(
        IAppSession session,
        CloseEventArgs e)
    {
        if (!ProcessRegistry.TryRemove(session.SessionID, out var entry))
        {
            return;
        }

        entry.Cancellation.Cancel();

        if (!entry.Process.HasExited)
        {
            try
            {
                entry.Process.Kill(true);
            }
            catch (Exception ex)
            {
                LogWarning(session, ex, "Failed to terminate ConsoleGame process for session {SessionID}", session.SessionID);
            }
        }

        await entry.DisposeAsync();

        LogInformation(session, "Session {SessionID} closed ({Reason})", session.SessionID, e.Reason);
    }

    private static async Task<ProcessEntry?> CreateProcessEntryAsync(WebSocketSession session)
    {
        var executable = TerminalProcessLocator.ResolveExecutablePath();
        var startInfo = new ProcessStartInfo
        {
            FileName = executable.FileName,
            Arguments = executable.Arguments,
            WorkingDirectory = executable.WorkingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            LogError(session, ex, "Failed to launch ConsoleGame process");
            await session.SendAsync($"Failed to launch ConsoleGame: {ex.Message}\n");
            await session.CloseAsync(CloseReason.InternalError);
            process.Dispose();
            return null;
        }

        var entry = new ProcessEntry(process, process.StandardInput, new CancellationTokenSource());

        process.Exited += async (_, _) =>
        {
            LogInformation(session, "ConsoleGame process exited for session {SessionID}", session.SessionID);

            try
            {
                await session.SendAsync("\n[Process terminated]\n");
                await session.CloseAsync();
            }
            catch
            {
                // ignore errors triggered during shutdown
            }
        };

        return entry;
    }

    private static int ResolvePort()
    {
        var portValue = Environment.GetEnvironmentVariable("CONSOLE_GAME_WS_PORT");
        return int.TryParse(portValue, out var port) ? port : 4040;
    }

    private static async Task PumpOutputAsync(WebSocketSession session, StreamReader reader, CancellationToken token, string streamName)
    {
        var buffer = new char[1024];

        try
        {
            while (!token.IsCancellationRequested)
            {
                var read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false);
                if (read <= 0)
                {
                    break;
                }

                var text = new string(buffer, 0, read);
                await session.SendAsync(Encoding.UTF8.GetBytes(text)).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // expected during shutdown
        }
        catch (Exception ex)
        {
            LogError(session, ex, "{Stream} pumping failed for session {SessionID}", streamName, session.SessionID);
        }
    }

    private static async Task SafeAwaitAsync(Task? task)
    {
        if (task is null)
        {
            return;
        }

        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // ignore cancellation during shutdown
        }
        catch (Exception)
        {
            // ignore other errors when disposing
        }
    }

    private static void LogInformation(IAppSession session, string message, params object[] args)
    {
        if (session is ILogger logger)
        {
            logger.LogInformation(message, args);
        }
    }

    private static void LogWarning(IAppSession session, Exception exception, string message, params object[] args)
    {
        if (session is ILogger logger)
        {
            logger.LogWarning(exception, message, args);
        }
    }

    private static void LogError(IAppSession session, Exception exception, string message, params object[] args)
    {
        if (session is ILogger logger)
        {
            logger.LogError(exception, message, args);
        }
    }

    private sealed class ProcessEntry : IAsyncDisposable, IDisposable
    {
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        public ProcessEntry(Process process, StreamWriter stdin, CancellationTokenSource cancellation)
        {
            Process = process;
            Stdin = stdin;
            Cancellation = cancellation;
        }

        public Process Process { get; }
        public StreamWriter Stdin { get; }
        public CancellationTokenSource Cancellation { get; }
        public Task? StdoutPump { get; private set; }
        public Task? StderrPump { get; private set; }

        public void StartPumps(WebSocketSession session)
        {
            StdoutPump = PumpOutputAsync(session, Process.StandardOutput, Cancellation.Token, "stdout");
            StderrPump = PumpOutputAsync(session, Process.StandardError, Cancellation.Token, "stderr");
        }

        public async Task ForwardInputAsync(string message, WebSocketSession session)
        {
            try
            {
                await _writeLock.WaitAsync(Cancellation.Token).ConfigureAwait(false);
                await Stdin.WriteAsync(message).ConfigureAwait(false);
                await Stdin.FlushAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // expected during shutdown
            }
            catch (Exception ex)
            {
                LogError(session, ex, "Failed to forward input to ConsoleGame for session {SessionID}", session.SessionID);
                await session.SendAsync($"Input error: {ex.Message}\n").ConfigureAwait(false);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            Stdin.Dispose();
            Cancellation.Dispose();
            Process.Dispose();
            _writeLock.Dispose();

            await SafeAwaitAsync(StdoutPump).ConfigureAwait(false);
            await SafeAwaitAsync(StderrPump).ConfigureAwait(false);
        }

        public void Dispose()
        {
            Stdin.Dispose();
            Cancellation.Dispose();
            Process.Dispose();
            _writeLock.Dispose();
        }
    }
    private static class TerminalProcessLocator
    {
        private const string ExecutableEnv = "CONSOLE_GAME_EXE";
        private const string ArgumentsEnv = "CONSOLE_GAME_ARGS";

        public static TerminalExecutable ResolveExecutablePath()
        {
            var explicitPath = Environment.GetEnvironmentVariable(ExecutableEnv);
            if (!string.IsNullOrWhiteSpace(explicitPath))
            {
                var fullPath = Path.GetFullPath(explicitPath);
                if (File.Exists(fullPath))
                {
                    return new TerminalExecutable(fullPath, ResolveArguments(), Path.GetDirectoryName(fullPath)!);
                }

                throw new FileNotFoundException($"ConsoleGame executable not found at '{fullPath}'. Build the project or update {ExecutableEnv}.");
            }

            var candidate = ProbeDefaultExecutable();
            if (candidate is not null)
            {
                return candidate;
            }

            throw new FileNotFoundException(
                "ConsoleGame executable could not be located. Set the CONSOLE_GAME_EXE environment variable after building ConsoleGame.App.");
        }

        private static TerminalExecutable? ProbeDefaultExecutable()
        {
            var relativePath = Path.Combine("src", "ConsoleGame.App", "bin", "Debug", "net8.0");
            var baseDirectory = AppContext.BaseDirectory;
            var probePaths = new[]
            {
                Path.Combine(baseDirectory, relativePath, "ConsoleGame.App.exe"),
                Path.Combine(baseDirectory, relativePath, "ConsoleGame.App"),
                Path.Combine(Directory.GetCurrentDirectory(), relativePath, "ConsoleGame.App.exe"),
                Path.Combine(Directory.GetCurrentDirectory(), relativePath, "ConsoleGame.App")
            };

            foreach (var path in probePaths)
            {
                if (File.Exists(path))
                {
                    return new TerminalExecutable(path, ResolveArguments(), Path.GetDirectoryName(path)!);
                }
            }

            return null;
        }

        private static string ResolveArguments()
        {
            return Environment.GetEnvironmentVariable(ArgumentsEnv) ?? string.Empty;
        }
    }

    private sealed record TerminalExecutable(string FileName, string Arguments, string WorkingDirectory);
}
