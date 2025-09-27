// Unity sample scene demonstrating Analytics + Resource façades with FanOut vs Sharded effects
// P6-07: Unity sample scene using Analytics + Resource façades

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime.Unity;
using Yokan.PintoBean.CodeGen;
using Yokan.PintoBean.Runtime;

// Note: In actual Unity project, add: using UnityEngine;

namespace PintoBean.Unity.Sample
{
    /// <summary>
    /// Unity sample demonstrating Analytics + Resource façades with FanOut vs Sharded routing strategies.
    /// Shows the difference between FanOut (sends to all providers) and Sharded (routes based on criteria).
    /// On Start(), sends 2 analytics events and loads a test resource, showing different behaviors.
    /// </summary>
    public class AnalyticsResourcesSample : ServiceAwareMonoBehaviour // In Unity: MonoBehaviour
    {
        private IAnalytics? _analytics;
        private IResourceStore? _resourceStore;

        /// <summary>
        /// Unity Start method. Initializes services and demonstrates Analytics + Resources integration.
        /// </summary>
        // In Unity: void Start()
        public void Initialize()
        {
            // In Unity: Debug.Log("=== PintoBean Analytics + Resources Sample ===");
            System.Console.WriteLine("=== PintoBean Analytics + Resources Sample ===");
            
            try
            {
                // Resolve services through Unity DI bridge
                _analytics = GetService<IAnalytics>();
                _resourceStore = GetService<IResourceStore>();

                // In Unity: Debug.Log("✅ Successfully resolved Analytics and Resource services");
                System.Console.WriteLine("✅ Successfully resolved Analytics and Resource services");

                // Start the demonstration
                _ = DemonstrateAnalyticsAndResources();
            }
            catch (Exception ex)
            {
                // In Unity: Debug.LogError($"❌ Failed to resolve services: {ex.Message}");
                System.Console.WriteLine($"❌ Failed to resolve services: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates Analytics FanOut vs Sharded routing and Resource loading.
        /// </summary>
        private async Task DemonstrateAnalyticsAndResources()
        {
            if (_analytics == null || _resourceStore == null)
            {
                // In Unity: Debug.LogError("Services not available");
                System.Console.WriteLine("❌ Services not available");
                return;
            }

            try
            {
                // === ANALYTICS DEMONSTRATION ===
                // In Unity: Debug.Log("\n🎯 Demonstrating Analytics Events");
                System.Console.WriteLine("\n🎯 Demonstrating Analytics Events");

                // Event 1: Player action (should demonstrate routing behavior)
                var playerEvent = new AnalyticsEvent
                {
                    EventName = "player.level.start",
                    UserId = "unity-player-001",
                    SessionId = "unity-session-123",
                    Properties = new Dictionary<string, object>
                    {
                        ["level"] = 5,
                        ["character"] = "warrior",
                        ["timestamp"] = DateTime.UtcNow.ToString("O")
                    }
                };

                // In Unity: Debug.Log($"📤 Sending player event: {playerEvent.EventName}");
                System.Console.WriteLine($"📤 Sending player event: {playerEvent.EventName}");
                
                await _analytics.Track(playerEvent, CancellationToken.None);

                // Event 2: System event (should demonstrate different routing behavior)
                var systemEvent = new AnalyticsEvent
                {
                    EventName = "system.performance.metrics",
                    Properties = new Dictionary<string, object>
                    {
                        ["fps"] = 60,
                        ["memory_mb"] = 256,
                        ["cpu_usage"] = 0.45,
                        ["timestamp"] = DateTime.UtcNow.ToString("O")
                    }
                };

                // In Unity: Debug.Log($"📤 Sending system event: {systemEvent.EventName}");
                System.Console.WriteLine($"📤 Sending system event: {systemEvent.EventName}");
                
                await _analytics.Track(systemEvent, CancellationToken.None);

                // === RESOURCES DEMONSTRATION ===
                // In Unity: Debug.Log("\n📦 Demonstrating Resource Loading");
                System.Console.WriteLine("\n📦 Demonstrating Resource Loading");

                // Load a test resource
                var resourceKey = "unity-test-config.json";
                
                // In Unity: Debug.Log($"📁 Loading resource: {resourceKey}");
                System.Console.WriteLine($"📁 Loading resource: {resourceKey}");

                var resourceData = await _resourceStore.LoadResourceAsync(resourceKey, CancellationToken.None);

                // In Unity: Debug.Log($"✅ Resource loaded successfully from: {resourceData.Source}");
                System.Console.WriteLine($"✅ Resource loaded successfully from: {resourceData.Source}");
                
                // In Unity: Debug.Log($"📄 Resource content preview: {resourceData.Content.Substring(0, Math.Min(100, resourceData.Content.Length))}...");
                System.Console.WriteLine($"📄 Resource content preview: {resourceData.Content.Substring(0, Math.Min(100, resourceData.Content.Length))}...");

                // === SUMMARY ===
                // In Unity: Debug.Log("\n✅ Analytics + Resources Sample completed successfully!");
                System.Console.WriteLine("\n✅ Analytics + Resources Sample completed successfully!");
                
                // In Unity: Debug.Log("💡 FanOut Strategy: Events sent to ALL analytics providers");
                System.Console.WriteLine("💡 FanOut Strategy: Events sent to ALL analytics providers");
                
                // In Unity: Debug.Log("💡 Sharded Strategy: Events routed to specific providers based on event name prefix");
                System.Console.WriteLine("💡 Sharded Strategy: Events routed to specific providers based on event name prefix");
                
                // In Unity: Debug.Log("💡 Resources: Loaded via PickOne strategy with provider priority fallback");
                System.Console.WriteLine("💡 Resources: Loaded via PickOne strategy with provider priority fallback");
            }
            catch (Exception ex)
            {
                // In Unity: Debug.LogError($"❌ Error during demonstration: {ex.Message}");
                System.Console.WriteLine($"❌ Error during demonstration: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Resource store interface for loading various types of resources asynchronously.
    /// This represents Tier-1 service contract for resource management.
    /// </summary>
    public interface IResourceStore
    {
        /// <summary>
        /// Load a resource by key asynchronously.
        /// </summary>
        Task<ResourceData> LoadResourceAsync(string resourceKey, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Check if a resource exists asynchronously.
        /// </summary>
        Task<bool> ResourceExistsAsync(string resourceKey, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Get metadata about the resource store.
        /// </summary>
        string StoreName { get; }
    }

    /// <summary>
    /// Resource data container.
    /// </summary>
    public record ResourceData(string ResourceKey, string Content, string Source, DateTime LoadedAt);

    // Note: The actual service façades would be implemented using [RealizeService]
    // code generation in a real Unity project. For this sample, we demonstrate the interface usage
    // and the services would be resolved through the Unity DI bridge.
}