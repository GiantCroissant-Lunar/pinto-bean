using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Acceptance criteria tests for P4-06: Version compatibility & type-identity guards.
/// Tests that plugins compiled against different contract versions are rejected with clear errors.
/// </summary>
public class AcceptanceCriteriaP4_06Tests : IDisposable
{
    private readonly string _testDirectory;

    public AcceptanceCriteriaP4_06Tests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"PintoBean_P4_06_Tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    /// <summary>
    /// Tests that a plugin compiled against a different contract version is rejected during discovery.
    /// </summary>
    [Fact]
    public async Task AcceptanceCriteria_PluginWithIncompatibleContractVersion_RejectedDuringDiscovery()
    {
        // Arrange - Create discovery with contract version validation enabled
        var discovery = new FilePluginDiscovery(validateContractVersion: true);

        // Plugin with incompatible contract version
        var incompatibleVersionManifest = @"{
    ""id"": ""incompatible-plugin"",
    ""version"": ""1.0.0"",
    ""assemblies"": [""IncompatiblePlugin.dll""],
    ""capabilities"": [""test""],
    ""contractVersion"": ""0.2.0-dev""
}";

        // Plugin with compatible contract version
        var compatibleVersionManifest = @"{
    ""id"": ""compatible-plugin"",
    ""version"": ""1.0.0"",
    ""assemblies"": [""CompatiblePlugin.dll""],
    ""capabilities"": [""test""],
    ""contractVersion"": ""0.1.0-dev""
}";

        var incompatibleDir = Path.Combine(_testDirectory, "incompatible");
        Directory.CreateDirectory(incompatibleDir);
        await File.WriteAllTextAsync(Path.Combine(incompatibleDir, "plugin.json"), incompatibleVersionManifest);

        var compatibleDir = Path.Combine(_testDirectory, "compatible");
        Directory.CreateDirectory(compatibleDir);
        await File.WriteAllTextAsync(Path.Combine(compatibleDir, "plugin.json"), compatibleVersionManifest);

        // Act
        var discoveredPlugins = await discovery.DiscoverPluginsAsync(_testDirectory);

        // Assert - Only the compatible plugin should be discovered
        Assert.Single(discoveredPlugins);
        Assert.Equal("compatible-plugin", discoveredPlugins[0].Id);
        Assert.Equal("0.1.0-dev", discoveredPlugins[0].ContractVersion);
    }

    /// <summary>
    /// Tests that a plugin without a declared contract version is rejected during discovery.
    /// </summary>
    [Fact]
    public async Task AcceptanceCriteria_PluginWithoutContractVersion_RejectedDuringDiscovery()
    {
        // Arrange - Create discovery with contract version validation enabled
        var discovery = new FilePluginDiscovery(validateContractVersion: true);

        // Plugin without contract version declaration
        var noVersionManifest = @"{
    ""id"": ""no-version-plugin"",
    ""version"": ""1.0.0"",
    ""assemblies"": [""NoVersionPlugin.dll""],
    ""capabilities"": [""test""]
}";

        var pluginDir = Path.Combine(_testDirectory, "no-version");
        Directory.CreateDirectory(pluginDir);
        await File.WriteAllTextAsync(Path.Combine(pluginDir, "plugin.json"), noVersionManifest);

        // Act
        var discoveredPlugins = await discovery.DiscoverPluginsAsync(_testDirectory);

        // Assert - No plugins should be discovered due to missing contract version
        Assert.Empty(discoveredPlugins);
    }

    /// <summary>
    /// Tests that contract version validation can be disabled.
    /// </summary>
    [Fact]
    public async Task AcceptanceCriteria_ContractVersionValidationDisabled_AllowsIncompatibleVersions()
    {
        // Arrange - Create discovery with contract version validation disabled
        var discovery = new FilePluginDiscovery(validateContractVersion: false);

        // Plugin with incompatible contract version
        var incompatibleVersionManifest = @"{
    ""id"": ""incompatible-plugin"",
    ""version"": ""1.0.0"",
    ""assemblies"": [""IncompatiblePlugin.dll""],
    ""capabilities"": [""test""],
    ""contractVersion"": ""0.2.0-dev""
}";

        var pluginDir = Path.Combine(_testDirectory, "disabled-validation");
        Directory.CreateDirectory(pluginDir);
        await File.WriteAllTextAsync(Path.Combine(pluginDir, "plugin.json"), incompatibleVersionManifest);

        // Act
        var discoveredPlugins = await discovery.DiscoverPluginsAsync(_testDirectory);

        // Assert - Plugin should be discovered when validation is disabled
        Assert.Single(discoveredPlugins);
        Assert.Equal("incompatible-plugin", discoveredPlugins[0].Id);
    }

    /// <summary>
    /// Tests that plugin activation fails with clear error message for incompatible contract version.
    /// </summary>
    [Fact]
    public async Task AcceptanceCriteria_PluginActivationWithIncompatibleVersion_FailsWithClearError()
    {
        // Arrange - Create a plugin descriptor with incompatible contract version
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "TestPlugin.dll")
        {
            ContractVersion = "0.2.0-dev"
        };

        using var pluginHost = new PluginHost();
        
        // Create a mock load context for testing
        var mockLoadContext = new FakeLoadContext("test-context");
        var handle = new PluginHandle("test-plugin", mockLoadContext, descriptor);
        
        // Manually add to the plugin host's internal collection for testing
        // This simulates the plugin being loaded but not yet activated
        var pluginsField = typeof(PluginHost).GetField("_plugins", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var plugins = (System.Collections.Concurrent.ConcurrentDictionary<string, PluginHandle>)pluginsField!.GetValue(pluginHost)!;
        plugins.TryAdd("test-plugin", handle);

        bool activationFailed = false;
        string? errorMessage = null;
        
        pluginHost.PluginFailed += (sender, args) =>
        {
            activationFailed = true;
            errorMessage = args.Exception.Message;
        };

        // Act
        var result = await pluginHost.ActivateAsync("test-plugin");

        // Assert
        Assert.False(result);
        Assert.True(activationFailed);
        Assert.NotNull(errorMessage);
        Assert.Contains("incompatible", errorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("0.2.0-dev", errorMessage);
        Assert.Contains("0.1.0-dev", errorMessage);
        Assert.Equal(PluginState.Failed, handle.State);
    }

    /// <summary>
    /// Tests that plugin activation succeeds for compatible contract version.
    /// </summary>
    [Fact]
    public async Task AcceptanceCriteria_PluginActivationWithCompatibleVersion_Succeeds()
    {
        // Arrange - Create a plugin descriptor with compatible contract version
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "TestPlugin.dll")
        {
            ContractVersion = "0.1.0-dev"
        };

        using var pluginHost = new PluginHost();
        
        // Create a mock load context for testing
        var mockLoadContext = new FakeLoadContext("test-context");
        var handle = new PluginHandle("test-plugin", mockLoadContext, descriptor);
        
        // Manually add to the plugin host's internal collection for testing
        var pluginsField = typeof(PluginHost).GetField("_plugins", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var plugins = (System.Collections.Concurrent.ConcurrentDictionary<string, PluginHandle>)pluginsField!.GetValue(pluginHost)!;
        plugins.TryAdd("test-plugin", handle);

        // Act
        var result = await pluginHost.ActivateAsync("test-plugin");

        // Assert
        Assert.True(result);
        Assert.Equal(PluginState.Active, handle.State);
        Assert.NotNull(handle.ActivatedAt);
    }

    /// <summary>
    /// Tests contract version compatibility validation logic.
    /// </summary>
    [Theory]
    [InlineData("0.1.0-dev", true)]  // Exact match should be compatible
    [InlineData("0.2.0-dev", false)] // Different version should be incompatible
    [InlineData("1.0.0", false)]     // Different version should be incompatible
    [InlineData(null, false)]        // Null version should be incompatible
    [InlineData("", false)]          // Empty version should be incompatible
    [InlineData("   ", false)]       // Whitespace version should be incompatible
    public void AcceptanceCriteria_ContractVersionCompatibility_ValidatesAsExpected(string? contractVersion, bool expectedCompatible)
    {
        // Act
        var isCompatible = ContractVersioning.IsCompatible(contractVersion);

        // Assert
        Assert.Equal(expectedCompatible, isCompatible);
    }

    /// <summary>
    /// Tests that error messages for incompatible versions are clear and informative.
    /// </summary>
    [Fact]
    public void AcceptanceCriteria_IncompatibleVersionErrorMessage_IsClearAndInformative()
    {
        // Act
        var errorMessage = ContractVersioning.GetCompatibilityErrorMessage("test-plugin", "0.2.0-dev");

        // Assert
        Assert.Contains("test-plugin", errorMessage);
        Assert.Contains("0.2.0-dev", errorMessage);
        Assert.Contains("0.1.0-dev", errorMessage);
        Assert.Contains("incompatible", errorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rebuild", errorMessage, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that error messages for missing contract versions are clear and informative.
    /// </summary>
    [Fact]
    public void AcceptanceCriteria_MissingVersionErrorMessage_IsClearAndInformative()
    {
        // Act
        var errorMessage = ContractVersioning.GetCompatibilityErrorMessage("test-plugin", null);

        // Assert
        Assert.Contains("test-plugin", errorMessage);
        Assert.Contains("does not declare", errorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("0.1.0-dev", errorMessage);
        Assert.Contains("rebuild", errorMessage, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}