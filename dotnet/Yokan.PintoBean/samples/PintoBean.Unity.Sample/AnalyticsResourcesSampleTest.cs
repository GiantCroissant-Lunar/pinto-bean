// Test program for Unity Analytics + Resources sample (P6-07)
// This demonstrates the sample functionality as a console application

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime;
using PintoBean.Unity.Sample;

namespace PintoBean.Unity.Sample.Test
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== P6-07: Unity Analytics + Resources Sample Test ===\n");

            // Set up dependency injection
            var services = new ServiceCollection();

            // Add resilience executor
            services.AddPollyResilience(options =>
            {
                options.DefaultTimeoutSeconds = 2.0;
                options.MaxRetryAttempts = 2;
                options.BaseRetryDelayMilliseconds = 100.0;
                options.EnableCircuitBreaker = false;
            });

            // Add aspect runtime
            services.AddNoOpAspectRuntime();

            // Add service registry and selection strategies
            services.AddServiceRegistry(registry =>
            {
                // Register Analytics providers (for FanOut demo)
                registry.Register<IAnalytics>(new UnityAnalyticsProvider("unity-analytics"), 
                    Priority.Normal, ServiceCategory.Analytics);
                registry.Register<IAnalytics>(new FirebaseAnalyticsProvider("firebase-analytics"), 
                    Priority.Normal, ServiceCategory.Analytics);

                // Register Resource providers (for PickOne demo) 
                registry.Register<IResourceStore>(new CacheResourceStore("cache-resources"), 
                    Priority.Critical, ServiceCategory.Resources);
                registry.Register<IResourceStore>(new NetworkResourceStore("network-resources"), 
                    Priority.High, ServiceCategory.Resources);
                registry.Register<IResourceStore>(new LocalFileResourceStore("local-file-resources"), 
                    Priority.Normal, ServiceCategory.Resources);
            });

            // Configure FanOut strategy for Analytics (default)
            services.AddSelectionStrategies(options =>
            {
                options.UseFanOutFor<IAnalytics>();
                options.UsePickOneFor<IResourceStore>();
            });

            // Register our facades
            services.AddTransient<IAnalytics, AnalyticsService>();
            services.AddTransient<IResourceStore, ResourceStoreService>();

            var serviceProvider = services.BuildServiceProvider();

            // Create the sample and test it
            var sample = new AnalyticsResourcesSample();
            
            // Mock the DI resolution for testing
            var analytics = serviceProvider.GetRequiredService<IAnalytics>();
            var resourceStore = serviceProvider.GetRequiredService<IResourceStore>();
            
            // Set private fields via reflection for testing (normally done by Unity DI bridge)
            var analyticsField = typeof(AnalyticsResourcesSample).GetField("_analytics", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var resourceStoreField = typeof(AnalyticsResourcesSample).GetField("_resourceStore", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            analyticsField?.SetValue(sample, analytics);
            resourceStoreField?.SetValue(sample, resourceStore);

            // Run the sample
            sample.Initialize();

            // Wait a bit for async operations to complete
            await Task.Delay(1000);

            Console.WriteLine("\n✅ Sample test completed successfully!");
            Console.WriteLine("💡 In Unity, this would be triggered automatically by the MonoBehaviour.Start() method");
        }
    }

    // Sample Analytics Providers for testing
    public class UnityAnalyticsProvider : IAnalytics
    {
        public string ProviderId { get; }

        public UnityAnalyticsProvider(string providerId)
        {
            ProviderId = providerId;
        }

        public Task Track(AnalyticsEvent analyticsEvent, System.Threading.CancellationToken cancellationToken = default)
        {
            var properties = string.Join(", ", analyticsEvent.Properties?.Keys ?? Array.Empty<string>());
            var userInfo = !string.IsNullOrEmpty(analyticsEvent.UserId) ? $" for user {analyticsEvent.UserId}" : "";
            var sessionInfo = !string.IsNullOrEmpty(analyticsEvent.SessionId) ? $" in session {analyticsEvent.SessionId}" : "";
            
            Console.WriteLine($"[Unity Analytics] Tracking event '{analyticsEvent.EventName}' with {analyticsEvent.Properties?.Count ?? 0} properties{userInfo}{sessionInfo}");
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

        public Task Track(AnalyticsEvent analyticsEvent, System.Threading.CancellationToken cancellationToken = default)
        {
            var properties = string.Join(", ", analyticsEvent.Properties?.Keys ?? Array.Empty<string>());
            var userInfo = !string.IsNullOrEmpty(analyticsEvent.UserId) ? $" | User: {analyticsEvent.UserId}" : "";
            var sessionInfo = !string.IsNullOrEmpty(analyticsEvent.SessionId) ? $" | Session: {analyticsEvent.SessionId}" : "";
            
            Console.WriteLine($"[Firebase Analytics] Event: {analyticsEvent.EventName} [{properties}]{userInfo}{sessionInfo} | Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            return Task.CompletedTask;
        }
    }

    // Sample Resource Store Providers for testing
    public class CacheResourceStore : IResourceStore
    {
        public string StoreName { get; }

        public CacheResourceStore(string name)
        {
            StoreName = name;
        }

        public Task<ResourceData> LoadResourceAsync(string resourceKey, System.Threading.CancellationToken cancellationToken = default)
        {
            var content = $$"""
            {
                "resourceKey": "{{resourceKey}}",
                "source": "cache",
                "cached": true,
                "timestamp": "{{DateTime.UtcNow:O}}",
                "data": "Cached content for {{resourceKey}}"
            }
            """;

            return Task.FromResult(new ResourceData(resourceKey, content, StoreName, DateTime.UtcNow));
        }

        public Task<bool> ResourceExistsAsync(string resourceKey, System.Threading.CancellationToken cancellationToken = default)
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

        public Task<ResourceData> LoadResourceAsync(string resourceKey, System.Threading.CancellationToken cancellationToken = default)
        {
            var content = $$"""
            {
                "resource": "{{resourceKey}}",
                "loadedFrom": "network",
                "timestamp": "{{DateTime.UtcNow:O}}",
                "data": "Network content for {{resourceKey}}"
            }
            """;

            return Task.FromResult(new ResourceData(resourceKey, content, StoreName, DateTime.UtcNow));
        }

        public Task<bool> ResourceExistsAsync(string resourceKey, System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }

    public class LocalFileResourceStore : IResourceStore
    {
        public string StoreName { get; }

        public LocalFileResourceStore(string name)
        {
            StoreName = name;
        }

        public Task<ResourceData> LoadResourceAsync(string resourceKey, System.Threading.CancellationToken cancellationToken = default)
        {
            var content = $$"""
            {
                "resource": "{{resourceKey}}",
                "loadedFrom": "local-file",
                "timestamp": "{{DateTime.UtcNow:O}}",
                "data": "Local file content for {{resourceKey}}"
            }
            """;

            return Task.FromResult(new ResourceData(resourceKey, content, StoreName, DateTime.UtcNow));
        }

        public Task<bool> ResourceExistsAsync(string resourceKey, System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}