# PintoBean Unity Sample

This is a Unity sample project that demonstrates integration with the Yokan PintoBean service platform's 4-tier architecture and Unity DI bridge.

## Overview

This sample provides:
- **Tier-1 (Abstractions)**: References `IHelloService` contract for service operations
- **Tier-2 (Generated Façades)**: `AnalyticsService` with `[RealizeService]` attribute (source generation placeholder)  
- **Tier-3 (Runtime)**: References to runtime services like `IServiceRegistry`, `IResilienceExecutor`, `IAspectRuntime`
- **Tier-4 (Unity Runtime)**: Unity DI bridge for Microsoft.Extensions.DependencyInjection integration
- **Providers**: References stub providers for Unity service integration

## Unity DI Bridge Features

The Unity DI bridge (`Yokan.PintoBean.Runtime.Unity`) provides:

### Core Components
- **UnityServiceProviderBridge**: Global singleton bridge between MS.DI and Unity
- **ServiceAwareMonoBehaviour**: Base class for MonoBehaviours that need DI services
- **IUnityLifetimeScopeFactory**: Factory interface for creating Unity lifetime scopes
- **DefaultUnityLifetimeScopeFactory**: Default implementation for Unity lifetime scope creation

### Usage Patterns

#### 1. Basic MonoBehaviour Integration
```csharp
public class MyService : ServiceAwareMonoBehaviour
{
    private IHelloService _helloService;

    void Start()
    {
        // Resolve services using the base class methods
        _helloService = GetService<IHelloService>();
        
        // Or handle gracefully if service might not exist
        var optionalService = GetServiceOrNull<IHelloService>();
    }
}
```

#### 2. Service Registration
```csharp
var services = new ServiceCollection();
services.AddServiceRegistry();
services.AddSelectionStrategies();
services.AddTransient<IHelloService, MyHelloService>();

// Add Unity bridge support
var factory = new DefaultUnityLifetimeScopeFactory();
services.AddUnityBridge(factory);

var serviceProvider = services.BuildServiceProvider();

// Initialize Unity bridge
factory.CreateLifetimeScope(serviceProvider);
```

#### 3. Manual Bridge Usage
```csharp
// Initialize the global bridge
UnityServiceProviderBridge.Initialize(serviceProvider);

// Resolve services anywhere in Unity code
var helloService = UnityServiceProviderBridge.Current.GetService<IHelloService>();
```

## Structure

- `Bootstrap.cs` - Unity MonoBehaviour that demonstrates calling `IAnalytics.Track()`
- `UnityDiBridgeSample.cs` - MonoBehaviour demonstrating Unity DI bridge usage with IHelloService
- `PintoBean.Unity.Sample.asmdef` - Unity assembly definition with tier references
- `README.md` - This documentation

## Compilation Status

This sample is **compile-only** and designed to verify that:
- Unity assembly definition references work correctly
- `IHelloService` resolution through Unity DI bridge works successfully  
- Tier-2/3/4 assemblies are properly referenced
- No Unity dependencies leak into Tier-1 or Tier-2

## Architecture Benefits

The Unity DI bridge provides:
- **Separation of Concerns**: Unity-specific code isolated in Tier-4
- **Testability**: Services can be tested independently of Unity
- **Flexibility**: Easy to swap service implementations
- **Performance**: Minimal overhead singleton bridge pattern
- **Safety**: Graceful handling of missing services

## Next Steps

Actual runtime integration with HybridCLR flow will be implemented in **P4-03**. The Unity DI bridge establishes the foundation for:
- Proper dependency injection wiring between Unity and MS.DI
- Service registry configuration with Unity MonoBehaviours
- Analytics provider registration in Unity environments
- Unity-specific cross-cutting concerns while maintaining architectural boundaries

## Build Requirements

- Unity 2022.3+ (for assembly definition support)
- .NET 8.0 SDK (for referenced assemblies compilation)

## Usage

This sample is designed to compile within Unity's assembly compilation system and is tested via the CI "Unity compile" job (when available).