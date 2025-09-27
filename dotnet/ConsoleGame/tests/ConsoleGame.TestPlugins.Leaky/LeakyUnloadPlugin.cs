using ConsoleGame.Contracts;
using System.Collections.Generic;

namespace ConsoleGame.TestPlugins.Leaky;

[Plugin("consolegame.test.leaky", "Leaky Test Plugin", "0.1.0", Description = "Plugin that intentionally leaks references to prevent unload.")]
public sealed class LeakyUnloadPlugin : IPlugin, IRuntimePlugin
{
    private static readonly object LeakLock = new();
    private static readonly List<EventHandler> ActiveHandlers = new();
    private static bool _leakAttached;

    public string Name => "Leaky Test Plugin";

    public IPluginContext? Context { get; set; }

    public string Describe() => "Leaky plugin keeps references alive via AppDomain events.";

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        lock (LeakLock)
        {
            if (!_leakAttached)
            {
                var handler = new EventHandler(OnProcessExit);
                AppDomain.CurrentDomain.ProcessExit += handler;
                ActiveHandlers.Add(handler);
                _leakAttached = true;
            }
        }

        // Simulate brief work
        return Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
    }

    public Task PrepareUnloadAsync(CancellationToken cancellationToken = default)
    {
        // Intentionally skip removing event handler to keep leak
        return Task.CompletedTask;
    }

    public static void ResetLeak()
    {
        lock (LeakLock)
        {
            foreach (var handler in ActiveHandlers)
            {
                try
                {
                    AppDomain.CurrentDomain.ProcessExit -= handler;
                }
                catch
                {
                    // ignore best-effort cleanup
                }
            }
            ActiveHandlers.Clear();
            _leakAttached = false;
        }
    }

    private static void OnProcessExit(object? sender, EventArgs e)
    {
        // no-op
    }
}
