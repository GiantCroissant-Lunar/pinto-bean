// P6-07: Unity Analytics + Resources Sample Demonstration
// Shows how the Unity sample integrates Analytics (FanOut/Sharded) with Resources (PickOne)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime;

namespace PintoBean.Unity.AnalyticsResourcesDemo
{
    /// <summary>
    /// Demonstrates the Unity Analytics + Resources sample concepts in a console application.
    /// This shows how the Unity MonoBehaviour would work when integrated with actual providers.
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== P6-07: Unity Analytics + Resources Sample Demo ===\n");
            Console.WriteLine("This demonstrates the Unity sample behavior with FanOut vs Sharded strategies.\n");

            // First demonstrate FanOut strategy (default for Analytics)
            Console.WriteLine("🎯 Demo 1: FanOut Strategy (Default Configuration)");
            Console.WriteLine("Expected: Analytics events sent to ALL providers\n");
            
            await DemonstrateFanOutStrategy();

            Console.WriteLine("\n" + new string('=', 60) + "\n");

            // Then demonstrate Sharded strategy
            Console.WriteLine("🎯 Demo 2: Sharded Strategy (Alternative Configuration)");
            Console.WriteLine("Expected: Analytics events routed to specific providers by prefix\n");
            
            await DemonstrateShardedStrategy();

            Console.WriteLine("\n✅ Unity Analytics + Resources Demo completed!");
            Console.WriteLine("💡 In Unity, this would be triggered by MonoBehaviour.Start() via DI bridge");
        }

        private static async Task DemonstrateFanOutStrategy()
        {
            // Set up services with FanOut strategy for Analytics
            var services = new ServiceCollection();
            
            services.AddPollyResilience(options =>
            {
                options.DefaultTimeoutSeconds = 2.0;
                options.MaxRetryAttempts = 2;
                options.BaseRetryDelayMilliseconds = 100.0;
                options.EnableCircuitBreaker = false;
            });

            services.AddNoOpAspectRuntime();

            services.AddServiceRegistry(registry =>
            {
                // Register Analytics providers
                registry.Register<IAnalytics>(new UnityAnalyticsProvider("unity-analytics"), 
                    ProviderCapabilities.Create("unity-analytics").WithPriority(Priority.Normal));
                registry.Register<IAnalytics>(new FirebaseAnalyticsProvider("firebase-analytics"), 
                    ProviderCapabilities.Create("firebase-analytics").WithPriority(Priority.Normal));

                // Register Resource providers (for PickOne demo)
                registry.Register<IResourceStore>(new CacheResourceStore("cache-resources"), 
                    ProviderCapabilities.Create("cache-resources").WithPriority(Priority.Critical));
                registry.Register<IResourceStore>(new NetworkResourceStore("network-resources"), 
                    ProviderCapabilities.Create("network-resources").WithPriority(Priority.High));
            });

            // Configure FanOut for Analytics (default)
            services.UseFanOutFor<IAnalytics>();
            services.UsePickOneFor<IResourceStore>();

            var serviceProvider = services.BuildServiceProvider();

            // Simulate Unity sample behavior
            await RunUnitySimulation(serviceProvider, "FanOut");
        }

        private static async Task DemonstrateShardedStrategy()
        {
            // Set up services with Sharded strategy for Analytics
            var services = new ServiceCollection();
            
            services.AddPollyResilience(options =>
            {
                options.DefaultTimeoutSeconds = 2.0;
                options.MaxRetryAttempts = 2;
                options.BaseRetryDelayMilliseconds = 100.0;
                options.EnableCircuitBreaker = false;
            });

            services.AddNoOpAspectRuntime();

            services.AddServiceRegistry(registry =>
            {
                // Register Analytics providers with explicit shard routing
                registry.Register<IAnalytics>(new UnityAnalyticsProvider("unity-analytics"), 
                    ProviderCapabilities.Create("unity-analytics").WithPriority(Priority.Normal));
                registry.Register<IAnalytics>(new FirebaseAnalyticsProvider("firebase-analytics"), 
                    ProviderCapabilities.Create("firebase-analytics").WithPriority(Priority.Normal));

                // Register Resource providers
                registry.Register<IResourceStore>(new CacheResourceStore("cache-resources"), 
                    ProviderCapabilities.Create("cache-resources").WithPriority(Priority.Critical));
            });

            // Configure Sharded for Analytics
            services.UseShardedFor<IAnalytics>();
            services.UsePickOneFor<IResourceStore>();

            var serviceProvider = services.BuildServiceProvider();

            // Simulate Unity sample behavior
            await RunUnitySimulation(serviceProvider, "Sharded");
        }

        private static async Task RunUnitySimulation(IServiceProvider serviceProvider, string strategyType)
        {
            Console.WriteLine($"🔧 Unity DI Bridge Simulation ({strategyType} Strategy):");
            
            // Get services (simulates Unity DI bridge resolution)
            var registry = serviceProvider.GetRequiredService<IServiceRegistry>();
            var analytics = registry.For<IAnalytics>();
            var resourceStore = registry.For<IResourceStore>();

            // Simulate Unity sample's Start() method behavior
            Console.WriteLine("📤 Sending analytics events (like Unity sample):\n");

            // Event 1: Player event (same as Unity sample)
            var playerEvent = new AnalyticsEvent
            {
                EventName = "player.level.start",
                UserId = "unity-player-001",
                SessionId = "unity-session-123",
                Properties = new Dictionary<string, object>
                {
                    ["level"] = 5,
                    ["character"] = "warrior"
                }
            };

            Console.WriteLine($"   Event: {playerEvent.EventName}");
            await analytics.InvokeAsync((provider, ct) => provider.Track(playerEvent, ct));

            // Event 2: System event (same as Unity sample)
            var systemEvent = new AnalyticsEvent
            {
                EventName = "system.performance.metrics",
                Properties = new Dictionary<string, object>
                {
                    ["fps"] = 60,
                    ["memory_mb"] = 256
                }
            };

            Console.WriteLine($"   Event: {systemEvent.EventName}");
            await analytics.InvokeAsync((provider, ct) => provider.Track(systemEvent, ct));

            // Resource loading (same as Unity sample)
            Console.WriteLine("\n📦 Loading resource (like Unity sample):");
            var resourceData = await resourceStore.InvokeAsync((store, ct) => 
                store.LoadResourceAsync("unity-test-config.json", ct));
            
            Console.WriteLine($"   ✅ Resource loaded from: {resourceData.Source}");
            Console.WriteLine($"   📄 Content preview: {resourceData.Content.Substring(0, Math.Min(50, resourceData.Content.Length))}...");
        }
    }

    // Sample Analytics Providers (same pattern as existing samples)
    public class UnityAnalyticsProvider : IAnalytics
    {
        public string ProviderId { get; }

        public UnityAnalyticsProvider(string providerId)
        {
            ProviderId = providerId;
        }

        public Task Track(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default)
        {
            var userInfo = !string.IsNullOrEmpty(analyticsEvent.UserId) ? $" for user {analyticsEvent.UserId}" : "";
            var sessionInfo = !string.IsNullOrEmpty(analyticsEvent.SessionId) ? $" in session {analyticsEvent.SessionId}" : "";
            
            Console.WriteLine($"     [Unity Analytics] Tracking '{analyticsEvent.EventName}'{userInfo}{sessionInfo}");
            return Task.CompletedTask;
        }
    }

    public class FirebaseAnalyticsProvider : IAnalytics
    {
        public string ProviderId { get; }

        public FirebaseAnalyticsProvider(string providerId)
        {
            ProviderId = providerId;
        }

        public Task Track(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default)
        {
            var userInfo = !string.IsNullOrEmpty(analyticsEvent.UserId) ? $" | User: {analyticsEvent.UserId}" : "";
            var sessionInfo = !string.IsNullOrEmpty(analyticsEvent.SessionId) ? $" | Session: {analyticsEvent.SessionId}" : "";
            
            Console.WriteLine($"     [Firebase Analytics] Event: {analyticsEvent.EventName}{userInfo}{sessionInfo}");
            return Task.CompletedTask;
        }
    }

    // Sample Resource Store (same pattern as existing samples)
    public class CacheResourceStore : IResourceStore
    {
        public string StoreName { get; }

        public CacheResourceStore(string name)
        {
            StoreName = name;
        }

        public Task<ResourceData> LoadResourceAsync(string resourceKey, CancellationToken cancellationToken = default)
        {
            var content = $$"""
            {
                "resourceKey": "{{resourceKey}}",
                "source": "cache",
                "cached": true,
                "data": "Unity test configuration"
            }
            """;

            return Task.FromResult(new ResourceData(resourceKey, content, StoreName, DateTime.UtcNow));
        }

        public Task<bool> ResourceExistsAsync(string resourceKey, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }

    public class NetworkResourceStore : IResourceStore
    {
        public string StoreName { get; }

        public NetworkResourceStore(string name)
        {
            StoreName = name;
        }

        public Task<ResourceData> LoadResourceAsync(string resourceKey, CancellationToken cancellationToken = default)
        {
            var content = $$"""
            {
                "resourceKey": "{{resourceKey}}",
                "source": "network",
                "data": "Network configuration"
            }
            """;

            return Task.FromResult(new ResourceData(resourceKey, content, StoreName, DateTime.UtcNow));
        }

        public Task<bool> ResourceExistsAsync(string resourceKey, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }

    // Resource data container (same as other samples)
    public record ResourceData(string ResourceKey, string Content, string Source, DateTime LoadedAt);

    // Resource store interface (same pattern as other samples)
    public interface IResourceStore
    {
        Task<ResourceData> LoadResourceAsync(string resourceKey, CancellationToken cancellationToken = default);
        Task<bool> ResourceExistsAsync(string resourceKey, CancellationToken cancellationToken = default);
        string StoreName { get; }
    }
}