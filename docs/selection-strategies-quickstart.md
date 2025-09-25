# Selection Strategies Quickstart

This guide provides a practical introduction to using selection strategies in Yokan PintoBean, based on [RFC-0003](rfcs/rfc-0003-selection-strategies.md).

## Quick Start

### 1. Enable Selection Strategies in DI

Add selection strategies to your dependency injection container:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Yokan.PintoBean.Runtime;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Enable selection strategies with RFC-0003 defaults
        services.AddSelectionStrategies();
        
        // Register your service implementations
        services.AddSingleton<IAnalyticsService, UnityAnalyticsService>();
        services.AddSingleton<IAnalyticsService, FirebaseAnalyticsService>();
        services.AddSingleton<IResourceService, FileResourceService>();
        services.AddSingleton<IAIService, OpenAIService>();
    })
    .Build();
```

### 2. Understanding Strategy Types

#### PickOne (Default)
Selects a single provider based on:
- Capability filtering
- Platform filtering  
- Priority ordering
- Deterministic tie-breaking

**Use case**: Primary/fallback scenarios, deterministic routing

```csharp
// Resources use PickOne by default - selects best provider
var resourceService = serviceProvider.GetService<IResourceService>();
var data = await resourceService.LoadAsync("player-config.json");
```

#### FanOut
Invokes **all** matched providers and aggregates results/failures:

**Use case**: Broadcasting operations, multi-target analytics

```csharp
// Analytics uses FanOut by default - sends to all providers
var analyticsService = serviceProvider.GetService<IAnalyticsService>();
await analyticsService.TrackEvent("player.level.complete", eventData);
// ^ Automatically sent to Unity Analytics, Firebase, etc.
```

#### Sharded
Routes by key extraction (e.g., event name prefix) with support for explicit mapping:

**Use case**: Load balancing, geographic routing, A/B testing, deterministic provider assignment

```csharp
// Basic sharded routing: uses consistent hashing based on event prefix
var analyticsService = serviceProvider.GetService<IAnalyticsService>();
await analyticsService.TrackEvent("player.level.complete", eventData);  // → Player shard
await analyticsService.TrackEvent("system.startup", eventData);         // → System shard

// Advanced: Explicit shard mapping with fallback to consistent hashing
services.AddSelectionStrategies(options =>
{
    // Define explicit shard-to-provider mapping
    var explicitShardMap = new Dictionary<string, string>
    {
        ["player"] = "UnityAnalyticsProvider",
        ["system"] = "FirebaseAnalyticsProvider"
        // Other shards (e.g., "inventory") will use consistent hashing
    };
    
    options.UseStrategy<IAnalyticsService>(
        DefaultSelectionStrategies.CreateAnalyticsShardedWithMap<IAnalyticsService>(explicitShardMap)
    );
});
```

## Default Category Mappings

PintoBean includes sensible defaults per RFC-0003:

| Category | Default Strategy | Reasoning |
|----------|------------------|-----------|
| **Analytics** | `FanOut` | Common to send to Unity Analytics + Firebase simultaneously |
| **Resources** | `PickOne` | Primary backend with fallback via resilience patterns |
| **SceneFlow** | `PickOne` | Deterministic flow control with controlled fallback |
| **AI** | `PickOne` | Router decides backend, optional FanOut for evaluation |

## Overriding Strategies

### Per-Category Overrides

```csharp
services.AddSelectionStrategies(options =>
{
    // Override Analytics to use PickOne instead of FanOut
    options.SetCategoryDefault(ServiceCategory.Analytics, SelectionStrategyType.PickOne);
    
    // Use Sharded for AI services
    options.SetCategoryDefault(ServiceCategory.AI, SelectionStrategyType.Sharded);
});
```

### Per-Contract Overrides

Use fluent extension methods for specific service contracts:

```csharp
services.AddSelectionStrategies()
    .UsePickOneFor<IAnalyticsService>()    // Override Analytics default (FanOut → PickOne)
    .UseFanOutFor<IResourceService>()      // Override Resources default (PickOne → FanOut)  
    .UseShardedFor<IAIService>();          // Override AI default (PickOne → Sharded)
```

### Advanced Configuration

```csharp
services.AddSelectionStrategies(options =>
{
    // Combine category and service-specific overrides
    options.SetCategoryDefault(ServiceCategory.Analytics, SelectionStrategyType.FanOut);
    
    // Custom strategy for specific service
    options.UseStrategyFor<IAnalyticsService>(SelectionStrategyType.PickOne);
    
    // Custom factory for complex scenarios
    options.UseCustomStrategyFor<ISpecialService>(provider => 
        new CustomSelectionStrategy<ISpecialService>(
            provider.GetService<IServiceRegistry>()));
});
```

## Configuration Validation

Verify your configuration at runtime:

```csharp
var options = serviceProvider.GetRequiredService<SelectionStrategyOptions>();

Console.WriteLine("Category defaults:");
Console.WriteLine($"  Analytics: {options.GetDefaultForCategory(ServiceCategory.Analytics)}");
Console.WriteLine($"  Resources: {options.GetDefaultForCategory(ServiceCategory.Resources)}");
Console.WriteLine($"  SceneFlow: {options.GetDefaultForCategory(ServiceCategory.SceneFlow)}");
Console.WriteLine($"  AI: {options.GetDefaultForCategory(ServiceCategory.AI)}");

Console.WriteLine("\nService-specific overrides:");
var overrides = options.GetAllStrategyOverrides();
foreach (var (serviceType, strategy) in overrides)
{
    Console.WriteLine($"  {serviceType.Name}: {strategy}");
}
```

## Platform-Specific Configuration

### Unity (ScriptableObjects)
```csharp
// In Unity, create StrategyMappingAsset ScriptableObjects
// These get loaded into the Registry at boot time
[CreateAssetMenu(menuName = "PintoBean/Strategy Mapping")]
public class StrategyMappingAsset : ScriptableObject
{
    [SerializeField] private ServiceCategory category;
    [SerializeField] private SelectionStrategyType strategy;
    // ...
}
```

### .NET/Godot (appsettings.json)
```json
{
  "PintoBean": {
    "SelectionStrategies": {
      "CategoryDefaults": {
        "Analytics": "FanOut",
        "Resources": "PickOne",
        "AI": "Sharded"
      },
      "ServiceOverrides": {
        "IAnalyticsService": "PickOne",
        "IResourceService": "FanOut"
      }
    }
  }
}
```

## Complete Example

Here's a complete working example:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Yokan.PintoBean.Runtime;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                // Enable strategies with custom configuration
                services.AddSelectionStrategies(options =>
                {
                    // Keep Analytics as FanOut (broadcast to all)
                    // Override AI to use Sharded routing
                    options.SetCategoryDefault(ServiceCategory.AI, SelectionStrategyType.Sharded);
                })
                .UsePickOneFor<IResourceService>(); // Override Resources to PickOne
                
                // Register multiple providers
                services.AddSingleton<IAnalyticsService, UnityAnalyticsService>();
                services.AddSingleton<IAnalyticsService, FirebaseAnalyticsService>();
                services.AddSingleton<IResourceService, FileResourceService>();
                services.AddSingleton<IResourceService, NetworkResourceService>();
                services.AddSingleton<IAIService, OpenAIService>();
                services.AddSingleton<IAIService, LocalAIService>();
            })
            .Build();

        // Use the services - strategies are applied automatically
        var analyticsService = host.Services.GetRequiredService<IAnalyticsService>();
        var resourceService = host.Services.GetRequiredService<IResourceService>();
        var aiService = host.Services.GetRequiredService<IAIService>();
        
        // FanOut: Sent to both Unity and Firebase
        await analyticsService.TrackEvent("player.achievement.unlocked", data);
        
        // PickOne: Uses best available resource provider
        var config = await resourceService.LoadAsync("game-config.json");
        
        // Sharded: Routes by request characteristics
        var response = await aiService.ProcessAsync("generate.character.dialog", prompt);
        
        await host.RunAsync();
    }
}
```

## Next Steps

- Review [RFC-0003](rfcs/rfc-0003-selection-strategies.md) for detailed architecture
- Explore the [Strategy Demo Console](../dotnet/Yokan.PintoBean/samples/PintoBean.StrategyDemo.Console/) sample
- Check out other [RFCs](rfcs/) for broader platform architecture

## Troubleshooting

**Q: My strategy overrides aren't working**
A: Ensure you're calling `AddSelectionStrategies()` before registering service implementations.

**Q: FanOut isn't aggregating results properly**  
A: Verify your service contracts return `Task<T>` or `ValueTask<T>` for proper aggregation.

**Q: Sharded routing seems random**
A: Check your key extraction logic - default uses event name prefix before first dot. For deterministic routing, consider using explicit shard maps.

**Q: How do I ensure specific shards always go to specific providers?**
A: Use explicit shard mapping with `CreateAnalyticsShardedWithMap` or `CreateShardedWithMap` factory methods. Unmapped shards will fallback to consistent hashing.

**Q: Performance issues with FanOut**
A: Consider using circuit breakers via resilience patterns, or switch to PickOne for high-frequency operations.