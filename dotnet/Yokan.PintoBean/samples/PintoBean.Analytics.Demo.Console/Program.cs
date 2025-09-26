using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime;
using Yokan.PintoBean.Providers.Stub;
using System.Collections.Generic;
using System.Linq;

namespace PintoBean.Analytics.Demo.Console;

/// <summary>
/// Analytics demo program showing FanOut and Sharded strategy routing via a façade.
/// Demonstrates realistic analytics event tracking with Unity and Firebase providers.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        System.Console.WriteLine("=== PintoBean Analytics Demo ===\n");

        // Create host with DI container
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                // Add required runtime services FIRST
                services.AddResilienceExecutor();
                services.AddNoOpAspectRuntime();

                // Register service registry with provider configuration
                services.AddServiceRegistry(registry =>
                {
                    System.Console.WriteLine("📝 Registering Analytics providers...");
                    
                    // Register analytics providers for FanOut/Sharded routing
                    var unityProvider = new UnityAnalyticsProvider();
                    var firebaseProvider = new FirebaseAnalyticsProvider();

                    var unityCapabilities = ProviderCapabilities.Create("unity-analytics")
                        .WithPriority(Priority.Normal)
                        .WithTags("unity", "mobile", "analytics");

                    var firebaseCapabilities = ProviderCapabilities.Create("firebase-analytics")
                        .WithPriority(Priority.Normal)
                        .WithTags("firebase", "mobile", "analytics");

                    registry.Register<IAnalytics>(unityProvider, unityCapabilities);
                    registry.Register<IAnalytics>(firebaseProvider, firebaseCapabilities);

                    System.Console.WriteLine($"  ✅ Unity Analytics (ID: {unityCapabilities.ProviderId})");
                    System.Console.WriteLine($"  ✅ Firebase Analytics (ID: {firebaseCapabilities.ProviderId})");
                    System.Console.WriteLine($"  📊 Total registrations for IAnalytics: {registry.GetRegistrations<IAnalytics>().Count()}");
                });

                // Configure selection strategies AFTER registry setup
                services.AddSelectionStrategies();
                
                // Register the Analytics façade service
                services.AddSingleton<Analytics>();
                services.AddSingleton<IAnalytics>(provider => provider.GetRequiredService<Analytics>());
            })
            .Build();

        System.Console.WriteLine("\n🔍 Verifying Analytics service setup...");
        
        // Verify the registry has providers
        var registry = host.Services.GetRequiredService<IServiceRegistry>();
        var registrations = registry.GetRegistrations<IAnalytics>().ToList();
        System.Console.WriteLine($"Found {registrations.Count} Analytics providers in registry");
        
        foreach (var reg in registrations)
        {
            System.Console.WriteLine($"  - {reg.Capabilities.ProviderId} (Priority: {reg.Capabilities.Priority})");
        }

        if (registrations.Count == 0)
        {
            System.Console.WriteLine("❌ No providers found! Check registration process.");
            return;
        }

        // Check what strategy is being used for IAnalytics
        var strategyFactory = host.Services.GetRequiredService<ISelectionStrategyFactory>();
        var analyticsStrategy = strategyFactory.CreateStrategy<IAnalytics>();
        System.Console.WriteLine($"📊 Strategy for IAnalytics: {analyticsStrategy.StrategyType}");

        var analytics = host.Services.GetRequiredService<IAnalytics>();
        System.Console.WriteLine($"Analytics service type: {analytics.GetType().Name}");

        System.Console.WriteLine("\n🎯 Testing FanOut Strategy (Default for Analytics)");
        System.Console.WriteLine("Expected: Events sent to ALL providers (Unity + Firebase)\n");

        await DemonstrateFanOutStrategy(analytics);

        System.Console.WriteLine("\n🎯 Testing Sharded Strategy (Event Prefix Routing)");
        System.Console.WriteLine("Expected: Events routed to specific providers based on prefix\n");

        await DemonstrateShardedStrategy(host.Services);

        System.Console.WriteLine("\n✅ Analytics Demo completed successfully!");
    }

    private static async Task DemonstrateFanOutStrategy(IAnalytics analytics)
    {
        System.Console.WriteLine("--- FanOut Strategy Demo ---");

        // Track various events - all should go to both providers
        var events = new[]
        {
            new AnalyticsEvent 
            { 
                EventName = "app.startup",
                UserId = "user123",
                SessionId = "session456",
                Properties = new Dictionary<string, object> 
                { 
                    ["version"] = "1.2.0",
                    ["platform"] = "mobile"
                }
            },
            new AnalyticsEvent 
            { 
                EventName = "player.level.complete",
                UserId = "user123",
                SessionId = "session456",
                Properties = new Dictionary<string, object> 
                { 
                    ["level"] = 5,
                    ["score"] = 1250,
                    ["duration"] = 180
                }
            },
            new AnalyticsEvent 
            { 
                EventName = "system.error",
                Properties = new Dictionary<string, object> 
                { 
                    ["error_code"] = "E001",
                    ["severity"] = "high"
                }
            }
        };

        foreach (var evt in events)
        {
            System.Console.WriteLine($"📤 Tracking: {evt.EventName}");
            await analytics.Track(evt);
            System.Console.WriteLine();
        }
    }

    private static async Task DemonstrateShardedStrategy(IServiceProvider services)
    {
        System.Console.WriteLine("--- Sharded Strategy Demo ---");
        System.Console.WriteLine("Configuring Analytics to use Sharded strategy with event prefix routing\n");

        // Create a new service scope with sharded configuration
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        
        // Configure Analytics to use Sharded strategy instead of default FanOut
        var shardedServices = new ServiceCollection();
        shardedServices.AddResilienceExecutor();
        shardedServices.AddNoOpAspectRuntime();
        
        shardedServices.AddServiceRegistry(registry =>
        {
            // Register analytics providers with shard-specific configuration
            var unityProvider = new UnityAnalyticsProvider("unity-player-shard");
            var firebaseProvider = new FirebaseAnalyticsProvider("firebase-system-shard");

            // Unity handles player events, Firebase handles system events
            registry.Register<IAnalytics>(unityProvider, ProviderCapabilities.Create("unity-player-analytics"));
            registry.Register<IAnalytics>(firebaseProvider, ProviderCapabilities.Create("firebase-system-analytics"));
        });
        
        // Configure to use Sharded strategy specifically for IAnalytics
        shardedServices.AddSelectionStrategies(options =>
        {
            options.UseStrategyFor<IAnalytics>(SelectionStrategyType.Sharded);
        });
        
        shardedServices.AddSingleton<Analytics>();
        shardedServices.AddSingleton<IAnalytics>(provider => provider.GetRequiredService<Analytics>());
        
        var shardedProvider = shardedServices.BuildServiceProvider();
        var shardedAnalytics = shardedProvider.GetRequiredService<IAnalytics>();
        
        // Verify the strategy
        var shardedStrategyFactory = shardedProvider.GetRequiredService<ISelectionStrategyFactory>();
        var shardedStrategy = shardedStrategyFactory.CreateStrategy<IAnalytics>();
        System.Console.WriteLine($"📊 Strategy for IAnalytics: {shardedStrategy.StrategyType}");
        
        System.Console.WriteLine("\n🔀 Testing event prefix routing:");
        System.Console.WriteLine("- player.* events should route to Unity Analytics");
        System.Console.WriteLine("- system.* events should route to Firebase Analytics\n");

        var playerEvent = new AnalyticsEvent 
        { 
            EventName = "player.achievement.unlocked",
            UserId = "user123",
            Properties = new Dictionary<string, object> { ["achievement"] = "first_win" }
        };

        var systemEvent = new AnalyticsEvent 
        { 
            EventName = "system.performance",
            Properties = new Dictionary<string, object> { ["fps"] = 60, ["memory_mb"] = 512 }
        };

        System.Console.WriteLine("📤 Tracking player event (should route to Unity only):");
        await shardedAnalytics.Track(playerEvent);
        System.Console.WriteLine();

        System.Console.WriteLine("📤 Tracking system event (should route to Firebase only):");
        await shardedAnalytics.Track(systemEvent);
        System.Console.WriteLine();

        System.Console.WriteLine("✅ Sharded strategy routing completed!");
    }
}