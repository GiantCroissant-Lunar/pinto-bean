using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Acceptance criteria tests for P4-01: Load-context abstraction (ILoadContext) + Plugin Host (IPluginHost).
/// Verifies that all requirements from the issue are met.
/// </summary>
public class AcceptanceCriteriaP4Tests
{
    [Fact]
    public void ILoadContext_HasRequiredMethods()
    {
        // Verify ILoadContext has all required methods from the issue
        var interfaceType = typeof(ILoadContext);
        
        // Assembly Load(Type/Path)
        Assert.NotNull(interfaceType.GetMethod("Load", new[] { typeof(string) }));
        Assert.NotNull(interfaceType.GetMethod("Load", new[] { typeof(Type) }));
        
        // TryGetType
        var tryGetTypeMethod = interfaceType.GetMethod("TryGetType");
        Assert.NotNull(tryGetTypeMethod);
        Assert.Equal(typeof(bool), tryGetTypeMethod.ReturnType);
        
        // CreateInstance - verified through functional tests rather than reflection
        // to avoid trimmer warnings with DynamicallyAccessedMembersAttribute
        
        // Dispose (from IDisposable)
        Assert.True(typeof(IDisposable).IsAssignableFrom(interfaceType));
    }

    [Fact]
    public void IPluginHost_HasRequiredMethods()
    {
        // Verify IPluginHost has all required methods from the issue
        var interfaceType = typeof(IPluginHost);
        
        // LoadPlugin(PluginDescriptor)
        var loadPluginMethod = interfaceType.GetMethod("LoadPluginAsync");
        Assert.NotNull(loadPluginMethod);
        Assert.Equal(typeof(Task<PluginHandle>), loadPluginMethod.ReturnType);
        
        // Activate(), Deactivate(), Unload()
        Assert.NotNull(interfaceType.GetMethod("ActivateAsync"));
        Assert.NotNull(interfaceType.GetMethod("DeactivateAsync"));
        Assert.NotNull(interfaceType.GetMethod("UnloadAsync"));
        
        // Dispose (from IDisposable)
        Assert.True(typeof(IDisposable).IsAssignableFrom(interfaceType));
    }

    [Fact]
    public void IPluginHost_HasRequiredEvents()
    {
        // Verify IPluginHost has all required events from the issue
        var interfaceType = typeof(IPluginHost);
        
        // Events: PluginLoaded, PluginUnloaded, PluginFailed
        Assert.NotNull(interfaceType.GetEvent("PluginLoaded"));
        Assert.NotNull(interfaceType.GetEvent("PluginUnloaded"));
        Assert.NotNull(interfaceType.GetEvent("PluginFailed"));
    }

    [Fact]
    public void PluginDescriptor_HasRequiredProperties()
    {
        // Verify PluginDescriptor has all required properties from the issue
        var type = typeof(PluginDescriptor);
        
        // Id, Version, AssemblyPath(s), Manifest?, Capabilities
        Assert.NotNull(type.GetProperty("Id"));
        Assert.NotNull(type.GetProperty("Version"));
        Assert.NotNull(type.GetProperty("AssemblyPaths"));
        Assert.NotNull(type.GetProperty("Manifest"));
        Assert.NotNull(type.GetProperty("Capabilities"));
        
        // Test constructor
        var descriptor = new PluginDescriptor("test", "1.0", "/path.dll");
        Assert.Equal("test", descriptor.Id);
        Assert.Equal("1.0", descriptor.Version);
        Assert.Single(descriptor.AssemblyPaths);
    }

    [Fact]
    public void PluginHandle_HasRequiredProperties()
    {
        // Verify PluginHandle has all required properties from the issue
        var type = typeof(PluginHandle);
        
        // Id, LoadContext, State
        Assert.NotNull(type.GetProperty("Id"));
        Assert.NotNull(type.GetProperty("LoadContext"));
        Assert.NotNull(type.GetProperty("State"));
        Assert.NotNull(type.GetProperty("Descriptor"));
        
        // Test constructor and state
        using var context = new FakeLoadContext();
        var descriptor = new PluginDescriptor("test", "1.0", "/path.dll");
        var handle = new PluginHandle("test", context, descriptor);
        
        Assert.Equal("test", handle.Id);
        Assert.Equal(context, handle.LoadContext);
        Assert.Equal(PluginState.Loaded, handle.State);
        Assert.Equal(descriptor, handle.Descriptor);
    }

    [Fact]
    public void RuntimeBuildsWithEngineAgnosticInterfaces()
    {
        // Verify runtime builds with engine-agnostic interfaces (no Unity/Godot dependencies)
        var runtimeAssembly = typeof(IPluginHost).Assembly;
        
        // Check that there are no references to engine-specific assemblies
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
        var referencedAssemblies = runtimeAssembly.GetReferencedAssemblies();
#pragma warning restore IL2026
        
        foreach (var refAssembly in referencedAssemblies)
        {
            Assert.DoesNotContain("Unity", refAssembly.Name!);
            Assert.DoesNotContain("Godot", refAssembly.Name!);
            Assert.DoesNotContain("Unreal", refAssembly.Name!);
        }
        
        // Verify basic functionality works
        using var host = new PluginHost();
        Assert.NotNull(host);
        Assert.Empty(host.LoadedPlugins);
    }

    [Fact]
    public async Task UnitTestsCanNewUpFakeLoadContextAndDrivePluginHostStateTransitions()
    {
        // This test demonstrates the acceptance criteria:
        // "Unit tests can new up a FakeLoadContext and drive IPluginHost state transitions"
        
        // Arrange - Create FakeLoadContext and PluginHost
        var fakeContext = new FakeLoadContext("test-context");
        fakeContext.RegisterAssembly("/fake/plugin.dll", typeof(AcceptanceCriteriaP4Tests).Assembly);
        
        using var host = new PluginHost(descriptor => fakeContext);
        var descriptor = new PluginDescriptor("test-plugin", "1.0.0", "/fake/plugin.dll");
        
        // Track state transitions through events
        var loadedEventFired = false;
        var unloadedEventFired = false;
        host.PluginLoaded += (s, e) => loadedEventFired = true;
        host.PluginUnloaded += (s, e) => unloadedEventFired = true;
        
        // Act & Assert - Load
        var handle = await host.LoadPluginAsync(descriptor);
        Assert.Equal(PluginState.Loaded, handle.State);
        Assert.True(loadedEventFired);
        
        // Act & Assert - Activate
        var activated = await host.ActivateAsync("test-plugin");
        Assert.True(activated);
        Assert.Equal(PluginState.Active, handle.State);
        
        // Act & Assert - Deactivate
        var deactivated = await host.DeactivateAsync("test-plugin");
        Assert.True(deactivated);
        Assert.Equal(PluginState.Deactivated, handle.State);
        
        // Act & Assert - Unload
        var unloaded = await host.UnloadAsync("test-plugin");
        Assert.True(unloaded);
        Assert.Equal(PluginState.Unloaded, handle.State);
        Assert.True(unloadedEventFired);
        
        // Verify load context was used correctly
        Assert.Equal("test-context", handle.LoadContext.Id);
    }

    [Fact]
    public void FakeLoadContext_CanBeUsedForTesting()
    {
        // Verify FakeLoadContext works as intended for testing scenarios
        using var context = new FakeLoadContext("test");
        
        // Register and load assemblies
        var assembly = typeof(AcceptanceCriteriaP4Tests).Assembly;
        context.RegisterAssembly("/fake/path.dll", assembly);
        
        var loadedAssembly = context.Load("/fake/path.dll");
        Assert.Equal(assembly, loadedAssembly);
        
        // Load by type
        var loadedByType = context.Load(typeof(string));
        Assert.Equal(typeof(string).Assembly, loadedByType);
        
        // Register and find types
        context.RegisterType("TestType", typeof(string));
        var found = context.TryGetType("TestType", out var foundType);
        Assert.True(found);
        Assert.Equal(typeof(string), foundType);
        
        // Create instances
        var instance = context.CreateInstance<object>();
        Assert.NotNull(instance);
    }

    [Fact]
    public void AllRequiredTypesAreInRuntimeNamespace()
    {
        // Verify all required types are in the correct namespace
        Assert.Equal("Yokan.PintoBean.Runtime", typeof(ILoadContext).Namespace);
        Assert.Equal("Yokan.PintoBean.Runtime", typeof(IPluginHost).Namespace);
        Assert.Equal("Yokan.PintoBean.Runtime", typeof(PluginDescriptor).Namespace);
        Assert.Equal("Yokan.PintoBean.Runtime", typeof(PluginHandle).Namespace);
        Assert.Equal("Yokan.PintoBean.Runtime", typeof(PluginState).Namespace);
        Assert.Equal("Yokan.PintoBean.Runtime", typeof(FakeLoadContext).Namespace);
        Assert.Equal("Yokan.PintoBean.Runtime", typeof(PluginHost).Namespace);
    }
}