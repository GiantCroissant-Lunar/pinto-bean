# Unity Integration Guide

This guide provides end-to-end documentation for integrating Yokan PintoBean into Unity projects, covering installation, DI bridge architecture, strategy assets, profiles, and main-thread scheduling.

## Table of Contents

- [Installation Options](#installation-options)
- [Assembly Architecture](#assembly-architecture) 
- [DI Bridge: VContainer + Microsoft.Extensions.DependencyInjection](#di-bridge-vcontainer--microsoftextensionsdependencyinjection)
- [Strategy Assets (FanOut/Sharded) and Profiles (Play vs Editor)](#strategy-assets-fanoutsharded-and-profiles-play-vs-editor)
- [Main-Thread Scheduler Usage](#main-thread-scheduler-usage)
- [Basic Usage](#basic-usage)
- [Unity Logging & Telemetry](#unity-logging--telemetry)
- [Requirements & Compilation](#requirements--compilation)
- [Samples](#samples)
- [Troubleshooting](#troubleshooting)
- [Architecture References](#architecture-references)

## Installation Options

### Option 1: UPM Git URL (Recommended)

Unity Package Manager can install directly from this repository:

1. Open Unity Package Manager (Window > Package Manager)
2. Click the "+" button and select "Add package from git URL"
3. Enter: `https://github.com/GiantCroissant-Lunar/pinto-bean.git?path=Packages/com.giantcroissant.yokan`
4. Click "Add"

### Option 2: Copy Package Folder

For offline usage or modifications:

1. Clone or download this repository
2. Copy the `Packages/com.giantcroissant.yokan` folder to your Unity project's `Packages` directory
3. Unity will automatically detect and import the package

### Option 3: Manual Assembly References

If you prefer manual control:

1. Copy the relevant source files from `dotnet/Yokan.PintoBean/src/` to your Unity project
2. Ensure the `.asmdef` files are included:
   - `Yokan.PintoBean.Abstractions.asmdef`
   - `Yokan.PintoBean.Runtime.asmdef` 
   - `Yokan.PintoBean.Runtime.Unity.asmdef`
   - `Yokan.PintoBean.Providers.Stub.asmdef`
   - `Yokan.PintoBean.CodeGen.asmdef` (Editor-only)

## Assembly Architecture

The Unity integration follows the 4-tier architecture defined in [RFC-0001](docs/rfcs/rfc-0001-service-platform-core.md):

- **Tier-1 (Abstractions)**: Engine-agnostic contracts (`IHelloService`, `IAnalytics`, etc.)
- **Tier-2 (CodeGen)**: Source generators and analyzers (Editor-only, won't compile in builds)
- **Tier-3 (Runtime)**: Service registry and execution runtime with selection strategies
- **Tier-4 (Unity Adapters)**: Unity-specific DI bridge and MonoBehaviour base classes

### Assembly Definition Files

The Unity package includes these `.asmdef` files with proper dependencies:

- `Yokan.PintoBean.Abstractions.asmdef` - Tier-1 contracts (no dependencies)
- `Yokan.PintoBean.Runtime.asmdef` - Tier-3 runtime (depends on Abstractions)
- `Yokan.PintoBean.Runtime.Unity.asmdef` - Tier-4 Unity bridge (depends on Runtime)
- `Yokan.PintoBean.Providers.Stub.asmdef` - Sample providers (depends on Abstractions)
- `Yokan.PintoBean.CodeGen.asmdef` - Editor-only source generators (no runtime dependencies)

## DI Bridge: VContainer + Microsoft.Extensions.DependencyInjection

The Unity integration provides a bridge between Microsoft.Extensions.DependencyInjection and Unity environments through the `UnityServiceProviderBridge` class.

### Bridge Architecture

```csharp
// Core bridge initialization
var services = new ServiceCollection();
services.AddServiceRegistry();
services.AddSelectionStrategies();

// Unity-specific bridge setup
var factory = new DefaultUnityLifetimeScopeFactory();
services.AddUnityBridge(factory);

var serviceProvider = services.BuildServiceProvider();
UnityServiceProviderBridge.Initialize(serviceProvider);
```

### UnityServiceProviderBridge

The bridge provides a singleton pattern for accessing MS.DI services from Unity:

```csharp
public sealed class UnityServiceProviderBridge
{
    // Static access to current bridge instance
    public static UnityServiceProviderBridge Current { get; }
    
    // Service resolution methods
    public T GetService<T>() where T : notnull
    public T? GetServiceOrNull<T>() where T : class
    public object GetRequiredService(Type serviceType)
    
    // Unity scheduler integration
    public IUnityScheduler? Scheduler { get; }
}
```

### ServiceAwareMonoBehaviour

Base class for Unity MonoBehaviours that need DI services:

```csharp
public abstract class ServiceAwareMonoBehaviour // In Unity: : MonoBehaviour
{
    // Convenient service resolution
    protected T GetService<T>() where T : notnull
    protected T? GetServiceOrNull<T>() where T : class
    protected object GetRequiredService(Type serviceType)
}
```

### VContainer Integration

While the bridge works independently, it can integrate with VContainer for advanced lifetime scoping:

```csharp
// With VContainer
services.AddUnityBridge(provider => 
    new VContainerUnityLifetimeScopeFactory(containerBuilder));
```

## Strategy Assets (FanOut/Sharded) and Profiles (Play vs Editor)

Unity projects can configure selection strategies through ScriptableObject assets and profiles for different runtime modes.

### Selection Strategies Overview

Based on [RFC-0003](docs/rfcs/rfc-0003-selection-strategies.md), the platform supports three selection strategies:

- **PickOne**: Select single provider (default for most services)
- **FanOut**: Broadcast to all providers (default for Analytics)  
- **Sharded**: Route by key extraction (advanced Analytics scenarios)

### Strategy Configuration Assets

#### Creating Strategy Assets

Use Unity's menu system:

1. **PintoBean → Create Default Strategy Config** - Creates default configuration
2. **Assets → Create → PintoBean → Strategy Mapping** - Create custom strategy mapping
3. **Assets → Create → PintoBean → Shard Map** - Create shard routing configuration

#### StrategyMappingAsset Configuration

```csharp
// Example configuration via Unity Inspector:
Category Mappings:
- Analytics: FanOut      // Send to multiple analytics providers
- Resources: PickOne     // Primary with fallback pattern  
- SceneFlow: PickOne     // Deterministic scene transitions
- AI: PickOne           // Single AI provider selection

Contract-Specific Overrides:
- "MyGame.IPlayerStatsService": PickOne
- "MyGame.IDebugAnalyticsService": FanOut
```

#### ShardMapAsset Configuration

For Analytics services using Sharded strategy:

```csharp
// Explicit shard mappings in Unity Inspector:
Shard Mappings:
- "player.*": "UnityAnalyticsProvider"     // player.level.complete
- "system.*": "FirebaseAnalyticsProvider"  // system.startup
- "debug.*": "LocalAnalyticsProvider"      // debug.test.event

// Unmapped keys use consistent hashing
```

### Unity Profiles: Play vs Editor

The platform supports different configurations for Unity's runtime modes through profile assets.

#### Profile Types

**GameProfileAsset** (Play Mode + Builds):
- Higher timeouts (30s for network operations)
- More retry attempts (3 retries) 
- FanOut analytics for production telemetry
- Lower sampling rates (10%) for performance

**EditorProfileAsset** (Edit Mode in Unity Editor):
- Shorter timeouts (5s for quick iteration)
- Fewer retries (1 retry for fast failure)
- PickOne strategies for predictable testing
- Higher sampling rates (100%) for diagnostics

#### Creating Profile Assets

```csharp
// Via Unity menus:
// Assets → Create → PintoBean → Game Profile
// Assets → Create → PintoBean → Editor Profile

// Or via main menu:
// PintoBean → Create Game Profile Asset
// PintoBean → Create Editor Profile Asset
```

#### Profile Configuration Component

```csharp
public class ServiceBootstrap : MonoBehaviour
{
    [SerializeField] private GameProfileAsset gameProfile;
    [SerializeField] private EditorProfileAsset editorProfile;
    [SerializeField] private bool verboseLogging = true;
    
    void Awake()
    {
        var services = new ServiceCollection();
        
        // Auto-detect Unity mode and apply appropriate profile
        var profile = Application.isEditor && !Application.isPlaying 
            ? editorProfile : gameProfile;
            
        if (profile != null)
        {
            profile.ApplyToServices(services);
        }
        
        // Continue with service registration...
    }
}
```

#### Profile Settings Comparison

| Setting | Game Profile | Editor Profile | Reason |
|---------|-------------|----------------|---------|
| Analytics Strategy | FanOut | PickOne | Production needs multiple backends vs predictable testing |
| Network Timeout | 30s | 5s | Production resilience vs fast development iteration |
| Retry Attempts | 3 | 1 | Production stability vs quick failure detection |
| Sampling Rate | 10% | 100% | Performance optimization vs full diagnostics |

### StrategyConfigBootstrap Component

Automatically imports strategy configuration at runtime:

```csharp
[SerializeField] private bool forceReimport = false;
[SerializeField] private bool verboseLogging = true;

// Component automatically imports strategy assets on Start()
// Looks for assets in Resources folders:
// - Assets/Config/PintoBean/
// - Assets/Resources/
// - Project-specific Resources subfolders
```

## Main-Thread Scheduler Usage

Unity requires certain operations to run on the main thread. The platform provides `IUnityScheduler` for thread-safe operation posting.

### IUnityScheduler Interface

```csharp
public interface IUnityScheduler
{
    // Post synchronous action to main thread
    void Post(Action action);
    
    // Post asynchronous operation to main thread  
    Task PostAsync(Func<Task> operation);
    Task<T> PostAsync<T>(Func<Task<T>> operation);
    
    // Post with result handling
    Task<T> PostAsync<T>(Func<T> operation);
}
```

### UnitySchedulerProcessor

MonoBehaviour component that processes the scheduler queue:

```csharp
public class GameManager : MonoBehaviour
{
    private IUnityScheduler _scheduler;
    private UnitySchedulerProcessor _processor;
    
    void Start()
    {
        _scheduler = UnityServiceProviderBridge.Current.GetService<IUnityScheduler>();
        _processor = new UnitySchedulerProcessor(_scheduler);
    }
    
    void Update()
    {
        // Process scheduled operations on main thread
        _processor.ProcessQueue();
    }
    
    void OnDestroy()
    {
        _processor?.Cleanup();
    }
}
```

### Scheduler Usage Examples

```csharp
public class BackgroundServiceIntegration : ServiceAwareMonoBehaviour
{
    private IUnityScheduler _scheduler;
    
    void Start()
    {
        _scheduler = GetService<IUnityScheduler>();
        
        // Example: Background thread posting to main thread
        Task.Run(async () =>
        {
            // Background work
            var data = await FetchDataFromNetwork();
            
            // Post UI update to main thread
            await _scheduler.PostAsync(() =>
            {
                Debug.Log($"Data received: {data}");
                // Update Unity UI components safely
                UpdateGameUI(data);
            });
        });
    }
    
    private async Task DemoSchedulerPatterns()
    {
        // Pattern 1: Fire-and-forget action
        _scheduler.Post(() => 
        {
            Debug.Log("Main thread operation");
        });
        
        // Pattern 2: Async operation with result
        var result = await _scheduler.PostAsync(async () =>
        {
            // Async work on main thread
            await SomeUnityAsyncOperation();
            return "completed";
        });
        
        // Pattern 3: Synchronous operation with result
        var syncResult = await _scheduler.PostAsync(() =>
        {
            return SystemInfo.deviceModel;
        });
    }
}
```

### Thread-Safe Service Access

When using services from background threads, always use the scheduler for Unity-specific operations:

```csharp
public class AnalyticsService : IAnalyticsService
{
    private readonly IUnityScheduler _scheduler;
    
    public async Task TrackEventAsync(AnalyticsEvent analyticsEvent)
    {
        // Network call can happen on background thread
        var processed = await ProcessEventOnBackground(analyticsEvent);
        
        // Unity-specific logging must happen on main thread
        await _scheduler.PostAsync(() =>
        {
            Debug.Log($"Analytics event tracked: {processed.EventName}");
        });
    }
}

## Basic Usage

### Service-Aware MonoBehaviour

```csharp
using UnityEngine;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime.Unity;

public class GameManager : ServiceAwareMonoBehaviour
{
    private IHelloService _helloService;
    private IAnalytics _analytics;

    void Start()
    {
        // Resolve required services
        _helloService = GetService<IHelloService>();
        
        // Resolve optional services safely
        _analytics = GetServiceOrNull<IAnalytics>();
        
        if (_analytics != null)
        {
            // Track game start event
            _analytics.Track(new AnalyticsEvent 
            {
                EventName = "game.started",
                UserId = SystemInfo.deviceUniqueIdentifier,
                Properties = new Dictionary<string, object>
                {
                    ["platform"] = Application.platform.ToString(),
                    ["unity_version"] = Application.unityVersion
                }
            });
        }
    }
}
```

### Service Registration Bootstrap

```csharp
using UnityEngine;
using Microsoft.Extensions.DependencyInjection;
using Yokan.PintoBean.Runtime;
using Yokan.PintoBean.Runtime.Unity;

public class ServiceBootstrap : MonoBehaviour
{
    void Awake()
    {
        var services = new ServiceCollection();
        
        // Core service registry
        services.AddServiceRegistry();
        services.AddSelectionStrategies();
        
        // Runtime seams (choose your level)
        services.AddNoOpAspectRuntime();           // Minimal overhead
        services.AddResilienceExecutor();          // Basic resilience
        
        // Register your services
        services.AddTransient<IHelloService, MyHelloService>();
        
        // Unity bridge setup
        var factory = new DefaultUnityLifetimeScopeFactory();
        services.AddUnityBridge(factory);
        
        // Build and initialize
        var serviceProvider = services.BuildServiceProvider();
        factory.CreateLifetimeScope(serviceProvider);
    }
}
```

## Unity Logging & Telemetry

PintoBean provides Unity-specific aspect runtime implementations for logging and telemetry collection:

### Unity Aspect Runtime

The `UnityAspectRuntime` logs service calls to Unity's Debug console with minimal overhead:

```csharp
// Manual configuration
services.AddUnityAspectRuntime(enableMetrics: true, verboseLogging: false);

// Output in Unity Console:
// [PintoBean] ✓ IHelloService.SayHelloAsync completed in 45.20ms → String
// [PintoBean] 📊 user.actions: 1.00 [category=gameplay, level=tutorial]
```

### Profile-Based Configuration

Use Unity ScriptableObject profiles to configure aspect runtime per mode:

#### Game Profile (Play Mode)
```csharp
// Create via: Assets > Create > PintoBean > Game Profile
// Default: Unity aspect runtime for production logging
```

#### Editor Profile (Editor Mode)  
```csharp
// Create via: Assets > Create > PintoBean > Editor Profile
// Default: Adaptive runtime (OpenTelemetry in Editor, Unity in builds)
```

#### Applying Profile Configuration
```csharp
public class ServiceBootstrap : MonoBehaviour
{
    [SerializeField] private GameProfileAsset gameProfile;
    [SerializeField] private EditorProfileAsset editorProfile;
    
    void Awake()
    {
        var services = new ServiceCollection();
        
        // Apply aspect runtime based on profile
        var profile = Application.isEditor && !Application.isPlaying 
            ? editorProfile : gameProfile;
            
        if (profile != null)
        {
            profile.ApplyAspectRuntimeToServices(services);
        }
        else
        {
            // Fallback to Unity runtime
            services.AddUnityAspectRuntime();
        }
        
        // ... rest of service configuration
    }
}
```

### Aspect Runtime Types

- **NoOp**: No telemetry collection (minimal overhead)
- **Unity**: Unity Debug.* logging with low-cardinality tags  
- **OpenTelemetry**: Full OpenTelemetry tracing and metrics
- **Adaptive**: Auto-switches between Unity and OpenTelemetry based on environment

### Usage Examples

```csharp
// The aspect runtime automatically captures service calls when using the service registry:
var helloRegistry = serviceProvider.GetRequiredService<IServiceRegistry<IHelloService>>();

// This call will be logged by the configured aspect runtime
helloRegistry.Invoke(service => service.SayHelloAsync("World"));

// Manual aspect runtime usage (for custom operations)
var aspectRuntime = serviceProvider.GetRequiredService<IAspectRuntime>();
using var operation = aspectRuntime.StartOperation("custom-work");
// ... do work ...
aspectRuntime.RecordMetric("work.completed", 1.0, ("type", "background"));
```

## Requirements & Compilation

### Requirements

- Unity 2022.3 or newer (for assembly definition support)
- Compatible with IL2CPP and Mono backends
- .NET Standard 2.1 compatibility

### Compilation Notes

- **CodeGen assembly** is Editor-only and won't be included in builds
- **Runtime assemblies** work in both Editor and builds
- All assemblies are compatible with Unity's assembly compilation system
- No native dependencies required

## Samples

Check out the complete samples in:
- `Packages/com.giantcroissant.yokan/Samples~/Unity/` (UPM package)
- `dotnet/Yokan.PintoBean/samples/PintoBean.Unity.Sample/` (source repository)

## Troubleshooting

### Assembly Reference Issues
- Ensure all `.asmdef` files are present in your project
- Check that assembly references match the tier hierarchy
- Verify Unity's Assembly Definition settings

### Missing Services at Runtime  
- Ensure service registration occurs before first service resolution
- Use `GetServiceOrNull<T>()` for optional services
- Check that the Unity bridge is properly initialized

### Build Errors
- CodeGen assembly should be Editor-only (not included in builds)
- Verify IL2CPP compatibility settings if using IL2CPP backend

## Architecture References

This Unity integration guide builds upon several foundational documents:

### Core Architecture
- **[RFC-0001: Service Platform Core](docs/rfcs/rfc-0001-service-platform-core.md)** - 4-tier architecture and reverse-mode façade design
- **[RFC-0003: Selection Strategies](docs/rfcs/rfc-0003-selection-strategies.md)** - PickOne, FanOut, and Sharded strategy implementations

### Operational Guides  
- **[Hot-Swap Playbook](docs/hot-swap-playbook.md)** - Runtime plugin replacement in Unity environments (P4 soft-swap pattern)
- **[Selection Strategies Quickstart](docs/selection-strategies-quickstart.md)** - Practical configuration examples
- **[Unity Strategy Configuration Guide](docs/unity-strategy-configuration.md)** - Detailed ScriptableObject asset configuration

### Additional Documentation
- **[Unity Profiles Guide](Packages/com.giantcroissant.yokan/Documentation~/unity-profiles.md)** - Game vs Editor profile system
- **[Samples Quickstart](docs/samples-quickstart.md)** - Example implementations and usage patterns

## Support

For issues and questions:
- Check the [main documentation](dotnet/Yokan.PintoBean/README.md)
- Review the [RFCs](docs/rfcs/index.md) for architecture details
- Open an issue in the [GitHub repository](https://github.com/GiantCroissant-Lunar/pinto-bean)