using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for PluginDescriptor class.
/// </summary>
public class PluginDescriptorTests
{
    [Fact]
    public void PluginDescriptor_Constructor_WithValidArguments_InitializesCorrectly()
    {
        // Arrange
        const string id = "test-plugin";
        const string version = "1.0.0";
        var assemblyPaths = new[] { "/path/to/plugin.dll", "/path/to/dependency.dll" };

        // Act
        var descriptor = new PluginDescriptor(id, version, assemblyPaths);

        // Assert
        Assert.Equal(id, descriptor.Id);
        Assert.Equal(version, descriptor.Version);
        Assert.Equal(assemblyPaths, descriptor.AssemblyPaths);
        Assert.Null(descriptor.Manifest);
        Assert.Null(descriptor.Capabilities);
        Assert.Null(descriptor.Name);
        Assert.Null(descriptor.Description);
        Assert.Null(descriptor.Author);
    }

    [Fact]
    public void PluginDescriptor_Constructor_WithSingleAssembly_InitializesCorrectly()
    {
        // Arrange
        const string id = "test-plugin";
        const string version = "1.0.0";
        const string assemblyPath = "/path/to/plugin.dll";

        // Act
        var descriptor = new PluginDescriptor(id, version, assemblyPath);

        // Assert
        Assert.Equal(id, descriptor.Id);
        Assert.Equal(version, descriptor.Version);
        Assert.Single(descriptor.AssemblyPaths);
        Assert.Equal(assemblyPath, descriptor.AssemblyPaths.First());
    }

    [Fact]
    public void PluginDescriptor_Constructor_WithNullId_ThrowsArgumentException()
    {
        // Arrange
        const string version = "1.0.0";
        const string assemblyPath = "/path/to/plugin.dll";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PluginDescriptor(null!, version, assemblyPath));
        Assert.Throws<ArgumentException>(() => new PluginDescriptor("", version, assemblyPath));
        Assert.Throws<ArgumentException>(() => new PluginDescriptor("   ", version, assemblyPath));
    }

    [Fact]
    public void PluginDescriptor_Constructor_WithNullVersion_ThrowsArgumentException()
    {
        // Arrange
        const string id = "test-plugin";
        const string assemblyPath = "/path/to/plugin.dll";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PluginDescriptor(id, null!, assemblyPath));
        Assert.Throws<ArgumentException>(() => new PluginDescriptor(id, "", assemblyPath));
        Assert.Throws<ArgumentException>(() => new PluginDescriptor(id, "   ", assemblyPath));
    }

    [Fact]
    public void PluginDescriptor_Constructor_WithNullAssemblyPaths_ThrowsArgumentNullException()
    {
        // Arrange
        const string id = "test-plugin";
        const string version = "1.0.0";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PluginDescriptor(id, version, (IEnumerable<string>)null!));
    }

    [Fact]
    public void PluginDescriptor_Constructor_WithEmptyAssemblyPaths_ThrowsArgumentException()
    {
        // Arrange
        const string id = "test-plugin";
        const string version = "1.0.0";
        var emptyPaths = Array.Empty<string>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new PluginDescriptor(id, version, emptyPaths));
        Assert.Contains("At least one assembly path must be provided", exception.Message);
    }

    [Fact]
    public void PluginDescriptor_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/path/to/plugin.dll");
        var manifest = new Dictionary<string, object> { { "key", "value" } };
        var capabilities = new ProviderCapabilities { ProviderId = "test-provider" };

        // Act
        descriptor.Manifest = manifest;
        descriptor.Capabilities = capabilities;
        descriptor.Name = "Test Plugin";
        descriptor.Description = "A test plugin";
        descriptor.Author = "Test Author";

        // Assert
        Assert.Equal(manifest, descriptor.Manifest);
        Assert.Equal(capabilities, descriptor.Capabilities);
        Assert.Equal("Test Plugin", descriptor.Name);
        Assert.Equal("A test plugin", descriptor.Description);
        Assert.Equal("Test Author", descriptor.Author);
    }

    [Fact]
    public void PluginDescriptor_ToString_ReturnsExpectedFormat()
    {
        // Arrange
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/path/to/plugin.dll");

        // Act
        var result = descriptor.ToString();

        // Assert
        Assert.Equal("test-plugin v1.0.0", result);
    }

    [Fact]
    public void PluginDescriptor_Equals_WithSameIdAndVersion_ReturnsTrue()
    {
        // Arrange
        var descriptor1 = new PluginDescriptor("test-plugin", "1.0.0", "/path/to/plugin1.dll");
        var descriptor2 = new PluginDescriptor("test-plugin", "1.0.0", "/path/to/plugin2.dll");

        // Act & Assert
        Assert.True(descriptor1.Equals(descriptor2));
        Assert.True(descriptor1.Equals((object)descriptor2));
        Assert.Equal(descriptor1.GetHashCode(), descriptor2.GetHashCode());
    }

    [Fact]
    public void PluginDescriptor_Equals_WithDifferentId_ReturnsFalse()
    {
        // Arrange
        var descriptor1 = new PluginDescriptor("test-plugin-1", "1.0.0", "/path/to/plugin.dll");
        var descriptor2 = new PluginDescriptor("test-plugin-2", "1.0.0", "/path/to/plugin.dll");

        // Act & Assert
        Assert.False(descriptor1.Equals(descriptor2));
        Assert.False(descriptor1.Equals((object)descriptor2));
    }

    [Fact]
    public void PluginDescriptor_Equals_WithDifferentVersion_ReturnsFalse()
    {
        // Arrange
        var descriptor1 = new PluginDescriptor("test-plugin", "1.0.0", "/path/to/plugin.dll");
        var descriptor2 = new PluginDescriptor("test-plugin", "2.0.0", "/path/to/plugin.dll");

        // Act & Assert
        Assert.False(descriptor1.Equals(descriptor2));
        Assert.False(descriptor1.Equals((object)descriptor2));
    }

    [Fact]
    public void PluginDescriptor_Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/path/to/plugin.dll");

        // Act & Assert
        Assert.False(descriptor.Equals(null));
        Assert.False(descriptor.Equals((object?)null));
    }

    [Fact]
    public void PluginDescriptor_Equals_WithDifferentType_ReturnsFalse()
    {
        // Arrange
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/path/to/plugin.dll");
        var other = "not a plugin descriptor";

        // Act & Assert
        Assert.False(descriptor.Equals(other));
    }
}