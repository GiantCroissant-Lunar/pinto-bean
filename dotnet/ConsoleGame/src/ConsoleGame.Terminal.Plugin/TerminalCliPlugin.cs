using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConsoleGame.Contracts;
using ConsoleGame.Terminal.Transport;
using Microsoft.Extensions.Logging;

namespace ConsoleGame.Terminal.Plugin;

[Plugin("consolegame.terminal.cli", "Terminal CLI Plugin", "1.0.0",
    Description = "Plugin that provides CLI terminal client functionality")]
public class TerminalCliPlugin : IPlugin, IRuntimePlugin
{
    private readonly ILogger<TerminalCliPlugin> _logger;
    private readonly NamedPipeTerminalClient _client;
    private CancellationTokenSource? _cancellationTokenSource;

    public TerminalCliPlugin(
        ILogger<TerminalCliPlugin> logger,
        NamedPipeTerminalClient client)
    {
        _logger = logger;
        _client = client;
    }

    public IPluginContext? Context { get; set; }

    public string Name => "Terminal CLI Plugin";

    public string Describe() => $"Plugin {Name} - Provides CLI terminal client";

    public async Task ConfigureAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Configuring Terminal CLI Plugin");

        var pipeName = Environment.GetEnvironmentVariable("CONSOLE_GAME_PIPE_NAME") ?? "console-game-terminal";
        Console.WriteLine($"Connecting to pipe '{pipeName}'...");

        await _client.ConnectAsync(cancellationToken);
        Console.WriteLine("Connected. Type text and press Enter to send. Ctrl+C to exit.");
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Terminal CLI Plugin running");

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Set up Ctrl+C handler
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            _cancellationTokenSource?.Cancel();
        };

        var readTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var frame in _client.ReadFramesAsync(_cancellationTokenSource.Token))
                {
                    switch (frame.Type)
                    {
                        case TerminalFrameType.Stdout:
                            Console.Out.Write(Encoding.UTF8.GetString(frame.Payload));
                            break;
                        case TerminalFrameType.Stderr:
                            Console.Error.Write(Encoding.UTF8.GetString(frame.Payload));
                            break;
                        case TerminalFrameType.Control:
                            Console.Out.WriteLine($"[control] {Encoding.UTF8.GetString(frame.Payload)}");
                            break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
            }
        }, _cancellationTokenSource.Token);

        // Read from stdin and send to pipe
        try
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var line = await Console.In.ReadLineAsync();
                if (line is null)
                {
                    _logger.LogDebug("STDIN returned null; ending CLI input loop");
                    break;
                }

                _logger.LogDebug("Read line from STDIN: {Line}", line);
                var payload = Encoding.UTF8.GetBytes(line + "\n");
                _logger.LogDebug("Sending payload ({Length} bytes) to named pipe", payload.Length);
                await _client.SendInputAsync(payload, _cancellationTokenSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }

        _cancellationTokenSource.Cancel();
        await readTask.ConfigureAwait(false);
        Console.WriteLine("Disconnected.");
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping Terminal CLI Plugin");

        _cancellationTokenSource?.Cancel();
        await _client.DisposeAsync();

        _logger.LogInformation("Terminal CLI Plugin stopped");
    }
}
