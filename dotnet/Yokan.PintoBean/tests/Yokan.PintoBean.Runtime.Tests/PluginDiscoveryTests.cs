using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for plugin discovery and manifest validation functionality.
/// </summary>
public class PluginDiscoveryTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly FilePluginDiscovery _discovery;

    public PluginDiscoveryTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "plugin-discovery-tests", Guid.NewGuid().ToString());
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

    [Fact]
    public async Task DiscoverPluginsAsync_WithValidManifest_ReturnsPluginDescriptor()
    {
        // Arrange
        var validManifestJson = """
        {
            "id": "test-plugin",
            "version": "1.0.0",
            "assemblies": ["TestPlugin.dll"],
            "capabilities": ["analytics", "resources"],
            "entryType": "TestPlugin.PluginEntry",
            "name": "Test Plugin",
            "description": "A test plugin",
            "author": "Test Author"
        }
        """;

        var manifestPath = Path.Combine(_testDirectory, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, validManifestJson);

        // Act
        var descriptors = await _discovery.DiscoverPluginsAsync(_testDirectory);

        // Assert
        Assert.Single(descriptors);
        var descriptor = descriptors[0];
        Assert.Equal("test-plugin", descriptor.Id);
        Assert.Equal("1.0.0", descriptor.Version);
        Assert.Equal("Test Plugin", descriptor.Name);
        Assert.Equal("A test plugin", descriptor.Description);
        Assert.Equal("Test Author", descriptor.Author);
        Assert.Single(descriptor.AssemblyPaths);
        Assert.EndsWith("TestPlugin.dll", descriptor.AssemblyPaths[0]);
        
        Assert.NotNull(descriptor.Capabilities);
        Assert.Equal("test-plugin", descriptor.Capabilities.ProviderId);
        Assert.True(descriptor.Capabilities.HasTags("analytics", "resources"));
        Assert.Equal("TestPlugin.PluginEntry", descriptor.Capabilities.Metadata["entryType"]);
        
        Assert.NotNull(descriptor.Manifest);
        Assert.Contains("originalManifest", descriptor.Manifest.Keys);
        Assert.Contains("manifestPath", descriptor.Manifest.Keys);
    }

    [Fact]
    public async Task DiscoverPluginsAsync_WithInvalidManifest_ReturnsEmpty()
    {
        // Arrange - invalid manifest missing required ID
        var invalidManifestJson = """
        {
            "version": "1.0.0",
            "assemblies": ["TestPlugin.dll"],
            "capabilities": ["analytics"]
        }
        """;

        var manifestPath = Path.Combine(_testDirectory, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, invalidManifestJson);

        // Act
        var descriptors = await _discovery.DiscoverPluginsAsync(_testDirectory);

        // Assert
        Assert.Empty(descriptors);
    }

    [Fact]
    public async Task DiscoverPluginsAsync_WithValidAndInvalidManifests_ReturnsOnlyValid()
    {
        // Arrange - Create subdirectories with valid and invalid manifests
        var validDir = Path.Combine(_testDirectory, "valid");
        var invalidDir = Path.Combine(_testDirectory, "invalid");
        Directory.CreateDirectory(validDir);
        Directory.CreateDirectory(invalidDir);

        // Valid manifest
        var validManifestJson = """
        {
            "id": "valid-plugin",
            "version": "2.0.0",
            "assemblies": ["ValidPlugin.dll", "ValidPlugin.Helpers.dll"],
            "capabilities": ["analytics"]
        }
        """;
        await File.WriteAllTextAsync(Path.Combine(validDir, "plugin.json"), validManifestJson);

        // Invalid manifest (missing assemblies)
        var invalidManifestJson = """
        {
            "id": "invalid-plugin",
            "version": "1.0.0",
            "assemblies": [],
            "capabilities": ["resources"]
        }
        """;
        await File.WriteAllTextAsync(Path.Combine(invalidDir, "plugin.json"), invalidManifestJson);

        // Act
        var descriptors = await _discovery.DiscoverPluginsAsync(_testDirectory);

        // Assert - Only the valid plugin should be returned
        Assert.Single(descriptors);
        var descriptor = descriptors[0];
        Assert.Equal("valid-plugin", descriptor.Id);
        Assert.Equal("2.0.0", descriptor.Version);
        Assert.Equal(2, descriptor.AssemblyPaths.Count);
        Assert.True(descriptor.Capabilities!.HasTags("analytics"));
    }

    [Fact]
    public async Task DiscoverPluginsAsync_WithMinimumVersionValidation_FiltersCorrectly()
    {
        // Arrange
        var minimumVersion = new Version("1.5.0");
        var discoveryWithVersionCheck = new FilePluginDiscovery(minimumVersion: minimumVersion);

        // Plugin with version below minimum
        var oldPluginJson = """
        {
            "id": "old-plugin",
            "version": "1.0.0",
            "assemblies": ["OldPlugin.dll"],
            "capabilities": ["analytics"]
        }
        """;
        var oldDir = Path.Combine(_testDirectory, "old");
        Directory.CreateDirectory(oldDir);
        await File.WriteAllTextAsync(Path.Combine(oldDir, "plugin.json"), oldPluginJson);

        // Plugin with version meeting minimum
        var newPluginJson = """
        {
            "id": "new-plugin",
            "version": "2.0.0",
            "assemblies": ["NewPlugin.dll"],
            "capabilities": ["resources"]
        }
        """;
        var newDir = Path.Combine(_testDirectory, "new");
        Directory.CreateDirectory(newDir);
        await File.WriteAllTextAsync(Path.Combine(newDir, "plugin.json"), newPluginJson);

        // Act
        var descriptors = await discoveryWithVersionCheck.DiscoverPluginsAsync(_testDirectory);

        // Assert - Only the newer plugin should be returned
        Assert.Single(descriptors);
        Assert.Equal("new-plugin", descriptors[0].Id);
    }

    [Fact]
    public async Task DiscoverPluginsAsync_WithRequiredCapabilities_FiltersCorrectly()
    {
        // Arrange
        var requiredCapabilities = new[] { "analytics", "resources" };
        var discoveryWithCapabilityCheck = new FilePluginDiscovery(requiredCapabilities: requiredCapabilities);

        // Plugin missing required capabilities
        var incompletePluginJson = """
        {
            "id": "incomplete-plugin",
            "version": "1.0.0",
            "assemblies": ["IncompletePlugin.dll"],
            "capabilities": ["analytics"]
        }
        """;
        var incompleteDir = Path.Combine(_testDirectory, "incomplete");
        Directory.CreateDirectory(incompleteDir);
        await File.WriteAllTextAsync(Path.Combine(incompleteDir, "plugin.json"), incompletePluginJson);

        // Plugin with all required capabilities
        var completePluginJson = """
        {
            "id": "complete-plugin",
            "version": "1.0.0",
            "assemblies": ["CompletePlugin.dll"],
            "capabilities": ["analytics", "resources", "extra"]
        }
        """;
        var completeDir = Path.Combine(_testDirectory, "complete");
        Directory.CreateDirectory(completeDir);
        await File.WriteAllTextAsync(Path.Combine(completeDir, "plugin.json"), completePluginJson);

        // Act
        var descriptors = await discoveryWithCapabilityCheck.DiscoverPluginsAsync(_testDirectory);

        // Assert - Only the complete plugin should be returned
        Assert.Single(descriptors);
        Assert.Equal("complete-plugin", descriptors[0].Id);
    }

    [Fact]
    public async Task DiscoverPluginsAsync_WithNonExistentDirectory_ReturnsEmpty()
    {
        // Arrange
        var nonExistentDir = Path.Combine(_testDirectory, "does-not-exist");

        // Act
        var descriptors = await _discovery.DiscoverPluginsAsync(nonExistentDir);

        // Assert
        Assert.Empty(descriptors);
    }

    [Fact]
    public async Task DiscoverPluginsAsync_WithNullDirectory_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _discovery.DiscoverPluginsAsync(null!));
    }

    [Fact]
    public async Task DiscoverPluginsAsync_WithEmptyDirectory_ReturnsEmpty()
    {
        // Arrange - empty directory with no manifest files
        var emptyDir = Path.Combine(_testDirectory, "empty");
        Directory.CreateDirectory(emptyDir);

        // Act
        var descriptors = await _discovery.DiscoverPluginsAsync(emptyDir);

        // Assert
        Assert.Empty(descriptors);
    }

    [Fact]
    public async Task DiscoverPluginsAsync_WithMalformedJson_ContinuesDiscovery()
    {
        // Arrange
        // Create a valid manifest
        var validManifestJson = """
        {
            "id": "valid-plugin",
            "version": "1.0.0",
            "assemblies": ["ValidPlugin.dll"],
            "capabilities": ["analytics"]
        }
        """;
        var validDir = Path.Combine(_testDirectory, "valid");
        Directory.CreateDirectory(validDir);
        await File.WriteAllTextAsync(Path.Combine(validDir, "plugin.json"), validManifestJson);

        // Create a malformed JSON file
        var malformedDir = Path.Combine(_testDirectory, "malformed");
        Directory.CreateDirectory(malformedDir);
        await File.WriteAllTextAsync(Path.Combine(malformedDir, "plugin.json"), "{ invalid json }");

        // Act
        var descriptors = await _discovery.DiscoverPluginsAsync(_testDirectory);

        // Assert - Should continue and find the valid plugin despite the malformed one
        Assert.Single(descriptors);
        Assert.Equal("valid-plugin", descriptors[0].Id);
    }

    [Fact]
    public async Task DiscoverPluginsAsync_WithCustomManifestFileName_FindsCorrectFiles()
    {
        // Arrange
        var customDiscovery = new FilePluginDiscovery("custom-manifest.json");
        
        var manifestJson = """
        {
            "id": "custom-plugin",
            "version": "1.0.0",
            "assemblies": ["CustomPlugin.dll"],
            "capabilities": ["analytics"]
        }
        """;

        // Create file with custom name
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "custom-manifest.json"), manifestJson);
        
        // Also create a file with default name that shouldn't be found
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "plugin.json"), manifestJson);

        // Act
        var descriptors = await customDiscovery.DiscoverPluginsAsync(_testDirectory);

        // Assert - Should only find the custom-named manifest
        Assert.Single(descriptors);
        Assert.Equal("custom-plugin", descriptors[0].Id);
    }

    [Fact]
    public async Task DiscoverPluginsAsync_WithRelativeAssemblyPaths_ConvertsToAbsolute()
    {
        // Arrange
        var manifestJson = """
        {
            "id": "relative-path-plugin",
            "version": "1.0.0",
            "assemblies": ["bin/RelativePlugin.dll", "../shared/Shared.dll"],
            "capabilities": ["analytics"]
        }
        """;

        var subDir = Path.Combine(_testDirectory, "plugin-dir");
        Directory.CreateDirectory(subDir);
        var manifestPath = Path.Combine(subDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, manifestJson);

        // Act
        var descriptors = await _discovery.DiscoverPluginsAsync(_testDirectory);

        // Assert
        Assert.Single(descriptors);
        var descriptor = descriptors[0];
        Assert.Equal(2, descriptor.AssemblyPaths.Count);
        
        // Paths should be absolute
        Assert.True(Path.IsPathRooted(descriptor.AssemblyPaths[0]));
        Assert.True(Path.IsPathRooted(descriptor.AssemblyPaths[1]));
        
        // Check that relative paths were resolved correctly
        Assert.EndsWith(Path.Combine("plugin-dir", "bin", "RelativePlugin.dll"), descriptor.AssemblyPaths[0]);
        Assert.EndsWith(Path.Combine("shared", "Shared.dll"), descriptor.AssemblyPaths[1]);
    }

    [Fact]
    public void PluginManifest_IsValid_ValidatesCorrectly()
    {
        // Valid manifest
        var validManifest = new PluginManifest
        {
            Id = "test",
            Version = "1.0.0",
            Assemblies = new[] { "test.dll" }
        };
        Assert.True(validManifest.IsValid());

        // Invalid - missing ID
        var noId = new PluginManifest
        {
            Version = "1.0.0",
            Assemblies = new[] { "test.dll" }
        };
        Assert.False(noId.IsValid());

        // Invalid - missing version
        var noVersion = new PluginManifest
        {
            Id = "test",
            Assemblies = new[] { "test.dll" }
        };
        Assert.False(noVersion.IsValid());

        // Invalid - no assemblies
        var noAssemblies = new PluginManifest
        {
            Id = "test",
            Version = "1.0.0",
            Assemblies = Array.Empty<string>()
        };
        Assert.False(noAssemblies.IsValid());

        // Invalid - empty assembly path
        var emptyAssemblyPath = new PluginManifest
        {
            Id = "test",
            Version = "1.0.0",
            Assemblies = new[] { "" }
        };
        Assert.False(emptyAssemblyPath.IsValid());
    }
}