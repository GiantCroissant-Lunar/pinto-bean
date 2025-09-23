---
id: RFC-0003
title: Selection Strategies (PickOne, FanOut, Sharded) & Category Defaults
status: Draft
created: 2025-09-23
updated: 2025-09-23
authors: [Ray Wang]
categories: [Architecture, Runtime Policy]
tags: [registry, selection, fanout, sharding, analytics, ai, resources, sceneflow]
supersedes: []
depends_on: [RFC-0001, RFC-0002]
impl_links: []
---

## Summary
Defines the standard selection strategies used by façades via the typed registry, plus sane **category defaults** and override points.

## Strategies
### PickOne (default)
Select one provider per call using: capability filter → platform filter → priority → tie-break (stable hash/round-robin). Cache by (contract, criteria) with TTL; invalidate on `ProviderChanged`.

### FanOut
Invoke *all* matched providers; aggregate results/failures:
- Fire-and-forget (for “telemetry-style” void ops) or
- Aggregate (for Task/ValueTask return types; failures folded with policy).

### Sharded
Route by key `Func<Request,TKey>`. Default for analytics: **event name prefix before the first dot** (e.g., `player.level.complete` → `player`). Overridable via policy assets.

## Category defaults (overridable in Settings/DI)
- **Analytics** → **FanOut** (common to send to Unity Analytics + Firebase), optional `Sharded` for fine routing.
- **Resources** → **PickOne** (primary backend, fallback via resilience).
- **SceneFlow** → **PickOne** (deterministic flow), with controlled fallback on failure.
- **AI** → **PickOne** (router decides backend), optional **FanOut** for evaluation runs.

## Configuration & Overrides
- Unity: ScriptableObject Strategy Mapping assets → into Registry at boot.
- .NET/Godot: appsettings.json → options → Registry.
- Per-call overrides via criteria object (optional).

## Testing Strategy
- Determinism tests (stable PickOne under equal inputs).
- FanOut aggregation semantics.
- Sharded key extraction & routing coverage.

## Security & Performance
- Avoid high-cardinality tags; cap sharding dimension vocabularies.
- FanOut guarded by circuit-breakers (via resilience).

## Open Questions
- Weighted scoring (latency/health) as a first-class strategy?
- Category-specific default TTLs for cache.

## References
- ServiceStrategy architecture doc
- Proxy-Registry separation notes
