using System.Text;
using ConsoleGame.Terminal.Transport;
using Microsoft.Extensions.Logging;

var pipeName = Environment.GetEnvironmentVariable("CONSOLE_GAME_PIPE_NAME") ?? "console-game-terminal";

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Debug);
});
var logger = loggerFactory.CreateLogger("TerminalCli");

await using var client = new NamedPipeTerminalClient(pipeName, logger);

Console.WriteLine($"Connecting to pipe '{pipeName}'...");
await client.ConnectAsync();
Console.WriteLine("Connected. Type text and press Enter to send. Ctrl+C to exit.");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var readTask = Task.Run(async () =>
{
    try
    {
        await foreach (var frame in client.ReadFramesAsync(cts.Token))
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
        // expected on shutdown
    }
});

while (!cts.IsCancellationRequested)
{
    var line = await Console.In.ReadLineAsync();
    if (line is null)
    {
        logger.LogDebug("STDIN returned null; ending CLI input loop");
        break;
    }

    logger.LogDebug("Read line from STDIN: {Line}", line);
    var payload = Encoding.UTF8.GetBytes(line + "\n");
    logger.LogDebug("Sending payload ({Length} bytes) to named pipe", payload.Length);
    await client.SendInputAsync(payload, cts.Token);
}

cts.Cancel();
await readTask.ConfigureAwait(false);
Console.WriteLine("Disconnected.");
