# P5-02: Resources Sample (PickOne + Fallback via Resilience)

This sample demonstrates deterministic single-provider selection with fallback on failure using the PintoBean service registry platform.

## Overview

The sample showcases:

- **PickOne Selection Strategy**: Deterministically selects the highest priority provider available
- **Resilience Patterns**: Handles transient failures within the selected provider using Polly policies
- **Dynamic Provider Fallback**: Shows how to achieve fallback by dynamically registering/unregistering providers
- **Tier-1 Service Contract**: Implements `IResourceStore` interface with async Task-based operations

## Key Components

### IResourceStore Interface
```csharp
public interface IResourceStore
{
    Task<ResourceData> LoadResourceAsync(string resourceKey, CancellationToken cancellationToken = default);
    Task<bool> ResourceExistsAsync(string resourceKey, CancellationToken cancellationToken = default);
    string StoreName { get; }
}
```

### Provider Implementations
1. **CacheResourceStore** (Priority: Critical) - Fast in-memory cache with transient failure simulation
2. **NetworkResourceStore** (Priority: High) - Network-based resource loading
3. **LocalFileResourceStore** (Priority: Normal) - Reliable local file system fallback

## Demonstration Scenarios

1. **Normal Operation**: PickOne selects cache store (highest priority)
2. **Transient Failure**: Resilience executor retries within same provider
3. **Provider Fallback**: Dynamic unregistration causes fallback to network store
4. **Deeper Fallback**: Multiple provider removal shows fallback chain
5. **Service Recovery**: Re-registration demonstrates provider recovery
6. **Total Failure**: Graceful handling when no providers are available

## Running the Sample

```bash
cd dotnet/Yokan.PintoBean/samples/PintoBean.Resources.Demo.Console
dotnet run
```

## Sample Output

```
=== P5-02: Resources Sample (PickOne + Fallback via Resilience) ===
Demonstrating deterministic single-provider selection with fallback on failure

🔧 Setting up Dependency Injection with Polly Resilience...
📝 Registering resource store providers:
  ✅ Registered: network-resources (Priority: High)
  ✅ Registered: local-file-resources (Priority: Normal)  
  ✅ Registered: cache-resources (Priority: Critical)

🔍 Demonstrating PickOne Selection Strategy with Resilience:
   PickOne strategy selects the highest priority available provider
   Resilience patterns handle transient failures within selected provider
   For true fallback, providers can be unregistered/registered dynamically

1️⃣  Testing normal operation:
   ⏱️  Load time: 42ms
   📄 Loaded: player-config.json from CacheStore
   📊 Content preview: { "playerName": "CachedPlayer", "level": 1...

[... more scenarios ...]
```

## Architecture Insights

### PickOne Strategy Behavior
- **Deterministic**: Always selects the same provider given the same registered providers
- **Priority-based**: Uses `Priority` enum (Critical > High > Normal > Low)
- **Capability-aware**: Respects provider capabilities and platform filters

### Resilience Integration
- **Retry Logic**: Polly-based retry for transient failures
- **Timeout Handling**: Configurable timeouts prevent hanging operations
- **Failure Classification**: Distinguishes between transient and permanent failures

### Fallback Patterns
- **Provider-level**: Dynamic registration/unregistration for availability changes
- **Retry-level**: Multiple attempts within the same provider for transient issues
- **Graceful Degradation**: Clear error messages when no providers are available

## Testing

The sample includes comprehensive tests in `ResourcesP5_02Tests.cs` covering:
- Priority-based provider selection
- Resilience retry behavior
- Dynamic provider fallback
- Error handling scenarios

```bash
dotnet test --filter "ResourcesP5_02Tests"
```

## Related Documentation

- [Selection Strategies Quickstart](../../../../docs/selection-strategies-quickstart.md)
- [PintoBean Runtime API](../../src/Yokan.PintoBean.Runtime/)
- [Resilience Patterns](../../src/Yokan.PintoBean.Runtime/PollyResilienceExecutor.cs)