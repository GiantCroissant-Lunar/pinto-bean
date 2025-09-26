using System;
using System.Collections.Generic;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Describes a plugin that can be loaded by the plugin host.
/// </summary>
public sealed class PluginDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginDescriptor"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the plugin.</param>
    /// <param name="version">The version of the plugin.</param>
    /// <param name="assemblyPaths">The paths to the plugin assemblies.</param>
    public PluginDescriptor(string id, string version, IEnumerable<string> assemblyPaths)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Plugin ID cannot be null or whitespace.", nameof(id));
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Plugin version cannot be null or whitespace.", nameof(version));

        Id = id;
        Version = version;
        AssemblyPaths = assemblyPaths?.ToArray() ?? throw new ArgumentNullException(nameof(assemblyPaths));

        if (AssemblyPaths.Count == 0)
            throw new ArgumentException("At least one assembly path must be provided.", nameof(assemblyPaths));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginDescriptor"/> class with a single assembly.
    /// </summary>
    /// <param name="id">The unique identifier for the plugin.</param>
    /// <param name="version">The version of the plugin.</param>
    /// <param name="assemblyPath">The path to the plugin assembly.</param>
    public PluginDescriptor(string id, string version, string assemblyPath)
        : this(id, version, new[] { assemblyPath })
    {
    }

    /// <summary>
    /// Gets the unique identifier for the plugin.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the version of the plugin.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the paths to the plugin assemblies.
    /// </summary>
    public IReadOnlyList<string> AssemblyPaths { get; }

    /// <summary>
    /// Gets or sets the plugin manifest (optional metadata).
    /// </summary>
    public IReadOnlyDictionary<string, object>? Manifest { get; set; }

    /// <summary>
    /// Gets or sets the plugin capabilities.
    /// </summary>
    public ProviderCapabilities? Capabilities { get; set; }

    /// <summary>
    /// Gets or sets the display name of the plugin.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the plugin description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the plugin author.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Gets or sets the contract version this plugin was compiled against.
    /// Used for compatibility validation during plugin loading.
    /// </summary>
    public string? ContractVersion { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"{Id} v{Version}";

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is PluginDescriptor other && Id == other.Id && Version == other.Version;

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Id, Version);
}