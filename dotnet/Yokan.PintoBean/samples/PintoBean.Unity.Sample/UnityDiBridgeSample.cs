// Unity DI bridge sample for Yokan PintoBean service platform
// This demonstrates resolving IHelloService through the Unity DI bridge

using System;
using System.Threading.Tasks;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime.Unity;

// Note: In actual Unity project, add: using UnityEngine;

namespace PintoBean.Unity.Sample
{
    /// <summary>
    /// Sample MonoBehaviour demonstrating Unity DI bridge integration with PintoBean services.
    /// This shows how to resolve IHelloService through the Microsoft.Extensions.DependencyInjection bridge.
    /// 
    /// In Unity: inherit from MonoBehaviour
    /// For compile testing: inherit from ServiceAwareMonoBehaviour
    /// </summary>
    public class UnityDiBridgeSample : ServiceAwareMonoBehaviour // In Unity: MonoBehaviour
    {
        private IHelloService? _helloService;

        /// <summary>
        /// Unity Start method. Resolves services and demonstrates usage.
        /// </summary>
        // In Unity: void Start()
        public void Initialize()  
        {
            // In Unity: Debug.Log("PintoBean Unity DI Bridge Sample - Starting");
            System.Console.WriteLine("PintoBean Unity DI Bridge Sample - Starting");
            
            try
            {
                // Resolve IHelloService through the Unity DI bridge
                _helloService = GetService<IHelloService>();
                
                // In Unity: Debug.Log("Successfully resolved IHelloService through Unity DI bridge");
                System.Console.WriteLine("Successfully resolved IHelloService through Unity DI bridge");
                
                // Demonstrate service usage
                _ = DemoHelloServiceUsage();
            }
            catch (Exception ex)
            {
                // In Unity: Debug.LogError($"Failed to resolve IHelloService: {ex.Message}");
                System.Console.WriteLine($"Error: Failed to resolve IHelloService: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates calling IHelloService methods through the Unity DI bridge.
        /// </summary>
        private async Task DemoHelloServiceUsage()
        {
            if (_helloService == null)
                return;

            try
            {
                // Create hello request
                var helloRequest = new HelloRequest
                {
                    Name = "Unity",
                    Message = "Greetings from Unity DI Bridge!"
                };

                // Call SayHelloAsync through the bridge
                var helloResponse = await _helloService.SayHelloAsync(helloRequest);
                
                // In Unity: Debug.Log($"Hello Response: {helloResponse.Message}");
                System.Console.WriteLine($"Hello Response: {helloResponse.Message}");

                // Call SayGoodbyeAsync through the bridge
                var goodbyeRequest = new HelloRequest
                {
                    Name = "Unity",
                    Message = "Farewell from Unity DI Bridge!"
                };

                var goodbyeResponse = await _helloService.SayGoodbyeAsync(goodbyeRequest);
                
                // In Unity: Debug.Log($"Goodbye Response: {goodbyeResponse.Message}");
                System.Console.WriteLine($"Goodbye Response: {goodbyeResponse.Message}");
            }
            catch (Exception ex)
            {
                // In Unity: Debug.LogError($"Error calling IHelloService: {ex.Message}");
                System.Console.WriteLine($"Error calling IHelloService: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates alternative service resolution patterns.
        /// </summary>
        public void DemoAlternativeResolutionPatterns()
        {
            // Example 1: Try to get service, handle null gracefully
            var optionalService = GetServiceOrNull<IHelloService>();
            if (optionalService != null)
            {
                // In Unity: Debug.Log("Optional service resolution succeeded");
                System.Console.WriteLine("Optional service resolution succeeded");
            }

            // Example 2: Get required service (throws if not found)
            try
            {
                var requiredService = GetRequiredService<IHelloService>();
                // In Unity: Debug.Log("Required service resolution succeeded");
                System.Console.WriteLine("Required service resolution succeeded");
            }
            catch (InvalidOperationException ex)
            {
                // In Unity: Debug.LogError($"Required service not found: {ex.Message}");
                System.Console.WriteLine($"Required service not found: {ex.Message}");
            }

            // Example 3: Resolve by type
            var serviceByType = GetService(typeof(IHelloService));
            if (serviceByType != null)
            {
                // In Unity: Debug.Log("Service resolution by type succeeded");
                System.Console.WriteLine("Service resolution by type succeeded");
            }
        }
    }
}