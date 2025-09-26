# PintoBean Unity Sample

This is a Unity sample project shell that demonstrates compile-time integration with the Yokan PintoBean service platform's 4-tier architecture.

## Overview

This sample provides:
- **Tier-1 (Abstractions)**: References `IAnalytics` contract for analytics tracking
- **Tier-2 (Generated Façades)**: `AnalyticsService` with `[RealizeService]` attribute (source generation placeholder)
- **Tier-3 (Runtime)**: References to runtime services like `IServiceRegistry`, `IResilienceExecutor`, `IAspectRuntime`
- **Tier-4 (Providers)**: References stub providers for Unity Analytics integration

## Structure

- `Bootstrap.cs` - Unity MonoBehaviour that demonstrates calling `IAnalytics.Track()`
- `PintoBean.Unity.Sample.asmdef` - Unity assembly definition with tier references
- `README.md` - This documentation

## Compilation Status

This sample is **compile-only** and designed to verify that:
- Unity assembly definition references work correctly
- `IAnalytics.Track()` calls compile successfully  
- Tier-2/3/4 assemblies are properly referenced

## Next Steps

Actual runtime integration with HybridCLR flow will be implemented in **P4-03**. This stub establishes the foundation for:
- Proper dependency injection wiring
- Service registry configuration
- Analytics provider registration
- Unity-specific cross-cutting concerns

## Build Requirements

- Unity 2022.3+ (for assembly definition support)
- .NET 8.0 SDK (for referenced assemblies compilation)

## Usage

This sample is designed to compile within Unity's assembly compilation system and is tested via the CI "Unity compile" job (when available).