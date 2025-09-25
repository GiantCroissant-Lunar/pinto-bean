using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Integration tests for P4-07: Registry integration — swap active provider(s) on plugin (de)activation.
/// Validates that plugin lifecycle events properly register/unregister providers in the service registry.
/// </summary>
public class PluginRegistryIntegrationTests
{
    /// <summary>
    /// Test plugin that implements IProviderHost to expose service providers.
    /// </summary>
    private class TestPlugin : IProviderHost
    {
        private readonly List<ProviderDescriptor> _providers;

        public TestPlugin(string providerId = "test-provider")
        {
            var capabilities = new ProviderCapabilities
            {
                ProviderId = providerId,
                Priority = Priority.Normal,
                Platform = Platform.Any,
                RegisteredAt = DateTime.UtcNow
            };

            _providers = new List<ProviderDescriptor>
            {
                new ProviderDescriptor(typeof(IHelloService), new TestHelloService(), capabilities)
            };
        }

        public IEnumerable<ProviderDescriptor> GetProviders() => _providers;
    }

    /// <summary>
    /// Simple test implementation of IHelloService for integration testing.
    /// </summary>
    private class TestHelloService : IHelloService
    {
        public Task<HelloResponse> SayHelloAsync(HelloRequest request, System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new HelloResponse { Message = $"Hello {request.Name}" });
        }

        public Task<HelloResponse> SayGoodbyeAsync(HelloRequest request, System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new HelloResponse { Message = $"Goodbye {request.Name}" });
        }
    }

    /// <summary>
    /// Test load context that can return a pre-configured plugin instance.
    /// </summary>
    private class TestPluginLoadContext : ILoadContext
    {
        private readonly FakeLoadContext _fakeLoadContext;
        private readonly object _pluginInstance;
        private readonly Type _pluginType;

        public TestPluginLoadContext(string id, object pluginInstance, string assemblyPath = "/fake/test.dll")
        {
            _fakeLoadContext = new FakeLoadContext(id);
            _pluginInstance = pluginInstance;
            _pluginType = pluginInstance.GetType();
            
            // Register the current assembly so FakeLoadContext can load it
            _fakeLoadContext.RegisterAssembly(assemblyPath, _pluginType.Assembly);
        }

        public string Id => _fakeLoadContext.Id;
        public bool IsDisposed => _fakeLoadContext.IsDisposed;

        public Assembly Load(string assemblyPath) => _fakeLoadContext.Load(assemblyPath);
        public Assembly Load(Type type) => _fakeLoadContext.Load(type);

        public bool TryGetType(string typeName, out Type? type)
        {
            if (typeName == _pluginType.FullName || typeName == _pluginType.Name)
            {
                type = _pluginType;
                return true;
            }
            return _fakeLoadContext.TryGetType(typeName, out type);
        }

        public T CreateInstance<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params object?[]? args)
        {
            if (_pluginInstance is T instance)
                return instance;
            return _fakeLoadContext.CreateInstance<T>(args);
        }

        public object CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, params object?[]? args)
        {
            if (type.IsAssignableFrom(_pluginInstance.GetType()))
                return _pluginInstance;
            return _fakeLoadContext.CreateInstance(type, args);
        }

        public void Dispose() => _fakeLoadContext.Dispose();
    }

    [Fact]
    public async Task PluginActivation_WithProviderHost_RegistersProviders()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var plugin = new TestPlugin("test-plugin-1");
        var loadContext = new TestPluginLoadContext("test-plugin", plugin);
        
        var host = new PluginHost(descriptor => loadContext, registry);
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/fake/test.dll")
        {
            Capabilities = new ProviderCapabilities
            {
                ProviderId = "test-plugin-1",
                Priority = Priority.Normal,
                Platform = Platform.Any,
                RegisteredAt = DateTime.UtcNow,
                Metadata = ImmutableDictionary<string, object>.Empty.Add("entryType", typeof(TestPlugin).FullName!)
            }
        };

        bool providerChangeEventFired = false;
        registry.ProviderChanged += (sender, e) => providerChangeEventFired = true;

        // Act - Load and activate plugin
        var handle = await host.LoadPluginAsync(descriptor);
        var activated = await host.ActivateAsync("test-plugin");

        // Assert
        Assert.True(activated);
        Assert.True(providerChangeEventFired);
        
        // Check that providers are registered
        var registrations = registry.GetRegistrations<IHelloService>();
        Assert.Single(registrations);
        
        var registration = registrations.First();
        Assert.Equal("test-plugin-1", registration.Capabilities.ProviderId);
    }

    [Fact]
    public async Task PluginDeactivation_WithProviderHost_UnregistersProviders()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var plugin = new TestPlugin("test-plugin-2");
        var loadContext = new TestPluginLoadContext("test-plugin", plugin);
        
        var host = new PluginHost(descriptor => loadContext, registry);
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/fake/test.dll")
        {
            Capabilities = new ProviderCapabilities
            {
                ProviderId = "test-plugin-2",
                Priority = Priority.Normal,
                Platform = Platform.Any,
                RegisteredAt = DateTime.UtcNow,
                Metadata = ImmutableDictionary<string, object>.Empty.Add("entryType", typeof(TestPlugin).FullName!)
            }
        };

        // Act - Load, activate, then deactivate plugin
        var handle = await host.LoadPluginAsync(descriptor);
        await host.ActivateAsync("test-plugin");
        
        // Verify providers are registered before deactivation
        Assert.Single(registry.GetRegistrations<IHelloService>());
        
        var deactivated = await host.DeactivateAsync("test-plugin");

        // Assert
        Assert.True(deactivated);
        
        // Check that providers are unregistered
        var registrations = registry.GetRegistrations<IHelloService>();
        Assert.Empty(registrations);
    }

    [Fact]
    public async Task PluginUnload_WithProviderHost_UnregistersProviders()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var plugin = new TestPlugin("test-plugin-3");
        var loadContext = new TestPluginLoadContext("test-plugin", plugin);
        
        var host = new PluginHost(descriptor => loadContext, registry); 
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/fake/test.dll")
        {
            Capabilities = new ProviderCapabilities
            {
                ProviderId = "test-plugin-3",
                Priority = Priority.Normal,
                Platform = Platform.Any,
                RegisteredAt = DateTime.UtcNow,
                Metadata = ImmutableDictionary<string, object>.Empty.Add("entryType", typeof(TestPlugin).FullName!)
            }
        };

        // Act - Load, activate, then unload plugin
        var handle = await host.LoadPluginAsync(descriptor);
        await host.ActivateAsync("test-plugin");
        
        // Verify providers are registered before unload
        Assert.Single(registry.GetRegistrations<IHelloService>());
        
        var unloaded = await host.UnloadAsync("test-plugin");

        // Assert
        Assert.True(unloaded);
        
        // Check that providers are unregistered
        var registrations = registry.GetRegistrations<IHelloService>();
        Assert.Empty(registrations);
    }

    [Fact]
    public async Task PluginSwap_WithProviderHost_SwapsProviders()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var plugin1 = new TestPlugin("plugin-1");
        var plugin2 = new TestPlugin("plugin-2");
        
        var loadContext1 = new TestPluginLoadContext("plugin-1", plugin1, "/fake/plugin1.dll");
        var loadContext2 = new TestPluginLoadContext("plugin-2", plugin2, "/fake/plugin2.dll");
        
        var loadContextFactory = new Func<PluginDescriptor, ILoadContext>(descriptor =>
            descriptor.Id == "plugin-1" ? loadContext1 : loadContext2);
            
        var host = new PluginHost(loadContextFactory, registry);
        
        var descriptor1 = new PluginDescriptor("plugin-1", "1.0.0", "/fake/plugin1.dll")
        {
            Capabilities = new ProviderCapabilities
            {
                ProviderId = "plugin-1",
                Priority = Priority.Normal,
                Platform = Platform.Any,
                RegisteredAt = DateTime.UtcNow,
                Metadata = ImmutableDictionary<string, object>.Empty.Add("entryType", typeof(TestPlugin).FullName!)
            }
        };
        
        var descriptor2 = new PluginDescriptor("plugin-2", "1.0.0", "/fake/plugin2.dll")
        {
            Capabilities = new ProviderCapabilities
            {
                ProviderId = "plugin-2",
                Priority = Priority.Normal,
                Platform = Platform.Any,
                RegisteredAt = DateTime.UtcNow,
                Metadata = ImmutableDictionary<string, object>.Empty.Add("entryType", typeof(TestPlugin).FullName!)
            }
        };

        // Act - Load and activate first plugin
        var handle1 = await host.LoadPluginAsync(descriptor1);
        await host.ActivateAsync("plugin-1");
        
        // Verify first plugin's providers are registered
        var registrations = registry.GetRegistrations<IHelloService>();
        Assert.Single(registrations);
        Assert.Equal("plugin-1", registrations.First().Capabilities.ProviderId);
        
        // Load and activate second plugin (simulating swap)
        var handle2 = await host.LoadPluginAsync(descriptor2);
        await host.ActivateAsync("plugin-2");
        
        // Deactivate first plugin
        await host.DeactivateAsync("plugin-1");

        // Assert - Only second plugin's providers should be registered
        registrations = registry.GetRegistrations<IHelloService>();
        Assert.Single(registrations);
        Assert.Equal("plugin-2", registrations.First().Capabilities.ProviderId);
    }

    [Fact]
    public async Task PluginHost_WithoutServiceRegistry_DoesNotThrow()
    {
        // Arrange - PluginHost without service registry
        var plugin = new TestPlugin("test-plugin");
        var loadContext = new TestPluginLoadContext("test-plugin", plugin);
        var host = new PluginHost(descriptor => loadContext, serviceRegistry: null);
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/fake/test.dll")
        {
            Capabilities = new ProviderCapabilities
            {
                ProviderId = "test-plugin",
                Priority = Priority.Normal,
                Platform = Platform.Any,
                RegisteredAt = DateTime.UtcNow,
                Metadata = ImmutableDictionary<string, object>.Empty.Add("entryType", typeof(TestPlugin).FullName!)
            }
        };

        // Act - Should not throw even without registry
        var handle = await host.LoadPluginAsync(descriptor);
        var activated = await host.ActivateAsync("test-plugin");
        var deactivated = await host.DeactivateAsync("test-plugin");

        // Assert
        Assert.True(activated);
        Assert.True(deactivated);
    }
}