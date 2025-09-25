using System;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for PluginHandle class.
/// </summary>
public class PluginHandleTests
{
    [Fact]
    public void PluginHandle_Constructor_WithValidArguments_InitializesCorrectly()
    {
        // Arrange
        const string id = "test-plugin";
        using var loadContext = new FakeLoadContext();
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/path/to/plugin.dll");

        // Act
        var handle = new PluginHandle(id, loadContext, descriptor);

        // Assert
        Assert.Equal(id, handle.Id);
        Assert.Equal(loadContext, handle.LoadContext);
        Assert.Equal(descriptor, handle.Descriptor);
        Assert.Equal(PluginState.Loaded, handle.State);
        Assert.True(handle.LoadedAt <= DateTimeOffset.UtcNow);
        Assert.True(handle.LoadedAt >= DateTimeOffset.UtcNow.AddSeconds(-1));
        Assert.Null(handle.ActivatedAt);
        Assert.Null(handle.DeactivatedAt);
        Assert.Null(handle.Instance);
        Assert.Null(handle.LastError);
    }

    [Fact]
    public void PluginHandle_Constructor_WithNullId_ThrowsArgumentException()
    {
        // Arrange
        using var loadContext = new FakeLoadContext();
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/path/to/plugin.dll");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PluginHandle(null!, loadContext, descriptor));
        Assert.Throws<ArgumentException>(() => new PluginHandle("", loadContext, descriptor));
        Assert.Throws<ArgumentException>(() => new PluginHandle("   ", loadContext, descriptor));
    }

    [Fact]
    public void PluginHandle_Constructor_WithNullLoadContext_ThrowsArgumentNullException()
    {
        // Arrange
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/path/to/plugin.dll");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PluginHandle("test-plugin", null!, descriptor));
    }

    [Fact]
    public void PluginHandle_Constructor_WithNullDescriptor_ThrowsArgumentNullException()
    {
        // Arrange
        using var loadContext = new FakeLoadContext();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PluginHandle("test-plugin", loadContext, null!));
    }

    [Fact]
    public void PluginHandle_StateTransitions_ValidateReadOnlyProperties()
    {
        // Arrange
        using var loadContext = new FakeLoadContext();
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/path/to/plugin.dll");
        var handle = new PluginHandle("test-plugin", loadContext, descriptor);

        // Assert initial state - properties are read-only for external access
        Assert.Equal(PluginState.Loaded, handle.State);
        Assert.True(handle.LoadedAt <= DateTimeOffset.UtcNow);
        Assert.Null(handle.ActivatedAt);
        Assert.Null(handle.DeactivatedAt);
        Assert.Null(handle.Instance);
        Assert.Null(handle.LastError);
    }

    [Fact]
    public void PluginHandle_Instance_IsReadOnlyProperty()
    {
        // Arrange
        using var loadContext = new FakeLoadContext();
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/path/to/plugin.dll");
        var handle = new PluginHandle("test-plugin", loadContext, descriptor);

        // Assert - Instance property is initially null and read-only externally
        Assert.Null(handle.Instance);
    }

    [Fact]
    public void PluginHandle_ToString_ReturnsExpectedFormat()
    {
        // Arrange
        using var loadContext = new FakeLoadContext();
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/path/to/plugin.dll");
        var handle = new PluginHandle("test-plugin", loadContext, descriptor);

        // Act
        var result = handle.ToString();

        // Assert
        Assert.Equal("test-plugin (Loaded)", result);

        // Test with different state (would be updated internally by PluginHost)
        // This test shows the read-only nature - we can't modify state externally
        Assert.Equal("test-plugin (Loaded)", handle.ToString());
    }

    [Fact]
    public void PluginHandle_Equals_WithSameId_ReturnsTrue()
    {
        // Arrange
        using var loadContext1 = new FakeLoadContext();
        using var loadContext2 = new FakeLoadContext();
        var descriptor1 = new PluginDescriptor("test-plugin", "1.0.0", "/path/to/plugin1.dll");
        var descriptor2 = new PluginDescriptor("test-plugin", "2.0.0", "/path/to/plugin2.dll");
        
        var handle1 = new PluginHandle("test-plugin", loadContext1, descriptor1);
        var handle2 = new PluginHandle("test-plugin", loadContext2, descriptor2);

        // Act & Assert
        Assert.True(handle1.Equals(handle2));
        Assert.True(handle1.Equals((object)handle2));
        Assert.Equal(handle1.GetHashCode(), handle2.GetHashCode());
    }

    [Fact]
    public void PluginHandle_Equals_WithDifferentId_ReturnsFalse()
    {
        // Arrange
        using var loadContext = new FakeLoadContext();
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/path/to/plugin.dll");
        
        var handle1 = new PluginHandle("test-plugin-1", loadContext, descriptor);
        var handle2 = new PluginHandle("test-plugin-2", loadContext, descriptor);

        // Act & Assert
        Assert.False(handle1.Equals(handle2));
        Assert.False(handle1.Equals((object)handle2));
    }

    [Fact]
    public void PluginHandle_Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        using var loadContext = new FakeLoadContext();
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/path/to/plugin.dll");
        var handle = new PluginHandle("test-plugin", loadContext, descriptor);

        // Act & Assert
        Assert.False(handle.Equals(null));
        Assert.False(handle.Equals((object?)null));
    }

    [Fact]
    public void PluginHandle_Equals_WithDifferentType_ReturnsFalse()
    {
        // Arrange
        using var loadContext = new FakeLoadContext();
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/path/to/plugin.dll");
        var handle = new PluginHandle("test-plugin", loadContext, descriptor);
        var other = "not a plugin handle";

        // Act & Assert
        Assert.False(handle.Equals(other));
    }

    [Theory]
    [InlineData(PluginState.Loaded)]
    [InlineData(PluginState.Active)]
    [InlineData(PluginState.Deactivated)]
    [InlineData(PluginState.Unloaded)]
    [InlineData(PluginState.Failed)]
    public void PluginState_AllValues_AreValid(PluginState state)
    {
        // This test ensures all enum values are properly defined
        Assert.True(Enum.IsDefined(typeof(PluginState), state));
    }
}