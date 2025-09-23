---
id: RFC-0001
title: Service Platform Core (4-Tier, Reverse-Mode Façade)
status: Draft
created: 2025-09-23
updated: 2025-09-23
authors: [Ray Wang]
categories: [Architecture, Platform]
tags: [tier1, tier2, tier3, tier4, proxies, registry, di, cross-engine, hot-swap]
supersedes: []
depends_on: []
impl_links: []
---

## Summary
A cross-engine, service-oriented platform split into **Tier-1** (contracts), **Tier-2** (generated façade proxies), **Tier-3** (adapters: resilience, load contexts, telemetry, schedulers), and **Tier-4** (providers).  
**Reverse-mode façade**: Tier-2 `partial class Service` declares which contracts it realizes (`[RealizeService(typeof(IService))]`) and delegates to Tier-3 implementations via a **typed registry** with pluggable strategies (pick-one, fan-out, sharded). Tier-1 remains pure .NET (no engine refs).

## Background & Problem
We need engine-agnostic services (Unity, Godot, custom) with AOT-safe cross-cutting (tracing/metrics/resilience) and safe extension (plugins, hot-swap). Unity lacks unloadable ALC; .NET hosts/Godot support it.

## Proposal
### Tier responsibilities
- **Tier-1 (Contracts/Models)**: engine-free interfaces and DTOs only.
- **Tier-2 (Generated Façades)**: source-generated `partial class Service` for each realized contract; delegates to a typed registry + accepts cross-cutting adapters (`IResilienceExecutor`, telemetry runtime).
- **Tier-3 (Adapters)**: resilience (Polly via `IResilienceExecutor`), load-context (`ILoadContext`: ALC on .NET/Godot; HybridCLR soft-swap on Unity), telemetry, main-thread scheduler.
- **Tier-4 (Providers)**: engine/SDK integrations implementing Tier-3 abstractions.

### Reverse-mode façade & delegation
```csharp
// Tier 1
public interface IService { void Func1(); }

// Tier 2 (generated façade)
[RealizeService(typeof(IService))]
public partial class Service : IService
{
    public void Func1() => _registry.For<IService>().Invoke(s => s.Func1());
    private readonly IServiceRegistry _registry;
    public Service(IServiceRegistry registry, IResilienceExecutor rx, IAspectRuntime rt) { ... }
}

// Tier 3 (actual implementation stitched by DI/plugins)
public partial class Service { /* glue, composition, policies */ }
