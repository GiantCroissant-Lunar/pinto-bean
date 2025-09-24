using System.Threading;
using System.Threading.Tasks;

namespace ConsoleGame.Contracts;

/// <summary>
/// Minimal plugin contract with optional enhancements for metadata, context, configuration and unload.
/// Inspired by GameConsole.Plugins.Core but simplified to avoid external dependencies.
/// </summary>
public interface IPlugin
{
    /// <summary>Human-friendly name for the plugin.</summary>
    string Name { get; }

    /// <summary>Returns a brief description/status string.</summary>
    string Describe();

    /// <summary>Optional metadata about the plugin (null if not provided).</summary>
    IPluginMetadata? Metadata => null;

    /// <summary>Optional runtime context provided by host.</summary>
    IPluginContext? Context
    {
        get => null;
        set { /* no-op default */ }
    }

    /// <summary>Optional configuration hook before initialization.</summary>
    Task ConfigureAsync(IPluginContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>Optional check whether plugin can be safely unloaded.</summary>
    Task<bool> CanUnloadAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    /// <summary>Optional cleanup hook before unloading.</summary>
    Task PrepareUnloadAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
