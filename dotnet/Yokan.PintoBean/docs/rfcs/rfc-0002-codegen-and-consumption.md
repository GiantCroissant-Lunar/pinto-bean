---
id: RFC-0002
title: Code Generation & Consumption (Façades, Registry, Packaging)
status: Draft
created: 2025-09-23
updated: 2025-09-23
authors: [Ray Wang]
categories: [Tooling, Codegen]
tags: [source-generators, analyzers, aot, proxies, registry, unity, godot]
supersedes: []
depends_on: [RFC-0001]
impl_links: []
---

## Summary
Defines the source-generator attributes, outputs, diagnostics, and Unity/Godot/.NET consumption. Emphasizes reverse-mode façade, typed registry generation, and analyzer guardrails.

## Attribute surface
- `RealizeServiceAttribute(params Type[] contracts)` — **Tier-2/Tier-3/Tier-4 only**; **error** if seen in Tier-1.
- `GenerateRegistryAttribute(Type contract)` — emits `{Contract}SelectionMode`, `{Contract}Strategy`, `{Contract}Registry` with typed helpers.
- Optional: `[Quiesce(Seconds = 5)]` on providers (defaults can be overridden per provider).

## Generator outputs
- **Façade partials** implementing realized contracts; each method delegates to typed registry and wraps cross-cutting via injected adapters.
- **Registry scaffolding**: enums, strategy interfaces, helpers, and DI extensions to register providers + select strategies (PickOne, FanOut, Sharded stubs emitted when enabled).

## Diagnostics (Analyzer)
- **SG0001**: `RealizeServiceAttribute` not allowed in Tier-1.
- **SG0002**: Realizing zero contracts (likely misconfiguration).
- **SG0003**: Missing `GenerateRegistryAttribute` for a realized contract.
- **SG0004**: Façade method signature mismatch with contract.
- **SG0005**: Multi-contract façade spans categories → warn (encourage cohesion).

## Packaging & Consumption
- Unity/Godot consume generators as **Analyzers** packages; CI must run the dotnet build for Tier-2 before Unity compilation.
- Outputs are AOT-safe C#; no runtime emit.
- Generator toggles via MSBuild props: `EnableFanOut`, `EnableSharded`, etc.

## Testing Strategy
- Golden-file tests for generated façades & registries.
- Analyzer tests for diagnostics.

## Security & Performance
- Idempotent outputs; incremental generator pipeline.
- No embedded secrets; configs via appsettings/ScriptableObjects.

## Open Questions
- Default analyzer category boundaries (what defines “category” for SG0005).
- Additional policies (e.g., WeightedRandom, Health-Weighted).

## References
- GenerateRegistry attribute source
- Generator consumption guide
