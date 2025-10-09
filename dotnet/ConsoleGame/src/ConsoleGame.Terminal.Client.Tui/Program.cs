using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConsoleGame.Terminal.Transport;
using ConsoleGame.Terminal.Transport.Logging;
using Microsoft.Extensions.Logging;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Text;
using Terminal.Gui.Views;

try
{
    var pipeName = Environment.GetEnvironmentVariable("CONSOLE_GAME_PIPE_NAME") ?? "console-game-terminal";

    var logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
    Directory.CreateDirectory(logsDirectory);
    var logFilePath = Path.Combine(logsDirectory, $"tui-{DateTimeOffset.UtcNow:yyyyMMdd}.log");

    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole();
        builder.AddProvider(new FileLoggerProvider(logFilePath));
    });
    var logger = loggerFactory.CreateLogger("TerminalTui");

    await using var client = new NamedPipeTerminalClient(pipeName, logger);

    try
    {
        await client.ConnectAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to connect to named pipe '{pipeName}': {ex.Message}");
        Console.Error.WriteLine(ex.ToString());
        return;
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

    var outputView = new TextView
    {
        ReadOnly = true,
        WordWrap = false,
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = outputHeight
    };

    var inputField = new TextField
    {
        X = 0,
        Y = Pos.Bottom(outputView),
        Width = Dim.Fill(12),
        Height = 1
    };

    var sendButton = new Button
    {
        Text = "_Send",
        X = Pos.Right(inputField) + 1,
        Y = Pos.Top(inputField),
        Width = 10,
        Height = 1,
        IsDefault = true
    };

    var fillWidth = Dim.Fill();
    if (fillWidth is null)
    {
        fillWidth = Dim.Absolute(0);
    }

    var statusLabel = new Label
    {
        Text = $"Connected to {pipeName}. Ctrl+Q to quit.",
        X = 0,
        Y = Pos.Bottom(inputField) + 1,
        Width = fillWidth,
        Height = 1,
        TextAlignment = Alignment.Start
    };

    top.Add(outputView);
    top.Add(inputField);
    top.Add(sendButton);
    top.Add(statusLabel);

    var cts = new CancellationTokenSource();
    var outputBuffer = new StringBuilder();

    async Task SendInputAsync()
    {
        var text = inputField.Text?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var payload = Encoding.UTF8.GetBytes(text + "\n");
        try
        {
            await client.SendInputAsync(payload, cts.Token);
            inputField.Text = string.Empty;
        }
        catch (Exception ex)
        {
            statusLabel.Text = $"Send failed: {ex.Message}";
            Console.Error.WriteLine($"[TUI] Send error: {ex}");
        }
    }

    inputField.Accepting += (_, args) =>
    {
        args.Handled = true;
        _ = SendInputAsync();
    };

    sendButton.Accepting += (_, args) =>
    {
        args.Handled = true;
        _ = SendInputAsync();
    };

    var readerTask = Task.Run(async () =>
    {
        try
        {
            await foreach (var frame in client.ReadFramesAsync(cts.Token))
            {
                var text = Encoding.UTF8.GetString(frame.Payload);
                Application.Invoke(() =>
                {
                    switch (frame.Type)
                    {
                        case TerminalFrameType.Stdout:
                        case TerminalFrameType.Stderr:
                            outputBuffer.Append(text);
                            outputView.Text = outputBuffer.ToString();
                            outputView.MoveEnd();
                            break;
                        case TerminalFrameType.Control:
                            statusLabel.Text = text.TrimEnd();
                            break;
                    }
                });
            }
        }
        catch (OperationCanceledException)
        {
            // expected during shutdown
        }
        catch (Exception ex)
        {
            Application.Invoke(() => statusLabel.Text = $"Connection error: {ex.Message}");
            Console.Error.WriteLine($"[TUI] Frame reader error: {ex}");
        }
    });

    try
    {
        Application.Run(top);
    }
    finally
    {
        cts.Cancel();
        await readerTask.ConfigureAwait(false);
        Application.Shutdown();
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine("[TUI] Unhandled exception:");
    Console.Error.WriteLine(ex.ToString());
    Console.Error.Flush();
    throw;
}
