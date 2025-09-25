using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for IPluginHost implementation.
/// </summary>
public class PluginHostTests
{
    /// <summary>
    /// Creates a PluginHost with a pre-configured FakeLoadContext factory for testing.
    /// </summary>
    private static PluginHost CreateTestPluginHost()
    {
        return new PluginHost(descriptor =>
        {
            var context = new FakeLoadContext(descriptor.Id);
            // Pre-register fake assemblies for all paths in the descriptor
            foreach (var path in descriptor.AssemblyPaths)
            {
                context.RegisterAssembly(path, typeof(PluginHostTests).Assembly);
            }
            return context;
        });
    }
    [Fact]
    public void PluginHost_Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        using var host = new PluginHost();

        // Assert
        Assert.NotNull(host.LoadedPlugins);
        Assert.Empty(host.LoadedPlugins);
    }

    [Fact]
    public async Task LoadPluginAsync_WithValidDescriptor_LoadsSuccessfully()
    {
        // Arrange
        using var host = CreateTestPluginHost();
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/fake/path.dll");
        PluginLoadedEventArgs? eventArgs = null;
        host.PluginLoaded += (sender, args) => eventArgs = args;

        // Act
        var handle = await host.LoadPluginAsync(descriptor);

        // Assert
        Assert.NotNull(handle);
        Assert.Equal("test-plugin", handle.Id);
        Assert.Equal(PluginState.Loaded, handle.State);
        Assert.Single(host.LoadedPlugins);
        Assert.True(host.IsLoaded("test-plugin"));
        Assert.NotNull(eventArgs);
        Assert.Equal(handle, eventArgs.Handle);
    }

    [Fact]
    public async Task LoadPluginAsync_WithNullDescriptor_ThrowsArgumentNullException()
    {
        // Arrange
        using var host = new PluginHost();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => host.LoadPluginAsync(null!));
    }

    [Fact]
    public async Task LoadPluginAsync_WithDuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        using var host = CreateTestPluginHost();
        var descriptor1 = new PluginDescriptor("test-plugin", "1.0.0", "/fake/path1.dll");
        var descriptor2 = new PluginDescriptor("test-plugin", "2.0.0", "/fake/path2.dll");

        // Act
        await host.LoadPluginAsync(descriptor1);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => host.LoadPluginAsync(descriptor2));
    }

    [Fact]
    public async Task ActivateAsync_WithLoadedPlugin_ActivatesSuccessfully()
    {
        // Arrange
        using var host = CreateTestPluginHost();
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/fake/path.dll");
        var handle = await host.LoadPluginAsync(descriptor);

        // Act
        var result = await host.ActivateAsync("test-plugin");

        // Assert
        Assert.True(result);
        Assert.Equal(PluginState.Active, handle.State);
        Assert.NotNull(handle.ActivatedAt);
    }

    [Fact]
    public async Task ActivateAsync_WithActivePlugin_ReturnsTrue()
    {
        // Arrange
        using var host = CreateTestPluginHost();
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/fake/path.dll");
        await host.LoadPluginAsync(descriptor);
        await host.ActivateAsync("test-plugin");

        // Act
        var result = await host.ActivateAsync("test-plugin");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ActivateAsync_WithNonExistentPlugin_ReturnsFalse()
    {
        // Arrange
        using var host = new PluginHost();

        // Act
        var result = await host.ActivateAsync("non-existent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeactivateAsync_WithActivePlugin_DeactivatesSuccessfully()
    {
        // Arrange
        using var host = CreateTestPluginHost();
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/fake/path.dll");
        var handle = await host.LoadPluginAsync(descriptor);
        await host.ActivateAsync("test-plugin");

        // Act
        var result = await host.DeactivateAsync("test-plugin");

        // Assert
        Assert.True(result);
        Assert.Equal(PluginState.Deactivated, handle.State);
        Assert.NotNull(handle.DeactivatedAt);
    }

    [Fact]
    public async Task DeactivateAsync_WithDeactivatedPlugin_ReturnsTrue()
    {
        // Arrange
        using var host = CreateTestPluginHost();
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/fake/path.dll");
        await host.LoadPluginAsync(descriptor);
        await host.ActivateAsync("test-plugin");
        await host.DeactivateAsync("test-plugin");

        // Act
        var result = await host.DeactivateAsync("test-plugin");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeactivateAsync_WithLoadedPlugin_ReturnsFalse()
    {
        // Arrange
        using var host = CreateTestPluginHost();
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/fake/path.dll");
        await host.LoadPluginAsync(descriptor);

        // Act (try to deactivate without activating first)
        var result = await host.DeactivateAsync("test-plugin");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UnloadAsync_WithLoadedPlugin_UnloadsSuccessfully()
    {
        // Arrange
        using var host = CreateTestPluginHost();
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/fake/path.dll");
        var handle = await host.LoadPluginAsync(descriptor);
        PluginUnloadedEventArgs? eventArgs = null;
        host.PluginUnloaded += (sender, args) => eventArgs = args;

        // Act
        var result = await host.UnloadAsync("test-plugin");

        // Assert
        Assert.True(result);
        Assert.Equal(PluginState.Unloaded, handle.State);
        Assert.Empty(host.LoadedPlugins);
        Assert.False(host.IsLoaded("test-plugin"));
        Assert.NotNull(eventArgs);
        Assert.Equal("test-plugin", eventArgs.PluginId);
        Assert.Equal(descriptor, eventArgs.Descriptor);
    }

    [Fact]
    public async Task UnloadAsync_WithNonExistentPlugin_ReturnsFalse()
    {
        // Arrange
        using var host = new PluginHost();

        // Act
        var result = await host.UnloadAsync("non-existent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetPlugin_WithLoadedPlugin_ReturnsHandle()
    {
        // Arrange
        using var host = CreateTestPluginHost();
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/fake/path.dll");
        var expectedHandle = await host.LoadPluginAsync(descriptor);

        // Act
        var actualHandle = host.GetPlugin("test-plugin");

        // Assert
        Assert.NotNull(actualHandle);
        Assert.Equal(expectedHandle, actualHandle);
    }

    [Fact]
    public void GetPlugin_WithNonExistentPlugin_ReturnsNull()
    {
        // Arrange
        using var host = new PluginHost();

        // Act
        var handle = host.GetPlugin("non-existent");

        // Assert
        Assert.Null(handle);
    }

    [Fact]
    public void GetPlugin_WithNullId_ReturnsNull()
    {
        // Arrange
        using var host = new PluginHost();

        // Act
        var handle = host.GetPlugin(null!);

        // Assert
        Assert.Null(handle);
    }

    [Fact]
    public async Task PluginHost_StateTransitions_WorkCorrectly()
    {
        // Arrange
        using var host = CreateTestPluginHost();
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/fake/path.dll");

        // Act & Assert - Load
        var handle = await host.LoadPluginAsync(descriptor);
        Assert.Equal(PluginState.Loaded, handle.State);

        // Act & Assert - Activate
        await host.ActivateAsync("test-plugin");
        Assert.Equal(PluginState.Active, handle.State);

        // Act & Assert - Deactivate
        await host.DeactivateAsync("test-plugin");
        Assert.Equal(PluginState.Deactivated, handle.State);

        // Act & Assert - Reactivate
        await host.ActivateAsync("test-plugin");
        Assert.Equal(PluginState.Active, handle.State);

        // Act & Assert - Unload
        await host.UnloadAsync("test-plugin");
        Assert.Equal(PluginState.Unloaded, handle.State);
    }

    [Fact]
    public async Task PluginHost_Dispose_UnloadsAllPlugins()
    {
        // Arrange
        var host = CreateTestPluginHost();
        var descriptor1 = new PluginDescriptor("plugin1", "1.0.0", "/fake/path1.dll");
        var descriptor2 = new PluginDescriptor("plugin2", "1.0.0", "/fake/path2.dll");
        
        await host.LoadPluginAsync(descriptor1);
        await host.LoadPluginAsync(descriptor2);
        
        Assert.Equal(2, host.LoadedPlugins.Count);

        // Act
        host.Dispose();

        // Assert
        Assert.Throws<ObjectDisposedException>(() => host.LoadedPlugins);
    }

    [Fact]
    public void PluginHost_DisposedHost_ThrowsObjectDisposedException()
    {
        // Arrange
        var host = new PluginHost();
        host.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => host.LoadedPlugins);
        Assert.Throws<ObjectDisposedException>(() => host.IsLoaded("test"));
        Assert.Throws<ObjectDisposedException>(() => host.GetPlugin("test"));
    }

    [Fact]
    public async Task PluginHost_WithCustomLoadContextFactory_UsesCustomFactory()
    {
        // Arrange
        var customContextCreated = false;
        using var host = new PluginHost(descriptor =>
        {
            customContextCreated = true;
            var context = new FakeLoadContext(descriptor.Id + "-custom");
            // Pre-register fake assemblies
            foreach (var path in descriptor.AssemblyPaths)
            {
                context.RegisterAssembly(path, typeof(PluginHostTests).Assembly);
            }
            return context;
        });
        
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/fake/path.dll");

        // Act
        var handle = await host.LoadPluginAsync(descriptor);

        // Assert
        Assert.True(customContextCreated);
        Assert.Equal("test-plugin-custom", handle.LoadContext.Id);
    }
}