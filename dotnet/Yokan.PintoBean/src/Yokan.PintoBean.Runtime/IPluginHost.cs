using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Manages the lifecycle of plugins in isolated load contexts.
/// </summary>
public interface IPluginHost : IDisposable
{
    /// <summary>
    /// Gets all currently loaded plugins.
    /// </summary>
    IReadOnlyCollection<PluginHandle> LoadedPlugins { get; }

    /// <summary>
    /// Occurs when a plugin is successfully loaded.
    /// </summary>
    event EventHandler<PluginLoadedEventArgs>? PluginLoaded;

    /// <summary>
    /// Occurs when a plugin is unloaded.
    /// </summary>
    event EventHandler<PluginUnloadedEventArgs>? PluginUnloaded;

    /// <summary>
    /// Occurs when a plugin operation fails.
    /// </summary>
    event EventHandler<PluginFailedEventArgs>? PluginFailed;

    /// <summary>
    /// Loads a plugin from the specified descriptor.
    /// </summary>
    /// <param name="descriptor">The plugin descriptor.</param>
    /// <returns>A handle to the loaded plugin.</returns>
    /// <exception cref="ArgumentNullException">Thrown when descriptor is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the plugin cannot be loaded.</exception>
    Task<PluginHandle> LoadPluginAsync(PluginDescriptor descriptor);

    /// <summary>
    /// Activates a loaded plugin.
    /// </summary>
    /// <param name="pluginId">The ID of the plugin to activate.</param>
    /// <returns>true if the plugin was activated successfully; otherwise, false.</returns>
    Task<bool> ActivateAsync(string pluginId);

    /// <summary>
    /// Deactivates an active plugin.
    /// </summary>
    /// <param name="pluginId">The ID of the plugin to deactivate.</param>
    /// <returns>true if the plugin was deactivated successfully; otherwise, false.</returns>
    Task<bool> DeactivateAsync(string pluginId);

    /// <summary>
    /// Unloads a plugin and disposes its load context.
    /// </summary>
    /// <param name="pluginId">The ID of the plugin to unload.</param>
    /// <returns>true if the plugin was unloaded successfully; otherwise, false.</returns>
    Task<bool> UnloadAsync(string pluginId);

    /// <summary>
    /// Gets a plugin handle by ID.
    /// </summary>
    /// <param name="pluginId">The ID of the plugin.</param>
    /// <returns>The plugin handle if found; otherwise, null.</returns>
    PluginHandle? GetPlugin(string pluginId);

    /// <summary>
    /// Checks if a plugin is loaded.
    /// </summary>
    /// <param name="pluginId">The ID of the plugin.</param>
    /// <returns>true if the plugin is loaded; otherwise, false.</returns>
    bool IsLoaded(string pluginId);
}

/// <summary>
/// Provides data for the PluginLoaded event.
/// </summary>
public sealed class PluginLoadedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginLoadedEventArgs"/> class.
    /// </summary>
    /// <param name="handle">The plugin handle.</param>
    public PluginLoadedEventArgs(PluginHandle handle)
    {
        Handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    /// <summary>
    /// Gets the plugin handle.
    /// </summary>
    public PluginHandle Handle { get; }
}

/// <summary>
/// Provides data for the PluginUnloaded event.
/// </summary>
public sealed class PluginUnloadedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginUnloadedEventArgs"/> class.
    /// </summary>
    /// <param name="pluginId">The ID of the unloaded plugin.</param>
    /// <param name="descriptor">The plugin descriptor.</param>
    public PluginUnloadedEventArgs(string pluginId, PluginDescriptor descriptor)
    {
        PluginId = pluginId ?? throw new ArgumentNullException(nameof(pluginId));
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
    }

    /// <summary>
    /// Gets the ID of the unloaded plugin.
    /// </summary>
    public string PluginId { get; }

    /// <summary>
    /// Gets the plugin descriptor.
    /// </summary>
    public PluginDescriptor Descriptor { get; }
}

/// <summary>
/// Provides data for the PluginFailed event.
/// </summary>
public sealed class PluginFailedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginFailedEventArgs"/> class.
    /// </summary>
    /// <param name="pluginId">The ID of the failed plugin.</param>
    /// <param name="operation">The operation that failed.</param>
    /// <param name="exception">The exception that occurred.</param>
    public PluginFailedEventArgs(string pluginId, string operation, Exception exception)
    {
        PluginId = pluginId ?? throw new ArgumentNullException(nameof(pluginId));
        Operation = operation ?? throw new ArgumentNullException(nameof(operation));
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }

    /// <summary>
    /// Gets the ID of the failed plugin.
    /// </summary>
    public string PluginId { get; }

    /// <summary>
    /// Gets the operation that failed.
    /// </summary>
    public string Operation { get; }

    /// <summary>
    /// Gets the exception that occurred.
    /// </summary>
    public Exception Exception { get; }
}