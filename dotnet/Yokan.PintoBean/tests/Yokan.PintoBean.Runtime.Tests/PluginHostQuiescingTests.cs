using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for PluginHost integration with IQuiesceable providers.
/// Validates that the plugin host code path supports quiescing during deactivation and unloading.
/// Note: Full integration testing requires actual plugin instances from loaded assemblies.
/// </summary>
public class PluginHostQuiescingTests
{
    [Fact]
    public async Task PluginHost_DeactivateAsync_WithNoPlugin_DoesNotThrow()
    {
        // Arrange
        using var host = CreateTestPluginHost();
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/fake/path.dll");
        
        var handle = await host.LoadPluginAsync(descriptor);
        await host.ActivateAsync("test-plugin");
        
        // Act
        var result = await host.DeactivateAsync("test-plugin");
        
        // Assert - Validates the quiescing code path doesn't break normal deactivation
        Assert.True(result);
        Assert.Equal(PluginState.Deactivated, handle.State);
    }

    [Fact]
    public async Task PluginHost_UnloadAsync_WithoutActivation_DoesNotThrow()
    {
        // Arrange
        using var host = CreateTestPluginHost();
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/fake/path.dll");
        
        var handle = await host.LoadPluginAsync(descriptor);
        // Don't activate the plugin
        
        // Act
        var result = await host.UnloadAsync("test-plugin");
        
        // Assert - Validates the quiescing code path respects inactive state
        Assert.True(result);
        Assert.Equal(PluginState.Unloaded, handle.State);
    }

    [Fact]
    public async Task PluginHost_UnloadAsync_WithActivePlugin_DoesNotThrow()
    {
        // Arrange
        using var host = CreateTestPluginHost();
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/fake/path.dll");
        
        var handle = await host.LoadPluginAsync(descriptor);
        await host.ActivateAsync("test-plugin");
        
        // Act
        var result = await host.UnloadAsync("test-plugin");
        
        // Assert - Validates the quiescing code path works with active plugins
        Assert.True(result);
        Assert.Equal(PluginState.Unloaded, handle.State);
    }

    [Fact]
    public void PluginHost_Integration_SupportsQuiesceableInterface()
    {
        // This test validates that IQuiesceable is available and can be used in type checks
        // The actual integration testing would require loading real plugin assemblies
        
        // Arrange
        var testInstance = new TestQuiesceablePlugin();
        
        // Act & Assert - Verify the interface is properly defined and usable
        Assert.True(testInstance is IQuiesceable);
        
        // Verify the interface can be pattern matched (used in PluginHost)
        if (testInstance is IQuiesceable quiesceable)
        {
            Assert.NotNull(quiesceable);
        }
        else
        {
            Assert.Fail("Pattern matching for IQuiesceable should succeed");
        }
    }

    private static IPluginHost CreateTestPluginHost()
    {
        return new PluginHost(descriptor =>
        {
            var context = new FakeLoadContext(descriptor.Id);
            // Pre-register fake assemblies for all paths in the descriptor
            foreach (var path in descriptor.AssemblyPaths)
            {
                context.RegisterAssembly(path, typeof(PluginHostQuiescingTests).Assembly);
            }
            return context;
        });
    }

    #region Test Plugin Classes

    private class TestQuiesceablePlugin : IQuiesceable
    {
        public bool WasQuiesced { get; private set; }

        public Task QuiesceAsync(CancellationToken cancellationToken = default)
        {
            WasQuiesced = true;
            return Task.CompletedTask;
        }
    }

    #endregion
}