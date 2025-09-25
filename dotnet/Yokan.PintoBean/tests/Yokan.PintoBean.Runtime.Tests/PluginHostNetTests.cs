using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for PluginHostNet factory methods.
/// </summary>
public class PluginHostNetTests
{
    [Fact]
    public void Create_ReturnsPluginHostWithAlcLoadContext()
    {
        // Act
        using var host = PluginHostNet.Create();

        // Assert
        Assert.NotNull(host);
        Assert.IsType<PluginHost>(host);
        Assert.Empty(host.LoadedPlugins);
    }

    [Fact]
    public void Create_WithCustomFactory_UsesCustomFactory()
    {
        // Arrange
        var customFactoryCalled = false;
        ILoadContext CustomFactory(PluginDescriptor descriptor)
        {
            customFactoryCalled = true;
            return new FakeLoadContext(descriptor.Id);
        }

        // Act
        using var host = PluginHostNet.Create(CustomFactory);

        // Assert
        Assert.NotNull(host);
        Assert.IsType<PluginHost>(host);

        // Load a plugin to trigger the factory
        var descriptor = new PluginDescriptor("test", "1.0", "/fake/path.dll");
        var loadTask = host.LoadPluginAsync(descriptor);
        
        // The custom factory should have been called during load
        Assert.True(customFactoryCalled);
    }

    [Fact]
    public async Task PluginHostNet_WithAlcLoadContext_CanLoadAndUnloadPlugins()
    {
        // Arrange
        using var host = PluginHostNet.Create();
        var assemblyPath = GetTestAssemblyPath();
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", assemblyPath);

        // Act - Load
        var handle = await host.LoadPluginAsync(descriptor);

        // Assert - Load
        Assert.NotNull(handle);
        Assert.Equal(PluginState.Loaded, handle.State);
        Assert.IsType<AlcLoadContext>(handle.LoadContext);
        Assert.Equal("test-plugin", handle.LoadContext.Id);

        // Act - Unload
        var unloaded = await host.UnloadAsync("test-plugin");

        // Assert - Unload
        Assert.True(unloaded);
        Assert.Equal(PluginState.Unloaded, handle.State);
        Assert.True(handle.LoadContext.IsDisposed);
    }

    [Fact]
    public async Task PluginHostNet_CanActivateAndDeactivatePlugins()
    {
        // Arrange
        using var host = PluginHostNet.Create();
        var assemblyPath = GetTestAssemblyPath();
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", assemblyPath);
        await host.LoadPluginAsync(descriptor);

        // Act & Assert - Activate
        var activated = await host.ActivateAsync("test-plugin");
        Assert.True(activated);

        var handle = host.GetPlugin("test-plugin");
        Assert.NotNull(handle);
        Assert.Equal(PluginState.Active, handle.State);

        // Act & Assert - Deactivate
        var deactivated = await host.DeactivateAsync("test-plugin");
        Assert.True(deactivated);
        Assert.Equal(PluginState.Deactivated, handle.State);
    }

    [UnconditionalSuppressMessage("SingleFile", "IL3000:Avoid accessing Assembly file path when publishing as a single file", 
        Justification = "Test code handles single-file scenario appropriately.")]
    private static string GetTestAssemblyPath()
    {
        var location = typeof(PluginHostNetTests).Assembly.Location;
        if (!string.IsNullOrEmpty(location))
        {
            return location;
        }
        
        // Fallback for single-file scenarios
        var baseDir = AppContext.BaseDirectory;
        var testAssemblyPath = Path.Combine(baseDir, "Yokan.PintoBean.Runtime.Tests.dll");
        if (File.Exists(testAssemblyPath))
        {
            return testAssemblyPath;
        }
        
        // Use runtime assembly as fallback
        return Path.Combine(baseDir, "Yokan.PintoBean.Runtime.dll");
    }
}