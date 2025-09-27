using ConsoleGame.Contracts;

namespace ConsoleGame.TestPlugins.Sample;

[Plugin("consolegame.test.sample", "Sample Test Plugin", "0.1.0", Description = "Minimal plugin used for unload testing.")]
public sealed class SampleUnloadPlugin : IPlugin, IRuntimePlugin
{
    public string Name => "Sample Test Plugin";

    public IPluginContext? Context { get; set; }

    public string Describe() => "Sample test plugin for unload verification.";

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        return Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
    }
}
