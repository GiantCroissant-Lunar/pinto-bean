# Samples Quickstart Guide

This guide provides run instructions for each PintoBean sample and explains how selection strategies affect routing behavior.

## Quick Reference

| Sample | Strategy | Purpose | Location |
|--------|----------|---------|----------|
| [StrategyDemo.Console](#strategydemo-console) | All | Strategy configuration overview | `samples/PintoBean.StrategyDemo.Console` |
| [Analytics.Demo.Console](#analytics-demo-console) | FanOut/Sharded | Event tracking with routing | `samples/PintoBean.Analytics.Demo.Console` |
| [Resources.Demo.Console](#resources-demo-console) | PickOne | Resource loading with fallback | `samples/PintoBean.Resources.Demo.Console` |
| [SceneFlow.Demo.Console](#sceneflow-demo-console) | PickOne | Deterministic scene management | `samples/PintoBean.SceneFlow.Demo.Console` |
| [Hello.Demo.Console](#hello-demo-console) | Basic | Platform introduction | `samples/PintoBean.Hello.Demo.Console` |

## Prerequisites

Build the solution first:

```bash
cd dotnet/Yokan.PintoBean
dotnet build
```

## Sample Descriptions

### StrategyDemo.Console

**Purpose**: Comprehensive demonstration of all three selection strategies and configuration options.

**Run Instructions**:
```bash
dotnet run --project samples/PintoBean.StrategyDemo.Console
```

**What it demonstrates**:
- **Strategy by Category**: Shows default strategy mappings per RFC-0003
- **Configuration Options**: Category defaults and service-specific overrides
- **FanOut Error Policies**: Continue vs FailFast vs Custom aggregation
- **Fire-and-Forget Operations**: Void method handling with error policies

**Key Outputs**:
- Strategy selection by service category
- Configuration dump showing defaults and overrides
- FanOut error handling scenarios with different policies

---

### Analytics.Demo.Console

**Purpose**: Shows how Analytics strategies affect event routing - demonstrates both **FanOut** (broadcast to all providers) and **Sharded** (route by event prefix).

**Run Instructions**:
```bash
dotnet run --project samples/PintoBean.Analytics.Demo.Console
```

**What it demonstrates**:

#### FanOut Strategy (Default for Analytics)
- Events sent to **ALL** registered providers simultaneously
- Useful for sending to Unity Analytics + Firebase simultaneously
- Both Unity and Firebase receive every event

#### Sharded Strategy (Event Prefix Routing)
- Events routed by **event name prefix before first dot**
- `player.*` events → Unity Analytics
- `system.*` events → Firebase Analytics
- Deterministic routing based on event characteristics

**Key Outputs**:
```
🎯 Testing FanOut Strategy (Default for Analytics)
[Unity Analytics] Tracking event 'app.startup'
[Firebase Analytics] Event: app.startup

🎯 Testing Sharded Strategy (Event Prefix Routing)
[Unity Analytics] Tracking event 'player.achievement.unlocked'  # Only Unity
[Firebase Analytics] Event: system.performance                  # Only Firebase
```

**How to Toggle FanOut vs Sharded for Analytics**:

```csharp
services.AddSelectionStrategies(options =>
{
    // Keep default FanOut for broadcast behavior
    options.SetCategoryDefault(ServiceCategory.Analytics, SelectionStrategyType.FanOut);
    
    // OR switch to Sharded for event prefix routing
    options.SetCategoryDefault(ServiceCategory.Analytics, SelectionStrategyType.Sharded);
});
```

---

### Resources.Demo.Console

**Purpose**: Demonstrates **PickOne** strategy with resilience patterns for deterministic resource loading with automatic fallback.

**Run Instructions**:
```bash
dotnet run --project samples/PintoBean.Resources.Demo.Console
```

**What it demonstrates**:

#### PickOne Strategy Behavior
- **Deterministic Selection**: Always selects highest priority provider available
- **Priority-based**: Critical > High > Normal > Low
- **Single Provider**: Only one provider handles each request

#### Resources Fallback via Resilience
The sample shows two types of fallback:

1. **Resilience-level Fallback** (within same provider):
   - Polly retry policies handle transient failures
   - Same provider retries multiple times
   - Configurable timeout and retry attempts

2. **Provider-level Fallback** (between providers):
   - Dynamic provider registration/unregistration
   - When primary provider becomes unavailable, PickOne selects next highest priority
   - Demonstrates graceful degradation

**Provider Priority Chain**:
1. **CacheStore** (Priority: Critical) - Fast in-memory cache
2. **NetworkStore** (Priority: High) - Network-based loading  
3. **LocalFileStore** (Priority: Normal) - Reliable local fallback

**Key Outputs**:
```
1️⃣ Normal operation: CacheStore (highest priority)
2️⃣ Transient failure: CacheStore retries via resilience
3️⃣ Provider fallback: Cache removed → NetworkStore selected
4️⃣ Deeper fallback: Network removed → LocalFileStore selected
5️⃣ Service recovery: Network restored → NetworkStore reselected
6️⃣ Total failure: All providers removed → graceful error
```

---

### SceneFlow.Demo.Console

**Purpose**: Shows **PickOne** strategy for deterministic scene management with policy-based provider selection.

**Run Instructions**:
```bash
dotnet run --project samples/PintoBean.SceneFlow.Demo.Console
```

**What it demonstrates**:
- **Deterministic Flow Control**: Same provider selected consistently
- **Policy-based Selection**: Development vs Production vs Performance policies
- **Consistent Behavior**: Multiple runs show same provider selection
- **Priority Respect**: Higher priority providers selected over lower ones

**Provider Policies**:
- **Development**: Pre+Post logging, metadata enabled (Priority: High)
- **Production**: Post-only logging, no metadata (Priority: Normal)  
- **Performance**: No logging, minimal overhead (Priority: Low)

---

### Hello.Demo.Console

**Purpose**: Basic platform introduction with OpenTelemetry and resilience integration.

**Run Instructions**:
```bash
# Interactive mode - will prompt for configuration choice
dotnet run --project samples/PintoBean.Hello.Demo.Console

# Or provide input automatically:
echo "2" | dotnet run --project samples/PintoBean.Hello.Demo.Console
```

**Configuration Options**:
1. **NoOp + PassThrough**: Minimal baseline configuration
2. **OpenTelemetry + Polly**: Full tracing and resilience (recommended)

---

## Strategy Architecture Overview

### Selection Strategy Types

Based on [RFC-0003](rfcs/rfc-0003-selection-strategies.md), PintoBean supports three core strategies:

#### PickOne (Default)
- **Behavior**: Selects single highest-priority provider
- **Use Cases**: Resources, SceneFlow, AI
- **Fallback**: Via provider registration/unregistration + resilience patterns
- **Deterministic**: Same input always selects same provider

#### FanOut
- **Behavior**: Sends to ALL registered providers
- **Use Cases**: Analytics (Unity + Firebase), logging, telemetry
- **Error Handling**: Continue (default), FailFast, or Custom aggregation
- **Async**: Parallel execution with result aggregation

#### Sharded
- **Behavior**: Routes by extracted key (e.g., event prefix)
- **Use Cases**: Analytics routing, load balancing
- **Key Extraction**: Analytics uses event name prefix before first dot
- **Explicit Mapping**: Support for explicit shard-to-provider mappings

### Default Category Mappings (RFC-0003)

| Category | Default Strategy | Reasoning |
|----------|------------------|-----------|
| **Analytics** | `FanOut` | Broadcast to Unity Analytics + Firebase |
| **Resources** | `PickOne` | Primary backend with resilience fallback |
| **SceneFlow** | `PickOne` | Deterministic flow control |
| **AI** | `PickOne` | Router decides backend |

### Configuration & Overrides

Override defaults in DI configuration:

```csharp
services.AddSelectionStrategies(options =>
{
    // Override category defaults
    options.SetCategoryDefault(ServiceCategory.Analytics, SelectionStrategyType.Sharded);
    
    // Override specific service contracts
    options.UseStrategyFor<IAnalyticsService>(SelectionStrategyType.PickOne);
    
    // Or use fluent helpers
}).UsePickOneFor<IResourceService>()
  .UseFanOutFor<IAnalyticsService>()
  .UseShardedFor<IAIService>();
```

## Next Steps

- **Architecture**: Review [RFC-0003](rfcs/rfc-0003-selection-strategies.md) for detailed strategy architecture
- **Platform Core**: Check [RFC-0001](rfcs/rfc-0001-service-platform-core.md) for seams and 4-tier architecture
- **Advanced Usage**: Explore [Selection Strategies Quickstart](selection-strategies-quickstart.md) for more configuration examples
- **All RFCs**: Browse complete documentation at [RFC Index](rfcs/)

## Troubleshooting

**Q: My strategy overrides aren't working**  
A: Ensure you're calling `AddSelectionStrategies()` before registering service implementations.

**Q: FanOut isn't aggregating results properly**  
A: Verify your service contracts return `Task<T>` or `ValueTask<T>` for proper aggregation.

**Q: Sharded routing seems random**  
A: Check your key extraction logic - default uses event name prefix before first dot.

**Q: Resources fallback not working**  
A: Remember that PickOne uses provider priority + resilience patterns. For true fallback, providers must be unregistered/reregistered or use resilience executor patterns.