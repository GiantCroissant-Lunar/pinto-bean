using ConsoleGame.Terminal.Transport;
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

var options = new TerminalProcessOptions
{
    FileName = "dotnet",
    Arguments = "--info"
};

await using var host = TerminalProcessHost.Start(options, loggerFactory);
var pipeServer = new NamedPipeTerminalServer("consolegame-terminal-test", host.Session, loggerFactory.CreateLogger<NamedPipeTerminalServer>());
pipeServer.Start();

Console.WriteLine("Test host started. Press Enter to exit.");
Console.ReadLine();
await pipeServer.DisposeAsync();
