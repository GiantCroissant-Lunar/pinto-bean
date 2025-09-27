# Unity Integration Guide

This guide explains how to integrate Yokan PintoBean into Unity projects.

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

The Unity integration follows the 4-tier architecture:

- **Tier-1 (Abstractions)**: Engine-agnostic contracts (`IHelloService`, `IAnalytics`, etc.)
- **Tier-2 (CodeGen)**: Source generators and analyzers (Editor-only, won't compile in builds)
- **Tier-3 (Runtime)**: Service registry and execution runtime
- **Tier-4 (Unity Adapters)**: Unity-specific DI bridge and MonoBehaviour base classes

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

## Requirements

- Unity 2022.3 or newer (for assembly definition support)
- Compatible with IL2CPP and Mono backends
- .NET Standard 2.1 compatibility

## Compilation Notes

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

## Support

For issues and questions:
- Check the [main documentation](dotnet/Yokan.PintoBean/README.md)
- Review the [RFCs](dotnet/Yokan.PintoBean/docs/rfcs/) for architecture details
- Open an issue in the [GitHub repository](https://github.com/GiantCroissant-Lunar/pinto-bean)