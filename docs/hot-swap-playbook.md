# Hot-Swap Playbook

This document provides operational guidance for hot-swapping plugins in both Unity and .NET Host environments within the Yokan PintoBean platform.

## Overview

Hot-swapping allows runtime replacement of plugins without stopping the application. The platform supports two distinct approaches:

- **Unity Environment**: Soft-swap using quiesce/flip/dispose pattern (no assembly unloading)
- **.NET Host Environment**: Hard-swap using collectible AssemblyLoadContext (full unload capability)

## Unity Environment: Soft-Swap Pattern

### Architecture

Unity uses `PluginHostUnity` with `HybridClrLoadContext`, which provides plugin isolation but cannot unload assemblies due to Unity runtime limitations.

### Workflow: Quiesce/Flip/Dispose

1. **Quiesce Phase**: Existing plugin is marked for graceful shutdown
2. **Flip Phase**: New plugin version becomes active
3. **Dispose Phase**: Old plugin is disposed after grace period

### Implementation

```csharp
// Create Unity-specific plugin host
using var host = new PluginHostUnity(descriptor => 
    new HybridClrLoadContext(descriptor.Id));

// Perform soft-swap
var newDescriptor = new PluginDescriptor("plugin-v2", "2.0.0", "/path/to/v2.dll");
var success = await host.SoftSwapAsync("plugin-v1", newDescriptor);
```

### Grace Period Configuration

Configure grace periods using the `QuiesceAttribute`:

```csharp
[Quiesce(Seconds = 10)]
public class MyPlugin : IPlugin
{
    // Plugin implementation
}
```

Default grace period: 5 seconds (configurable via `PluginHostUnity.DefaultGracePeriodSeconds`)

### Events

Monitor swap progress with events:

```csharp
host.PluginQuiesced += (sender, e) => 
    Console.WriteLine($"Plugin {e.PluginId} entered quiesce state");
    
host.PluginSwapped += (sender, e) => 
    Console.WriteLine($"Plugin {e.OldPluginId} replaced with {e.NewPluginId}");
```

### Limitations

- **No Assembly Unloading**: Assemblies remain in memory
- **Memory Accumulation**: Multiple versions may coexist in memory
- **Type Identity**: Shared types must resolve to same assembly to avoid cast exceptions

## .NET Host Environment: Collectible ALC Pattern

### Architecture

.NET Host uses `PluginHost` with `AlcLoadContext`, leveraging collectible `AssemblyLoadContext` for true assembly unloading.

### Workflow: Load/Unload/Collect

1. **Load Phase**: Plugin loaded in isolated collectible ALC
2. **Unload Phase**: ALC is unloaded, making assemblies eligible for GC
3. **Collection Phase**: Garbage collector reclaims memory

### Implementation

```csharp
// Create .NET-specific plugin host
using var host = PluginHostNet.Create();

// Load plugin
var descriptor = new PluginDescriptor("plugin", "1.0.0", "/path/to/plugin.dll");
await host.LoadAsync(descriptor);

// Unload plugin (triggers ALC unload)
await host.UnloadAsync("plugin");

// Force garbage collection (testing only)
GC.Collect();
GC.WaitForPendingFinalizers();
```

### Unload Verification Pattern

Use weak references to verify successful unloading:

```csharp
WeakReference alcWeakRef;
{
    var handle = await host.LoadAsync(descriptor);
    alcWeakRef = new WeakReference(handle.LoadContext);
}

await host.UnloadAsync("plugin");

// Wait for collection
var collected = WaitForUnload(alcWeakRef, out var elapsed);
Assert.True(collected, $"ALC not collected after {elapsed}ms");

static bool WaitForUnload(WeakReference alcRef, out TimeSpan elapsed, 
    int attempts = 10, TimeSpan? delay = null)
{
    var sw = Stopwatch.StartNew();
    delay ??= TimeSpan.FromMilliseconds(50);
    
    for (int i = 0; i < attempts; i++)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        
        if (!alcRef.IsAlive)
        {
            elapsed = sw.Elapsed;
            return true;
        }
        
        Thread.Sleep(delay.Value);
    }
    
    elapsed = sw.Elapsed;
    return false;
}
```

### Memory Considerations

- **Shared Dependencies**: System assemblies resolve to default context to avoid type identity issues
- **Collectible Marking**: Only plugin-specific assemblies are marked collectible
- **Resource Cleanup**: Implement `IDisposable` in plugins for proper resource cleanup

```csharp
// In AlcLoadContext.CollectibleAssemblyLoadContext
protected override Assembly? Load(AssemblyName assemblyName)
{
    // System assemblies use default context
    if (IsSystemAssembly(assemblyName))
        return null; // Fallback to default context
        
    return null; // Plugin assemblies load in this context
}

private static bool IsSystemAssembly(AssemblyName assemblyName)
{
    var name = assemblyName.Name;
    return name != null && (
        name.StartsWith("System.") ||
        name.StartsWith("Microsoft.") ||
        name.StartsWith("Yokan.PintoBean") // Share runtime types
    );
}
```

## Registry Interaction and Cache Invalidation

### ProviderChanged Events

The service registry fires `ProviderChanged` events when providers are added, removed, or updated:

```csharp
registry.ProviderChanged += (sender, e) =>
{
    Console.WriteLine($"Provider change: {e.ChangeType} for {e.ServiceType.Name}");
    
    // Cache invalidation typically handled automatically
    switch (e.ChangeType)
    {
        case ProviderChangeType.Added:
            cache.Clear(); // Invalidate selection cache
            break;
        case ProviderChangeType.Removed:
            cache.Clear();
            break;
        case ProviderChangeType.Modified:
            cache.Clear();
            break;
    }
};
```

### Cache Invalidation Strategy

Provider selection caches automatically invalidate on registry changes:

```csharp
public interface IProviderSelectionCache<TService>
{
    void Clear(); // Called on provider registration changes
    bool Remove(ISelectionContext<TService> context); // Targeted removal
    void Set(ISelectionContext<TService> context, ISelectionResult<TService> result, TimeSpan? ttl = null);
}
```

### Hot-Swap Cache Impact

During hot-swap operations:
1. Old provider is marked inactive
2. `ProviderChanged` event fires with `ProviderChangeType.Removed`
3. Selection caches are cleared
4. New provider registration triggers `ProviderChangeType.Added`
5. Subsequent selections use fresh cache entries

## Troubleshooting

### Type Identity Issues

**Problem**: `InvalidCastException` when casting between plugin types

**Cause**: Same type loaded in different assembly load contexts creates distinct type identities

**Solution**: Ensure shared types resolve to the same context

```csharp
// ❌ Wrong: Plugin contracts in plugin ALC
var plugin = alcContext.CreateInstance<IMyContract>(); // Different type identity

// ✅ Correct: Contracts in shared context
if (provider is IMyContract contract) // Same type identity
{
    contract.DoWork();
}
```

**Prevention**:
- Keep contracts in Tier-1 (shared assemblies)
- Use interface-based communication
- Avoid direct type casting across contexts

### Capability Mismatches

**Problem**: Plugin claims capabilities it doesn't actually support

**Cause**: Incorrect `ProviderCapabilities` configuration

**Solution**: Validate capabilities during registration

```csharp
public class AudioPluginCapabilities : ProviderCapabilities
{
    public bool SupportsStreaming { get; set; }
    public string[] SupportedFormats { get; set; } = Array.Empty<string>();
}

// Validate during registration
if (capabilities is AudioPluginCapabilities audio)
{
    if (audio.SupportsStreaming && !HasStreamingSupport(provider))
    {
        throw new InvalidOperationException(
            "Provider claims streaming support but doesn't implement IStreamingAudio");
    }
}
```

### Memory Leaks in Unity

**Problem**: Memory usage increases after multiple soft-swaps

**Cause**: Assemblies not unloaded, event handler references, unclosed resources

**Solutions**:
1. Implement proper disposal patterns
2. Unsubscribe from events in plugin cleanup
3. Monitor memory usage in development

```csharp
public class MyPlugin : IPlugin, IDisposable
{
    private readonly Timer _timer;
    
    public void Dispose()
    {
        _timer?.Dispose();
        // Unsubscribe from events
        SomeService.SomeEvent -= OnSomeEvent;
    }
}
```

### ALC Collection Issues in .NET

**Problem**: AssemblyLoadContext not getting collected despite unload

**Cause**: Outstanding references, event subscriptions, or unclosed resources

**Diagnostics**:
```csharp
// Check for outstanding references
[Conditional("DEBUG")]
private static void DiagnoseALC(WeakReference alcRef)
{
    if (alcRef.IsAlive && alcRef.Target is AssemblyLoadContext alc)
    {
        Console.WriteLine($"ALC {alc.Name} still alive");
        // Use memory profiler to identify references
    }
}
```

**Solutions**:
1. Ensure all plugin resources are disposed
2. Unsubscribe from events before unload
3. Avoid static references to plugin types
4. Use weak references for long-lived subscriptions

### Performance Considerations

**Unity Soft-Swap**:
- Fast swap times (no assembly loading overhead)
- Memory accumulation over time
- Suitable for frequent updates

**.NET Hard-Swap**:
- Slower swap times (assembly load/unload overhead)
- True memory reclamation
- Suitable for major updates or memory-constrained environments

## Best Practices

1. **Design for Swapping**: Use interfaces, avoid static state
2. **Implement Proper Disposal**: Clean up resources in `Dispose()`
3. **Monitor Memory**: Track memory usage in Unity soft-swap scenarios
4. **Test Unload Scenarios**: Verify ALC collection in .NET environments
5. **Handle Swap Failures**: Implement fallback strategies for failed swaps
6. **Validate Capabilities**: Ensure provider capabilities match implementation
7. **Use Grace Periods Wisely**: Allow sufficient time for graceful shutdown

## Related Documentation

- [RFC-0001: Service Platform Core](rfcs/rfc-0001-service-platform-core.md) - Hot-swap architecture overview
- [Selection Strategies Quickstart](selection-strategies-quickstart.md) - Provider selection configuration