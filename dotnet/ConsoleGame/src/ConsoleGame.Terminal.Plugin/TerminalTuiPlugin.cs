using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConsoleGame.Contracts;
using ConsoleGame.Terminal.Transport;
using ConsoleGame.Terminal.Transport.Logging;
using Microsoft.Extensions.Logging;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Text;
using Terminal.Gui.Views;

namespace ConsoleGame.Terminal.Plugin;

[Plugin("consolegame.terminal.tui", "Terminal TUI Plugin", "1.0.0",
    Description = "Plugin that provides TUI terminal client functionality using Terminal.Gui")]
public class TerminalTuiPlugin : IPlugin, IRuntimePlugin
{
    private readonly ILogger<TerminalTuiPlugin> _logger;
    private readonly NamedPipeTerminalClient _client;
    private CancellationTokenSource? _cancellationTokenSource;
    private TextView? _outputView;
    private TextField? _inputField;
    private Label? _statusLabel;
    private StringBuilder _outputBuffer = new();

    public TerminalTuiPlugin(
        ILogger<TerminalTuiPlugin> logger,
        NamedPipeTerminalClient client)
    {
        _logger = logger;
        _client = client;
    }

    public IPluginContext? Context { get; set; }

    public string Name => "Terminal TUI Plugin";

    public string Describe() => $"Plugin {Name} - Provides TUI terminal client";

    public async Task ConfigureAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Configuring Terminal TUI Plugin");

        var pipeName = Environment.GetEnvironmentVariable("CONSOLE_GAME_PIPE_NAME") ?? "console-game-terminal";

        // Set up file logging
        var logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logsDirectory);
        var logFilePath = Path.Combine(logsDirectory, $"tui-{DateTimeOffset.UtcNow:yyyyMMdd}.log");

        try
        {
            await _client.ConnectAsync(cancellationToken);
            _logger.LogInformation("Connected to named pipe {PipeName}", pipeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to named pipe '{PipeName}'", pipeName);
            throw;
        }

        Application.Init();

        var top = new Toplevel
        {
            Title = $"ConsoleGame TUI ({pipeName})",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        var outputHeight = Dim.Fill();
        if (outputHeight is null)
        {
            outputHeight = Dim.Absolute(0);
        }
        outputHeight -= Dim.Absolute(3);

        _outputView = new TextView
        {
            ReadOnly = true,
            WordWrap = false,
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = outputHeight
        };

        _inputField = new TextField
        {
            X = 0,
            Y = Pos.Bottom(_outputView),
            Width = Dim.Fill(12),
            Height = 1
        };

        var sendButton = new Button
        {
            Text = "_Send",
            X = Pos.Right(_inputField) + 1,
            Y = Pos.Top(_inputField),
            Width = 10,
            Height = 1,
            IsDefault = true
        };

        var fillWidth = Dim.Fill();
        if (fillWidth is null)
        {
            fillWidth = Dim.Absolute(0);
        }

        _statusLabel = new Label
        {
            Text = $"Connected to {pipeName}. Ctrl+Q to quit.",
            X = 0,
            Y = Pos.Bottom(_inputField) + 1,
            Width = fillWidth,
            Height = 1,
            TextAlignment = Alignment.Start
        };

        top.Add(_outputView);
        top.Add(_inputField);
        top.Add(sendButton);
        top.Add(_statusLabel);

        _cancellationTokenSource = new CancellationTokenSource();

        // Set up input handling
        async Task SendInputAsync()
        {
            var text = _inputField?.Text?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var payload = Encoding.UTF8.GetBytes(text + "\n");
            try
            {
                await _client.SendInputAsync(payload, _cancellationTokenSource.Token);
                _inputField!.Text = string.Empty;
            }
            catch (Exception ex)
            {
                _statusLabel!.Text = $"Send failed: {ex.Message}";
                _logger.LogError(ex, "Send error");
            }
        }

        _inputField.Accepting += (_, args) =>
        {
            args.Handled = true;
            _ = SendInputAsync();
        };

        sendButton.Accepting += (_, args) =>
        {
            args.Handled = true;
            _ = SendInputAsync();
        };

        // Set up frame reading
        var readerTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var frame in _client.ReadFramesAsync(_cancellationTokenSource.Token))
                {
                    var text = Encoding.UTF8.GetString(frame.Payload);
                    Application.Invoke(() =>
                    {
                        switch (frame.Type)
                        {
                            case TerminalFrameType.Stdout:
                            case TerminalFrameType.Stderr:
                                _outputBuffer.Append(text);
                                _outputView!.Text = _outputBuffer.ToString();
                                _outputView.MoveEnd();
                                break;
                            case TerminalFrameType.Control:
                                _statusLabel!.Text = text.TrimEnd();
                                break;
                        }
                    });
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                Application.Invoke(() => _statusLabel!.Text = $"Connection error: {ex.Message}");
                _logger.LogError(ex, "Frame reader error");
            }
        }, _cancellationTokenSource.Token);

        try
        {
            Application.Run(top);
        }
        finally
        {
            _cancellationTokenSource.Cancel();
            await readerTask.ConfigureAwait(false);
            Application.Shutdown();
        }
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Terminal TUI Plugin running");

        // The RunAsync method is handled by the ConfigureAsync method in this case
        // since the TUI needs to run the Application.Run loop
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping Terminal TUI Plugin");

        _cancellationTokenSource?.Cancel();
        await _client.DisposeAsync();

        _logger.LogInformation("Terminal TUI Plugin stopped");
    }
}
