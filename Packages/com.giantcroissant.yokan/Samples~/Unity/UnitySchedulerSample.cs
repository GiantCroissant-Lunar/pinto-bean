// Unity scheduler sample for Yokan PintoBean service platform
// This demonstrates using the Unity scheduler for main-thread synchronization

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Yokan.PintoBean.Runtime.Unity;

// Note: In actual Unity project, add: using UnityEngine;

namespace PintoBean.Unity.Sample
{
    /// <summary>
    /// Sample MonoBehaviour demonstrating Unity scheduler integration for main-thread synchronization.
    /// This shows how to ensure provider calls run on the Unity main thread.
    /// 
    /// In Unity: inherit from MonoBehaviour
    /// For compile testing: inherit from ServiceAwareMonoBehaviour
    /// </summary>
    public class UnitySchedulerSample : ServiceAwareMonoBehaviour // In Unity: MonoBehaviour
    {
        private IUnityScheduler? _scheduler;
        private UnitySchedulerProcessor? _processor;

        /// <summary>
        /// Unity Start method. Sets up the scheduler and processor.
        /// </summary>
        // In Unity: void Start()
        public void Initialize()
        {
            // In Unity: Debug.Log("Unity Scheduler Sample - Initializing");
            System.Console.WriteLine("Unity Scheduler Sample - Initializing");

            // Get scheduler from the service provider bridge if available
            if (UnityServiceProviderBridge.IsInitialized)
            {
                _scheduler = UnityServiceProviderBridge.Current.Scheduler;
                
                if (_scheduler != null)
                {
                    // In Unity: Debug.Log("Unity scheduler found and ready");
                    System.Console.WriteLine("Unity scheduler found and ready");
                }
                else
                {
                    // In Unity: Debug.LogWarning("No Unity scheduler found. Consider calling AddUnityScheduler() during service configuration.");
                    System.Console.WriteLine("No Unity scheduler found. Consider calling AddUnityScheduler() during service configuration.");
                }
            }

            // Set up the scheduler processor
            _processor = new UnitySchedulerProcessor();
            _processor.Initialize();

            // Demonstrate scheduler usage
            _ = DemoSchedulerUsage();
        }

        /// <summary>
        /// Unity Update method. Processes the scheduler queue.
        /// </summary>
        // In Unity: void Update()
        public void ProcessScheduler() 
        {
            _processor?.ProcessSchedulerQueue();
        }

        /// <summary>
        /// Demonstrates scheduler usage for main-thread synchronization.
        /// </summary>
        private async Task DemoSchedulerUsage()
        {
            if (_scheduler == null)
            {
                // In Unity: Debug.LogWarning("No scheduler available for demo");
                System.Console.WriteLine("No scheduler available for demo");
                return;
            }

            // In Unity: Debug.Log("Starting scheduler demo");
            System.Console.WriteLine("Starting scheduler demo");

            // Demo 1: Post a simple action from a background thread
            await Task.Run(() =>
            {
                // In Unity: Debug.Log($"Running on background thread: {Thread.CurrentThread.ManagedThreadId}");
                System.Console.WriteLine($"Running on background thread: {Thread.CurrentThread.ManagedThreadId}");

                _scheduler.Post(() =>
                {
                    // In Unity: Debug.Log($"Action executed on main thread: {Thread.CurrentThread.ManagedThreadId}");
                    System.Console.WriteLine($"Action executed on main thread: {Thread.CurrentThread.ManagedThreadId}");
                });
            });

            // Demo 2: Post an async operation from a background thread
            await Task.Run(async () =>
            {
                // In Unity: Debug.Log("Posting async operation from background thread");
                System.Console.WriteLine("Posting async operation from background thread");

                await _scheduler.PostAsync(async () =>
                {
                    // In Unity: Debug.Log("Async operation started on main thread");
                    System.Console.WriteLine("Async operation started on main thread");
                    
                    // Simulate some async work
                    await Task.Delay(100);
                    
                    // In Unity: Debug.Log("Async operation completed on main thread");
                    System.Console.WriteLine("Async operation completed on main thread");
                });
            });

            // Demo 3: Using the bridge's convenience methods
            UnityServiceProviderBridge.Current.PostToMainThread(() =>
            {
                // In Unity: Debug.Log("Posted via bridge convenience method");
                System.Console.WriteLine("Posted via bridge convenience method");
            });

            await UnityServiceProviderBridge.Current.PostToMainThreadAsync(async () =>
            {
                // In Unity: Debug.Log("Posted async via bridge convenience method");
                System.Console.WriteLine("Posted async via bridge convenience method");
                await Task.Delay(50);
            });

            // In Unity: Debug.Log("Scheduler demo completed");
            System.Console.WriteLine("Scheduler demo completed");
        }

        /// <summary>
        /// Unity OnDestroy method. Cleans up resources.
        /// </summary>
        // In Unity: void OnDestroy()
        public void Cleanup()
        {
            _processor?.Cleanup();
            _processor = null;
            _scheduler = null;
        }
    }

    /// <summary>
    /// Example of how to configure services with Unity scheduler support.
    /// This would typically be called during application initialization.
    /// </summary>
    public static class UnitySchedulerConfiguration
    {
        /// <summary>
        /// Configures services with Unity scheduler support.
        /// </summary>
        /// <returns>The configured service provider.</returns>
        public static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Add Unity scheduler with the current thread as the main thread
            services.AddUnityScheduler();

            // Add Unity bridge support
            services.AddUnityBridge(new DefaultUnityLifetimeScopeFactory());

            // Build and initialize the service provider
            var serviceProvider = services.BuildServiceProvider();

            // Initialize the Unity bridge
            UnityServiceProviderBridge.Initialize(serviceProvider);

            return serviceProvider;
        }
    }
}