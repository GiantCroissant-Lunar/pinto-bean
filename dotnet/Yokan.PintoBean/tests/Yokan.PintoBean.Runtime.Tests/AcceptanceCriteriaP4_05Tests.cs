using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Acceptance criteria tests for P4-05: Plugin manifest + discovery (file-based) with capability validation.
/// Tests that only valid manifests are loaded while invalid ones are rejected.
/// </summary>
public class AcceptanceCriteriaP4_05Tests : IDisposable
{
    private readonly string _testDirectory;
    private readonly FilePluginDiscovery _discovery;

    public AcceptanceCriteriaP4_05Tests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "p4-05-acceptance-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        _discovery = new FilePluginDiscovery();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Acceptance Criteria: Unit test loads two manifests (valid/invalid) and only registers the valid one.
    /// </summary>
    [Fact]
    public async Task AcceptanceCriteria_LoadTwoManifests_OnlyRegistersValidOne()
    {
        // Arrange - Create two manifest files: one valid, one invalid
        
        // Valid manifest with all required fields and capabilities
        var validManifestJson = """
        {
            "id": "analytics-plugin",
            "version": "1.2.3",
            "assemblies": ["AnalyticsPlugin.dll", "AnalyticsPlugin.Core.dll"],
            "capabilities": ["analytics", "resources"],
            "entryType": "AnalyticsPlugin.PluginEntry",
            "name": "Analytics Plugin",
            "description": "Provides analytics and resource management capabilities",
            "author": "Plugin Developer"
        }
        """;

        var validDir = Path.Combine(_testDirectory, "valid-plugin");
        Directory.CreateDirectory(validDir);
        await File.WriteAllTextAsync(Path.Combine(validDir, "plugin.json"), validManifestJson);

        // Invalid manifest missing required ID field
        var invalidManifestJson = """
        {
            "version": "2.0.0",
            "assemblies": ["BrokenPlugin.dll"],
            "capabilities": ["resources"],
            "entryType": "BrokenPlugin.PluginEntry"
        }
        """;

        var invalidDir = Path.Combine(_testDirectory, "invalid-plugin");
        Directory.CreateDirectory(invalidDir);
        await File.WriteAllTextAsync(Path.Combine(invalidDir, "plugin.json"), invalidManifestJson);

        // Act - Discover plugins from both directories
        var discoveredPlugins = await _discovery.DiscoverPluginsAsync(_testDirectory);

        // Debug: Let's see what we actually found
        var foundFiles = Directory.GetFiles(_testDirectory, "plugin.json", SearchOption.AllDirectories);
        Assert.Equal(2, foundFiles.Length); // Should find both manifest files
        
        // Assert - Only one plugin should be registered (the valid one)
        Assert.Single(discoveredPlugins, "Expected exactly one valid plugin to be discovered");
        
        var validPlugin = discoveredPlugins[0];
        
        // Verify the valid plugin has correct manifest data
        Assert.Equal("analytics-plugin", validPlugin.Id);
        Assert.Equal("1.2.3", validPlugin.Version);
        Assert.Equal("Analytics Plugin", validPlugin.Name);
        Assert.Equal("Provides analytics and resource management capabilities", validPlugin.Description);
        Assert.Equal("Plugin Developer", validPlugin.Author);
        
        // Verify assemblies are correctly loaded
        Assert.Equal(2, validPlugin.AssemblyPaths.Count);
        Assert.Contains(validPlugin.AssemblyPaths, path => path.EndsWith("AnalyticsPlugin.dll"));
        Assert.Contains(validPlugin.AssemblyPaths, path => path.EndsWith("AnalyticsPlugin.Core.dll"));
        
        // Verify capabilities are correctly parsed and validated
        Assert.NotNull(validPlugin.Capabilities);
        Assert.Equal("analytics-plugin", validPlugin.Capabilities.ProviderId);
        Assert.True(validPlugin.Capabilities.HasTags("analytics"));
        Assert.True(validPlugin.Capabilities.HasTags("resources"));
        Assert.True(validPlugin.Capabilities.HasTags("analytics", "resources"));
        
        // Verify entry type is stored in metadata
        Assert.Contains("entryType", validPlugin.Capabilities.Metadata.Keys);
        Assert.Equal("AnalyticsPlugin.PluginEntry", validPlugin.Capabilities.Metadata["entryType"]);
        
        // Verify manifest metadata is preserved
        Assert.NotNull(validPlugin.Manifest);
        Assert.Contains("originalManifest", validPlugin.Manifest.Keys);
        Assert.Contains("manifestPath", validPlugin.Manifest.Keys);
    }

    /// <summary>
    /// Tests version validation functionality with minimum version requirements.
    /// </summary>
    [Fact]
    public async Task AcceptanceCriteria_VersionValidation_FiltersLowVersionPlugins()
    {
        // Arrange - Create discovery with minimum version requirement
        var minimumVersion = new Version("1.5.0");
        var versionValidatingDiscovery = new FilePluginDiscovery(minimumVersion: minimumVersion);

        // Plugin below minimum version
        var lowVersionManifest = """
        {
            "id": "old-plugin",
            "version": "1.0.0",
            "assemblies": ["OldPlugin.dll"],
            "capabilities": ["analytics"]
        }
        """;

        var lowVersionDir = Path.Combine(_testDirectory, "old-version");
        Directory.CreateDirectory(lowVersionDir);
        await File.WriteAllTextAsync(Path.Combine(lowVersionDir, "plugin.json"), lowVersionManifest);

        // Plugin meeting minimum version
        var acceptableVersionManifest = """
        {
            "id": "new-plugin",
            "version": "2.0.0",
            "assemblies": ["NewPlugin.dll"],
            "capabilities": ["resources"]
        }
        """;

        var acceptableVersionDir = Path.Combine(_testDirectory, "new-version");
        Directory.CreateDirectory(acceptableVersionDir);
        await File.WriteAllTextAsync(Path.Combine(acceptableVersionDir, "plugin.json"), acceptableVersionManifest);

        // Act
        var validatedPlugins = await versionValidatingDiscovery.DiscoverPluginsAsync(_testDirectory);

        // Assert - Only plugin with acceptable version should be loaded
        Assert.Single(validatedPlugins);
        Assert.Equal("new-plugin", validatedPlugins[0].Id);
        Assert.Equal("2.0.0", validatedPlugins[0].Version);
    }

    /// <summary>
    /// Tests capability validation with required capabilities filtering.
    /// </summary>
    [Fact]
    public async Task AcceptanceCriteria_RequiredCapabilities_FiltersIncompatiblePlugins()
    {
        // Arrange - Create discovery that requires both "analytics" and "resources" capabilities
        var requiredCapabilities = new[] { "analytics", "resources" };
        var capabilityValidatingDiscovery = new FilePluginDiscovery(requiredCapabilities: requiredCapabilities);

        // Plugin with incomplete capabilities
        var incompleteManifest = """
        {
            "id": "incomplete-plugin",
            "version": "1.0.0",
            "assemblies": ["IncompletePlugin.dll"],
            "capabilities": ["analytics"]
        }
        """;

        var incompleteDir = Path.Combine(_testDirectory, "incomplete");
        Directory.CreateDirectory(incompleteDir);
        await File.WriteAllTextAsync(Path.Combine(incompleteDir, "plugin.json"), incompleteManifest);

        // Plugin with all required capabilities
        var completeManifest = """
        {
            "id": "complete-plugin",
            "version": "1.0.0",
            "assemblies": ["CompletePlugin.dll"],
            "capabilities": ["analytics", "resources", "extra-feature"]
        }
        """;

        var completeDir = Path.Combine(_testDirectory, "complete");
        Directory.CreateDirectory(completeDir);
        await File.WriteAllTextAsync(Path.Combine(completeDir, "plugin.json"), completeManifest);

        // Act
        var compatiblePlugins = await capabilityValidatingDiscovery.DiscoverPluginsAsync(_testDirectory);

        // Assert - Only plugin with all required capabilities should be loaded
        Assert.Single(compatiblePlugins);
        Assert.Equal("complete-plugin", compatiblePlugins[0].Id);
        Assert.True(compatiblePlugins[0].Capabilities!.HasTags("analytics", "resources"));
        Assert.True(compatiblePlugins[0].Capabilities!.HasTags("extra-feature")); // Extra capabilities are allowed
    }
}