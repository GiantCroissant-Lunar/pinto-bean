# Hybrid Pure.DI + Splat Composition Pattern

## Overview

ConsoleGame now uses a hybrid dependency injection approach that combines compile-time Pure.DI composition for plug-in modules with Splat-based runtime composition for cross-plugin discovery. The pattern delivers the following benefits:

- **Compile-time safety:** Each module exposes a Pure.DI root (e.g. `ConsoleGame.Plugin.Audio/AudioPureDiRoot.cs`) that validates its object graph during build.
- **Runtime flexibility:** The host still relies on Splat (`ConsoleGame.SplatSupport`) to publish services that need to be resolved dynamically across module boundaries or during plug-in load/unload events.
- **Scoped lifetimes:** Plug-ins such as `ConsoleGame.Dungeon.Plugin` now build and dispose Pure.DI-backed service providers per load cycle, ensuring deterministic cleanup when assemblies are reloaded.

## Module-Level Composition

Each plug-in maintains a Pure.DI root that is responsible for wiring its local services. The audio plug-in illustrates the pattern:

- `AudioPureDiRoot` declares the Pure.DI setup and exposes the factory `AudioPureDiAdapter.CreateAudioService(...)`.
- `AudioServiceCollectionExtensions.AddAudioSubsystem(...)` adds Microsoft.Extensions DI bindings so host applications and tests can reference the audio services using standard service providers.
- `AudioComposition` bridges the Pure.DI root into Splat via `AudioPureDiAdapter`, ensuring existing consumers of `AudioComposition.Resolve<T>()` continue to function.

## Runtime Composition Bridging

When a plug-in is loaded, the host composes module services as follows:

1. Build a scoped `IServiceProvider` using Microsoft.Extensions.DependencyInjection.
2. Register the plug-in's Pure.DI- backed services into what the plug-in expects (e.g., audio via `AudioPureDiAdapter`).
3. Publish any cross-plugin services into Splat (`ConsoleGame.SplatSupport`).

`ConsoleGame.Dungeon.Plugin/DungeonPlugin.cs` demonstrates this bridge. During `ConfigureAsync(...)` it:

- Creates or obtains the effective `IServiceProvider` for the session.
- Resolves plug-in dependencies (`IAudioService`, `ILogService`, UI components) through the scoped provider.
- Tracks disposable resources so they are released when the plug-in unloads.

The provider is disposed in both the `RunAsync(...)` completion block and `Dispose()` to cover normal shutdown and forced unload scenarios.

## Lifecycle Best Practices

- **Create per-load scopes:** Always build a fresh Pure.DI provider when the plug-in loads, and dispose it before the plug-in is unloaded to avoid leaking assemblies or handles.
- **Surface adapters:** For every Pure.DI root, provide a small adapter class (similar to `AudioPureDiAdapter`) so other modules can call it without depending directly on Pure.DI internals.
- **Expose Microsoft.Extensions DI extensions:** Continue to offer `IServiceCollection` extensions for the modules so tests and other hosts can consume the services without going through Splat.

## Adding a New Plug-in

1. Author the Pure.DI root (e.g., `FooPureDiRoot`) and adapter (`FooPureDiAdapter`).
2. Provide an `IServiceCollection` extension that binds module options, logging, and core services via the adapter.
3. Update the module's Splat composition (if required) to resolve through the new adapter rather than instantiating dependencies manually.
4. Ensure the plug-in host constructs and disposes a scoped provider when the plug-in is loaded/unloaded, mirroring `DungeonPlugin`.

## Testing the Pattern

- The `ConsoleGame.Plugin.Audio.PureDiProbe` project validates that Pure.DI source generation continues to work in isolation.
- Plug-in runtime smoke tests should exercise load/unload cycles to confirm the scoped providers are disposed and that `AudioComposition.Reset()` (or equivalent) releases tracked disposables.

## Build & CI Notes

- Pure.DI emits transient generated files under each project's `obj/Debug/net8.0/Pure.DI/...` folder. When configuring CI or build caches, ensure these artifacts are not stripped before compilation completes.
- The Pure.DI generator can trigger benign CS0436 warnings (`TT` alias collisions). They do not affect build output but can be suppressed if desired via `#pragma warning disable CS0436` around the generated inclusion (optional).

Keep this document updated as additional plug-ins adopt the hybrid pattern or if the bridging strategy evolves.
