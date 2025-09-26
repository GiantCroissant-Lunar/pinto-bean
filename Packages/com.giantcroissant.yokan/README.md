# Yokan PintoBean for Unity

Cross-engine service platform with 4-tier architecture for Unity, Godot, and custom .NET applications.

## Installation

### Option 1: UPM Git URL Installation

1. Open Unity Package Manager (Window > Package Manager)
2. Click the "+" button and select "Add package from git URL"
3. Enter: `https://github.com/GiantCroissant-Lunar/pinto-bean.git?path=Packages/com.giantcroissant.yokan`
4. Click "Add"

### Option 2: Copy Package Folder

1. Clone or download this repository
2. Copy the `Packages/com.giantcroissant.yokan` folder to your Unity project's `Packages` directory
3. Unity will automatically detect and import the package

## Assembly Structure

This package provides the following assemblies:

- **Yokan.PintoBean.Abstractions** (Tier-1): Core contracts and interfaces
- **Yokan.PintoBean.Runtime** (Tier-3): Service registry and runtime adapters  
- **Yokan.PintoBean.Runtime.Unity** (Tier-4): Unity-specific DI bridge and adapters
- **Yokan.PintoBean.Providers.Stub** (Tier-4): Example provider implementations
- **Yokan.PintoBean.CodeGen** (Editor-only): Source generators and analyzers

## Requirements

- Unity 2022.3 or newer
- .NET 8.0 compatible runtime (for referenced assemblies)

## Quick Start

```csharp
using UnityEngine;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime.Unity;

public class MyMonoBehaviour : ServiceAwareMonoBehaviour
{
    void Start()
    {
        // Resolve services using the base class methods
        var helloService = GetService<IHelloService>();
        
        // Or handle gracefully if service might not exist
        var optionalService = GetServiceOrNull<IHelloService>();
    }
}
```

## Sample

Check the Unity sample in `Samples~/Unity` for a complete integration example demonstrating:
- Unity DI bridge usage
- Service registration patterns  
- Analytics integration
- Cross-cutting concerns

## Documentation

For detailed architecture documentation, see the [main repository documentation](https://github.com/GiantCroissant-Lunar/pinto-bean/tree/main/dotnet/Yokan.PintoBean/docs).

## License

MIT License - see the main repository for details.