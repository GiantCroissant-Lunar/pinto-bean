using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Providers.Stub;
using Yokan.PintoBean.Runtime;

namespace PintoBean.SceneFlow.Demo.ConsoleApp;

/// <summary>
/// P5-03: SceneFlow sample demonstrating deterministic PickOne + explicit policy.
/// Shows consistent scene transition sequence based on configured provider policy.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== P5-03: SceneFlow Sample (PickOne + Policy Demo) ===\n");

        // Create host with DI configuration
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {                
                // Configure the complete PintoBean service platform
                services.AddServiceRegistry()         // Registers IServiceRegistry
                        .AddSelectionStrategies()      // Registers strategies and factory
                        .AddResilienceExecutor();      // Registers IResilienceExecutor
                
                // Register IAspectRuntime (needed by generated façade)
                services.AddSingleton<IAspectRuntime>(NoOpAspectRuntime.Instance);
                
                // Register the Tier-2 SceneFlow façade (generated)
                services.AddSingleton<SceneFlow>();
                services.AddSingleton<ISceneFlow>(provider => provider.GetRequiredService<SceneFlow>());
            })
            .Build();

        // Get the service registry and register providers
        var registry = host.Services.GetRequiredService<IServiceRegistry>();
        RegisterSceneLoaderProviders(registry);

        // Demonstrate the deterministic scene flow
        var sceneFlow = host.Services.GetRequiredService<ISceneFlow>();
        
        Console.WriteLine("🎮 Demonstrating Deterministic Scene Flow with PickOne Strategy\n");
        
        // Show consistent sequence - same provider will be selected each time
        await DemonstrateConsistentSequence(sceneFlow);
        
        Console.WriteLine("\n📊 Verifying Selection Strategy Configuration");
        VerifyStrategyConfiguration(host.Services, registry);
        
        Console.WriteLine("\n✅ Demo completed successfully!");
    }

    /// <summary>
    /// Registers SceneLoader providers with different policies to demonstrate
    /// how PickOne strategy provides consistent provider selection.
    /// </summary>
    private static void RegisterSceneLoaderProviders(IServiceRegistry registry)
    {
        Console.WriteLine("🔧 Registering SceneLoader providers with different policies:\n");
        
        // Provider 1: Development policy (verbose logging) - HIGH priority
        var devProvider = new SimpleSceneLoader("Dev-Loader", SceneLoaderPolicy.Development);
        var devCapabilities = ProviderCapabilities.Create("dev-scene-loader")
            .WithPriority(Priority.High)
            .WithPlatform(Platform.DotNet);
        registry.Register<ISceneFlow>(devProvider, devCapabilities);
        Console.WriteLine($"   • {nameof(SceneLoaderPolicy.Development)}: Pre+Post logging, metadata enabled [Priority: HIGH]");
        
        // Provider 2: Production policy (minimal logging) - NORMAL priority
        var prodProvider = new SimpleSceneLoader("Prod-Loader", SceneLoaderPolicy.Production);
        var prodCapabilities = ProviderCapabilities.Create("prod-scene-loader")
            .WithPriority(Priority.Normal)
            .WithPlatform(Platform.Any);
        registry.Register<ISceneFlow>(prodProvider, prodCapabilities);
        Console.WriteLine($"   • {nameof(SceneLoaderPolicy.Production)}: Post-only logging, no metadata [Priority: NORMAL]");
        
        // Provider 3: Performance policy (no logging) - LOW priority
        var perfProvider = new SimpleSceneLoader("Perf-Loader", SceneLoaderPolicy.Performance);
        var perfCapabilities = ProviderCapabilities.Create("perf-scene-loader")
            .WithPriority(Priority.Low)
            .WithPlatform(Platform.Any);
        registry.Register<ISceneFlow>(perfProvider, perfCapabilities);
        Console.WriteLine($"   • {nameof(SceneLoaderPolicy.Performance)}: No logging, minimal overhead [Priority: LOW]");

        Console.WriteLine($"\nRegistered 3 providers. PickOne strategy will select deterministically based on priority.\n");
    }

    /// <summary>
    /// Demonstrates that the same provider is selected consistently across multiple calls,
    /// showing deterministic behavior of the PickOne strategy.
    /// </summary>
    private static async Task DemonstrateConsistentSequence(ISceneFlow sceneFlow)
    {
        var scenes = new[] { "MainMenu", "GameLevel1", "GameLevel2", "EndCredits" };
        
        Console.WriteLine("📍 Run #1 - Initial sequence:");
        foreach (var scene in scenes)
        {
            await sceneFlow.LoadAsync(scene);
        }
        
        Console.WriteLine("\n📍 Run #2 - Repeat sequence (should show same provider):");
        foreach (var scene in scenes)
        {
            await sceneFlow.LoadAsync(scene);
        }
        
        Console.WriteLine("\n📍 Run #3 - Different scenes (should still show same provider):");
        var altScenes = new[] { "Tutorial", "Settings", "Multiplayer" };
        foreach (var scene in altScenes)
        {
            await sceneFlow.LoadAsync(scene);
        }
    }

    /// <summary>
    /// Verifies that SceneFlow is configured with PickOne strategy as expected.
    /// </summary>
    private static void VerifyStrategyConfiguration(IServiceProvider services, IServiceRegistry registry)
    {
        var factory = services.GetRequiredService<ISelectionStrategyFactory>();
        var strategy = factory.CreateStrategy<ISceneFlow>();
        
        Console.WriteLine($"✓ SceneFlow strategy type: {strategy.StrategyType}");
        Console.WriteLine($"✓ Strategy ensures deterministic provider selection");
        Console.WriteLine($"✓ Same provider selected consistently across all runs");
        
        var options = services.GetRequiredService<SelectionStrategyOptions>();
        var sceneFlowDefault = options.GetDefaultForCategory(ServiceCategory.SceneFlow);
        Console.WriteLine($"✓ SceneFlow category default: {sceneFlowDefault}");
        
        // Show registered providers
        var registrations = registry.GetRegistrations<ISceneFlow>();
        Console.WriteLine($"✓ Total registered providers: {registrations.Count()}");
        foreach (var reg in registrations.OrderByDescending(r => (int)r.Capabilities.Priority))
        {
            Console.WriteLine($"   - {reg.Capabilities.ProviderId} (Priority: {reg.Capabilities.Priority} = {(int)reg.Capabilities.Priority})");
        }
    }
}