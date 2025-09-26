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
                // Configure selection strategies with Analytics defaults
                services.AddSelectionStrategies();
                
                // Register the Analytics façade service
                services.AddSingleton<Analytics>();
                services.AddSingleton<IAnalytics>(provider => provider.GetRequiredService<Analytics>());

                // Add required runtime services
                services.AddResilienceExecutor();
                services.AddNoOpAspectRuntime();

                // Register service registry with provider configuration
                services.AddServiceRegistry(registry =>
                {
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

                    System.Console.WriteLine("📝 Registered Analytics providers:");
                    System.Console.WriteLine($"  ✅ Unity Analytics (ID: {unityCapabilities.ProviderId})");
                    System.Console.WriteLine($"  ✅ Firebase Analytics (ID: {firebaseCapabilities.ProviderId})");
                    System.Console.WriteLine($"  📊 Total registrations for IAnalytics: {registry.GetRegistrations<IAnalytics>().Count()}");
                });
            })
            .Build();

        var analytics = host.Services.GetRequiredService<IAnalytics>();

        System.Console.WriteLine("🎯 Testing FanOut Strategy (Default for Analytics)");
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
        System.Console.WriteLine("Note: This requires configuring the registry to use Sharded strategy");
        System.Console.WriteLine("with event prefix routing (\"player.*\" vs \"system.*\")\n");

        // For the sharded demo, we'll need to reconfigure the service with sharded strategy
        // This demonstrates how the same façade can work with different strategies
        
        var registry = services.GetRequiredService<IServiceRegistry>();
        
        // Clear existing providers and re-register with sharded configuration
        // This is for demo purposes - normally you'd configure this at startup
        var unityProvider = new UnityAnalyticsProvider("unity-player-shard");
        var firebaseProvider = new FirebaseAnalyticsProvider("firebase-system-shard");

        // In a real implementation, you would configure explicit shard mapping
        // registry.RegisterProvider with shard-specific configuration
        
        System.Console.WriteLine("🔄 Simulating Sharded Strategy behavior:");
        System.Console.WriteLine("- player.* events → Unity Analytics");
        System.Console.WriteLine("- system.* events → Firebase Analytics");
        System.Console.WriteLine();

        // Simulate sharded routing by calling providers directly for demo
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

        System.Console.WriteLine("📤 Tracking player event (should route to Unity):");
        await unityProvider.Track(playerEvent);
        System.Console.WriteLine();

        System.Console.WriteLine("📤 Tracking system event (should route to Firebase):");
        await firebaseProvider.Track(systemEvent);
        System.Console.WriteLine();

        System.Console.WriteLine("💡 In production, the Analytics façade would automatically");
        System.Console.WriteLine("   route these events based on the configured Sharded strategy.");
    }
}