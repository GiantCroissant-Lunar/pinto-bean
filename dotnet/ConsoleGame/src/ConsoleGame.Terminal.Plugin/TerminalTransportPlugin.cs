using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ConsoleGame.Contracts;
using ConsoleGame.Terminal.Transport;
using Microsoft.Extensions.Logging;
using SuperSocket;
using SuperSocket.WebSocket;
using SuperSocket.WebSocket.Server;
using CloseReason = SuperSocket.Connection.CloseReason;

namespace ConsoleGame.Terminal.Plugin;

[Plugin("consolegame.terminal.transport", "Terminal Transport Plugin", "1.0.0",
    Description = "Plugin that provides terminal transport functionality including WebSocket server and named pipe server")]
public class TerminalTransportPlugin : IPlugin, IRuntimePlugin
{
    private readonly ILogger<TerminalTransportPlugin> _logger;
    private NamedPipeTerminalServer? _pipeServer;
    private WebSocketHost? _webSocketHost;
    private CancellationTokenSource? _cancellationTokenSource;

    public TerminalTransportPlugin(ILogger<TerminalTransportPlugin> logger)
    {
        _logger = logger;
    }

    public IPluginContext? Context { get; set; }

    public string Name => "Terminal Transport Plugin";

    public string Describe() => $"Plugin {Name} - Provides terminal transport services";

    public async Task ConfigureAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Configuring Terminal Transport Plugin");

        // Create WebSocket server
        _webSocketHost = WebSocketHostBuilder.Create()
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
                        Port = 4040
                    }
                ];
            })
            .ConfigureLogging((_, logging) =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddConsole();
            })
            .Build();

        // Start WebSocket server
        await _webSocketHost.StartAsync(cancellationToken);
        _logger.LogInformation("WebSocket server started on port 4040");

        // Start named pipe server
        var pipeName = "console-game-terminal";
        _pipeServer = new NamedPipeTerminalServer(pipeName, null, _logger);
        _pipeServer.Start();
        _logger.LogInformation("Named pipe server started on {PipeName}", pipeName);

        _logger.LogInformation("Terminal Transport Plugin configured and started");
    }

    private static async ValueTask HandleMessageAsync(WebSocketSession session, WebSocketPackage package)
    {
        // Handle incoming WebSocket messages - forward to terminal
        if (string.IsNullOrEmpty(package.Message))
        {
            return;
        }

        var payload = System.Text.Encoding.UTF8.GetBytes(package.Message);
        // TODO: Forward to active terminal sessions
    }

    private static async ValueTask OnSessionConnectedAsync(IAppSession session)
    {
        if (session is not WebSocketSession wsSession)
        {
            await session.CloseAsync(CloseReason.InternalError);
            return;
        }

        // TODO: Register WebSocket client with terminal session
        await wsSession.SendAsync("[connected] ConsoleGame Terminal Plugin ready.\n");
    }

    private static async ValueTask OnSessionClosedAsync(IAppSession session, CloseEventArgs e)
    {
        // Handle session cleanup
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Terminal Transport Plugin running");

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            // Keep the plugin running
            await Task.Delay(Timeout.Infinite, _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping Terminal Transport Plugin");

        _cancellationTokenSource?.Cancel();

        // Stop servers
        if (_webSocketHost is not null)
        {
            await _webSocketHost.StopAsync(cancellationToken);
        }

        if (_pipeServer is not null)
        {
            await _pipeServer.DisposeAsync().ConfigureAwait(false);
        }

        _logger.LogInformation("Terminal Transport Plugin stopped");
    }
}
