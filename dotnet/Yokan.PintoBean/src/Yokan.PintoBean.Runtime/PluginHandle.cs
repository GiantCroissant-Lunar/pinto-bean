using System;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Represents the runtime state of a loaded plugin.
/// </summary>
public sealed class PluginHandle
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginHandle"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the plugin.</param>
    /// <param name="loadContext">The load context containing the plugin.</param>
    /// <param name="descriptor">The plugin descriptor.</param>
    public PluginHandle(string id, ILoadContext loadContext, PluginDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Plugin ID cannot be null or whitespace.", nameof(id));

        Id = id;
        LoadContext = loadContext ?? throw new ArgumentNullException(nameof(loadContext));
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        State = PluginState.Loaded;
        LoadedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the unique identifier for the plugin.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the load context containing the plugin.
    /// </summary>
    public ILoadContext LoadContext { get; }

    /// <summary>
    /// Gets the plugin descriptor.
    /// </summary>
    public PluginDescriptor Descriptor { get; }

    /// <summary>
    /// Gets the current state of the plugin.
    /// </summary>
    public PluginState State { get; internal set; }

    /// <summary>
    /// Gets the timestamp when the plugin was loaded.
    /// </summary>
    public DateTimeOffset LoadedAt { get; }

    /// <summary>
    /// Gets the timestamp when the plugin was activated, if applicable.
    /// </summary>
    public DateTimeOffset? ActivatedAt { get; internal set; }

    /// <summary>
    /// Gets the timestamp when the plugin was deactivated, if applicable.
    /// </summary>
    public DateTimeOffset? DeactivatedAt { get; internal set; }

    /// <summary>
    /// Gets the plugin instance, if it has been created.
    /// </summary>
    public object? Instance { get; internal set; }

    /// <summary>
    /// Gets the last error that occurred with this plugin, if any.
    /// </summary>
    public Exception? LastError { get; internal set; }

    /// <summary>
    /// Gets the timestamp when the plugin was quiesced for soft-swap, if applicable.
    /// </summary>
    public DateTimeOffset? QuiescedAt { get; internal set; }

    /// <summary>
    /// Gets the grace period in seconds for quiescing, if applicable.
    /// </summary>
    public int? GracePeriodSeconds { get; internal set; }

    /// <inheritdoc />
    public override string ToString() => $"{Id} ({State})";

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is PluginHandle other && Id == other.Id;

    /// <inheritdoc />
    public override int GetHashCode() => Id.GetHashCode();
}

/// <summary>
/// Represents the possible states of a plugin.
/// </summary>
public enum PluginState
{
    /// <summary>
    /// The plugin is loaded but not yet activated.
    /// </summary>
    Loaded,

    /// <summary>
    /// The plugin is active and running.
    /// </summary>
    Active,

    /// <summary>
    /// The plugin has been deactivated but is still loaded.
    /// </summary>
    Deactivated,

    /// <summary>
    /// The plugin is being quiesced for soft-swap (Unity path).
    /// </summary>
    Quiescing,

    /// <summary>
    /// The plugin has been unloaded from memory.
    /// </summary>
    Unloaded,

    /// <summary>
    /// The plugin is in a failed state due to an error.
    /// </summary>
    Failed
}