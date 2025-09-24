using ConsoleGame.Contracts;

namespace ConsoleGame.TerminalLib;

[Plugin("consolegame.terminallib", "TerminalLib", "0.1.0", Description = "Simple sample plugin without external deps")]
public sealed class TuiPlugin : IPlugin, IRuntimePlugin
{
    public string Name => "TerminalLib Demo";

    public IPluginContext? Context { get; set; }

    public string Describe() => $"Plugin {Name} at {Context?.PluginDirectory ?? "?"}";

    public Task ConfigureAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Configure: {Name}");
        return Task.CompletedTask;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine(Describe());
        Console.WriteLine("Running... Press Ctrl+C to stop.");
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(200, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        Console.WriteLine("Stopped.");
    }
}
