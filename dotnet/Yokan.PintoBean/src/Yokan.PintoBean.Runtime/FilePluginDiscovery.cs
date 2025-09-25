using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// File-based implementation of plugin discovery that scans directories for plugin manifest files.
/// </summary>
public sealed class FilePluginDiscovery : IPluginDiscovery
{
    private readonly string _manifestFileName;
    private readonly Version? _minimumVersion;
    private readonly string[]? _requiredCapabilities;
    private readonly bool _validateContractVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilePluginDiscovery"/> class.
    /// </summary>
    /// <param name="manifestFileName">The name of the manifest file to search for. Defaults to "plugin.json".</param>
    /// <param name="minimumVersion">The minimum required plugin version. If null, no version validation is performed.</param>
    /// <param name="requiredCapabilities">The capabilities that must be present in valid plugins. If null, no capability validation is performed.</param>
    /// <param name="validateContractVersion">Whether to validate contract version compatibility. Defaults to false for backwards compatibility.</param>
    public FilePluginDiscovery(
        string manifestFileName = "plugin.json",
        Version? minimumVersion = null,
        string[]? requiredCapabilities = null,
        bool validateContractVersion = false)
    {
        _manifestFileName = manifestFileName ?? throw new ArgumentNullException(nameof(manifestFileName));
        _minimumVersion = minimumVersion;
        _requiredCapabilities = requiredCapabilities;
        _validateContractVersion = validateContractVersion;
    }

    /// <inheritdoc />
    public async Task<PluginDescriptor[]> DiscoverPluginsAsync(string directory, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Directory cannot be null or whitespace.", nameof(directory));

        if (!Directory.Exists(directory))
            return Array.Empty<PluginDescriptor>();

        var results = new List<PluginDescriptor>();
        var manifestFiles = Directory.GetFiles(directory, _manifestFileName, SearchOption.AllDirectories);

        foreach (var manifestFile in manifestFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var descriptor = await LoadPluginFromManifestAsync(manifestFile, cancellationToken);
                if (descriptor != null)
                {
                    results.Add(descriptor);
                }
            }
            catch (Exception)
            {
                // Log and continue - individual plugin failures shouldn't stop discovery
                // In a real implementation, this would use a logger
                continue;
            }
        }

        return results.ToArray();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access", Justification = "PluginManifest is a simple data class with known properties")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling", Justification = "PluginManifest is a simple data class with known properties")]
    private async Task<PluginDescriptor?> LoadPluginFromManifestAsync(string manifestPath, CancellationToken cancellationToken)
    {
        var manifestContent = await File.ReadAllTextAsync(manifestPath, cancellationToken);
        var manifest = JsonSerializer.Deserialize<PluginManifest>(manifestContent);

        if (manifest == null || !manifest.IsValid())
            return null;

        // Validate version if required
        if (_minimumVersion != null && !ValidateVersion(manifest.Version))
            return null;

        // Validate capabilities if required
        if (_requiredCapabilities?.Length > 0 && !ValidateCapabilities(manifest.Capabilities))
            return null;

        // Validate contract version if required
        if (_validateContractVersion && !ValidateContractVersion(manifest.ContractVersion))
            return null;

        // Convert relative assembly paths to absolute paths relative to the manifest directory
        var manifestDir = Path.GetDirectoryName(manifestPath)!;
        var assemblyPaths = manifest.Assemblies
            .Select(path => Path.IsPathRooted(path) ? path : Path.Combine(manifestDir, path))
            .ToArray();

        // Create plugin descriptor
        var descriptor = new PluginDescriptor(manifest.Id, manifest.Version, assemblyPaths)
        {
            Name = manifest.Name,
            Description = manifest.Description,
            Author = manifest.Author,
            ContractVersion = manifest.ContractVersion
        };

        // Create capabilities from manifest
        if (manifest.Capabilities.Length > 0)
        {
            var capabilities = ProviderCapabilities.Create(manifest.Id)
                .WithTags(manifest.Capabilities);

            if (!string.IsNullOrWhiteSpace(manifest.EntryType))
            {
                capabilities = capabilities.AddMetadata("entryType", manifest.EntryType);
            }

            descriptor.Capabilities = capabilities;
        }

        // Store the manifest as metadata
        var manifestDict = new Dictionary<string, object>
        {
            ["originalManifest"] = manifest,
            ["manifestPath"] = manifestPath
        };

        descriptor.Manifest = manifestDict;

        return descriptor;
    }

    private bool ValidateVersion(string versionString)
    {
        if (_minimumVersion == null)
            return true;

        if (!Version.TryParse(versionString, out var pluginVersion))
            return false;

        return pluginVersion >= _minimumVersion;
    }

    private bool ValidateCapabilities(string[] pluginCapabilities)
    {
        if (_requiredCapabilities == null || _requiredCapabilities.Length == 0)
            return true;

        return _requiredCapabilities.All(required => 
            pluginCapabilities.Contains(required, StringComparer.OrdinalIgnoreCase));
    }

    private bool ValidateContractVersion(string? contractVersion)
    {
        return ContractVersioning.IsCompatible(contractVersion);
    }
}