using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Represents a plugin manifest that describes a plugin's metadata and capabilities.
/// </summary>
public sealed class PluginManifest
{
    /// <summary>
    /// Gets or sets the unique identifier for the plugin.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of the plugin.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the paths to the plugin assemblies.
    /// </summary>
    [JsonPropertyName("assemblies")]
    public string[] Assemblies { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the capabilities provided by this plugin.
    /// </summary>
    [JsonPropertyName("capabilities")]
    public string[] Capabilities { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the fully qualified name of the plugin's entry type.
    /// </summary>
    [JsonPropertyName("entryType")]
    public string? EntryType { get; set; }

    /// <summary>
    /// Gets or sets the display name of the plugin (optional).
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the plugin description (optional).
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the plugin author (optional).
    /// </summary>
    [JsonPropertyName("author")]
    public string? Author { get; set; }

    /// <summary>
    /// Gets or sets the contract version this plugin was compiled against.
    /// Used for compatibility validation during plugin loading.
    /// </summary>
    [JsonPropertyName("contractVersion")]
    public string? ContractVersion { get; set; }

    /// <summary>
    /// Validates that the manifest contains required fields.
    /// </summary>
    /// <returns>True if the manifest is valid, false otherwise.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Id) &&
               !string.IsNullOrWhiteSpace(Version) &&
               Assemblies.Length > 0 &&
               Assemblies.All(a => !string.IsNullOrWhiteSpace(a));
    }
}