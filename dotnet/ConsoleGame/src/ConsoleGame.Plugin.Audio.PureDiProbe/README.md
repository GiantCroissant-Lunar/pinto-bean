# ConsoleGame.Plugin.Audio.PureDiProbe

Isolation probe project to test whether Pure.DI 2.2.12 can successfully generate code in this repository when only a single, trivial root is present.

Contents:
- `ProbeRoot.cs` defines a single root with one binding: `IAudioService -> LibVlcAudioService` (referencing Contracts project types if needed).

Expected outcome: `dotnet build` succeeds; generator emits backing implementation for `_audioService()`.
If it fails with duplicate members or partial method errors, issue is systemic (environment / generator bug) rather than multi-root collisions in original plugin.
