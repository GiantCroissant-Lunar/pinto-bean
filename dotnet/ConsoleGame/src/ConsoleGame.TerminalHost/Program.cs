using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleGame.Terminal.Transport;
using ConsoleGame.Terminal.Transport.Logging;
using Microsoft.Extensions.DependencyInjection;
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
    private static readonly ConcurrentDictionary<string, TerminalClientHandle> ClientRegistry = new();
    private static TerminalProcessHost? _processHost;
    private static NamedPipeTerminalServer? _pipeServer;

    public static async Task Main(string[] args)
    {
        var logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logsDirectory);
        var logFilePath = Path.Combine(logsDirectory, $"host-{DateTimeOffset.UtcNow:yyyyMMdd}.log");

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
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddConsole();
                logging.AddProvider(new FileLoggerProvider(logFilePath));
            })
            .Build();

        TerminalExecutable executable;
        try
        {
            executable = TerminalProcessLocator.ResolveExecutablePath();
        }
        catch (Exception ex)
        {
            var startupFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddConsole();
                builder.AddProvider(new FileLoggerProvider(logFilePath));
            });
            var startupLogger = startupFactory.CreateLogger("TerminalStartup");
            startupLogger.LogCritical(ex, "Failed to resolve ConsoleGame executable");
            Console.Error.WriteLine($"[TerminalHost] Failed to resolve ConsoleGame executable: {ex.Message}");
            Console.Error.WriteLine(ex);
            startupFactory.Dispose();
            return;
        }

        var serviceProvider = host.Services;
        var existingLoggerFactory = serviceProvider?.GetService<ILoggerFactory>();
        var ownsLoggerFactory = existingLoggerFactory is null;
        var loggerFactory = existingLoggerFactory ?? LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
            builder.AddProvider(new FileLoggerProvider(logFilePath));
        });
        var hostLogger = loggerFactory.CreateLogger("TerminalHost");

        hostLogger.LogInformation("ConsoleGame Terminal Host starting. Executable: {Executable} {Arguments}", executable.FileName, executable.Arguments);

        try
        {
            var processOptions = new TerminalProcessOptions
            {
                FileName = executable.FileName,
                Arguments = executable.Arguments,
                WorkingDirectory = executable.WorkingDirectory
            };
            processOptions.EnvironmentVariables["CONSOLEGAME_TUI_HEADLESS"] = "1";

            _processHost = TerminalProcessHost.Start(processOptions, loggerFactory);
            hostLogger.LogInformation("ConsoleGame process started with PID {Pid}", _processHost.ProcessId);

            var pipeName = ResolvePipeName();
            _pipeServer = new NamedPipeTerminalServer(pipeName, _processHost.Session, loggerFactory.CreateLogger<NamedPipeTerminalServer>());
            _pipeServer.Start();
            hostLogger.LogInformation("Named pipe server started on {PipeName}", pipeName);

            await host.RunAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            hostLogger.LogCritical(ex, "Unhandled exception in ConsoleGame Terminal Host");
            Console.Error.WriteLine($"[TerminalHost] Unhandled exception: {ex.Message}");
            Console.Error.WriteLine(ex);
            throw;
        }
        finally
        {
            if (_pipeServer is not null)
            {
                await _pipeServer.DisposeAsync().ConfigureAwait(false);
            }

            if (_processHost is not null)
            {
                await _processHost.DisposeAsync().ConfigureAwait(false);
            }

            if (ownsLoggerFactory && loggerFactory is IDisposable disposableFactory)
            {
                disposableFactory.Dispose();
            }
        }
    }

    private static async ValueTask HandleMessageAsync(WebSocketSession session, WebSocketPackage package)
    {
        if (!ClientRegistry.TryGetValue(session.SessionID, out var handle))
        {
            return;
        }

        if (string.IsNullOrEmpty(package.Message))
        {
            return;
        }

        var payload = Encoding.UTF8.GetBytes(package.Message);
        await handle.ForwardInputAsync(payload).ConfigureAwait(false);
    }

    private static async ValueTask OnSessionConnectedAsync(IAppSession session)
    {
        if (session is not WebSocketSession wsSession)
        {
            await session.CloseAsync(CloseReason.InternalError);
            return;
        }

        if (_processHost is null)
        {
            await wsSession.SendAsync("Terminal host unavailable.\n");
            await wsSession.CloseAsync(CloseReason.InternalError);
            return;
        }

        LogInformation(wsSession, "Session {SessionID} connected", session.SessionID);

        TerminalClientHandle handle;
        try
        {
            handle = _processHost.Session.RegisterClient($"ws:{session.SessionID}", async (frame, ct) =>
            {
                if (frame.Payload.Length == 0)
                {
                    return;
                }

                try
                {
                    await wsSession.SendAsync(frame.Payload).ConfigureAwait(false);
                }
                catch
                {
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            LogError(wsSession, ex, "Failed to register WebSocket client {SessionID}", session.SessionID);
            await wsSession.SendAsync("Internal error registering client.\n");
            await wsSession.CloseAsync(CloseReason.InternalError);
            return;
        }

        if (!ClientRegistry.TryAdd(session.SessionID, handle))
        {
            await wsSession.SendAsync("Internal error: session registry conflict.\n");
            await wsSession.CloseAsync(CloseReason.InternalError);
            await handle.DisposeAsync().ConfigureAwait(false);
            return;
        }

        await wsSession.SendAsync($"[connected] pipe={ResolvePipeName()}\n");
    }

    private static async ValueTask OnSessionClosedAsync(
        IAppSession session,
        CloseEventArgs e)
    {
        if (ClientRegistry.TryRemove(session.SessionID, out var handle))
        {
            try
            {
                await handle.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogWarning(session, ex, "Error disposing client handle for session {SessionID}", session.SessionID);
            }
        }

        LogInformation(session, "Session {SessionID} closed ({Reason})", session.SessionID, e.Reason);
    }

    private static int ResolvePort()
    {
        var portValue = Environment.GetEnvironmentVariable("CONSOLE_GAME_WS_PORT");
        return int.TryParse(portValue, out var port) ? port : 4040;
    }

    private static string ResolvePipeName()
    {
        var pipeValue = Environment.GetEnvironmentVariable("CONSOLE_GAME_PIPE_NAME");
        return string.IsNullOrWhiteSpace(pipeValue) ? "console-game-terminal" : pipeValue;
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
            var candidateFileNames = new[] { "ConsoleGame.App.exe", "ConsoleGame.App" };
            var configurations = new[] { "Debug", "Release" };
            var frameworks = new[] { "net8.0" };
            var searchRoots = new[]
            {
                AppContext.BaseDirectory,
                Directory.GetCurrentDirectory(),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."))
            }
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(root => new DirectoryInfo(root))
            .Where(dir => dir.Exists)
            .ToArray();

            foreach (var root in searchRoots)
            {
                for (var current = root; current is not null; current = current.Parent)
                {
                    foreach (var configuration in configurations)
                    {
                        foreach (var framework in frameworks)
                        {
                            foreach (var relative in new[]
                            {
                                Path.Combine("ConsoleGame.App", "bin", configuration, framework),
                                Path.Combine("src", "ConsoleGame.App", "bin", configuration, framework)
                            })
                            {
                                var candidateDirectory = Path.Combine(current.FullName, relative);
                                if (!Directory.Exists(candidateDirectory))
                                {
                                    continue;
                                }

                                foreach (var fileName in candidateFileNames)
                                {
                                    var fullPath = Path.Combine(candidateDirectory, fileName);
                                    if (!File.Exists(fullPath))
                                    {
                                        continue;
                                    }

                                    return new TerminalExecutable(fullPath, ResolveArguments(), candidateDirectory);
                                }
                            }
                        }
                    }
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
