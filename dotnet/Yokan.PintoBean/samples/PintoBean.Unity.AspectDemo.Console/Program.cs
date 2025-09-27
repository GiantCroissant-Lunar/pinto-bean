using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime;
using Yokan.PintoBean.Runtime.Unity;

namespace PintoBean.Unity.AspectDemo.Console;

/// <summary>
/// Demo program showcasing UnityAspectRuntime logging capabilities.
/// Simulates Unity play mode behavior with facade call logging.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        System.Console.WriteLine("=== PintoBean Unity Aspect Runtime Demo ===");
        System.Console.WriteLine();

        // Setup service collection with Unity aspect runtime
        var services = new ServiceCollection();
        
        // Register Unity aspect runtime with verbose logging
        services.AddUnityAspectRuntime(enableMetrics: true, verboseLogging: true);
        
        // Register demo services directly in DI (simpler approach for demo)
        services.AddTransient<IHelloService, DemoHelloService>();
        services.AddTransient<ICalculatorService, DemoCalculatorService>();
        
        var serviceProvider = services.BuildServiceProvider();
        
        System.Console.WriteLine("✅ Services configured with UnityAspectRuntime");
        System.Console.WriteLine();

        System.Console.WriteLine("=== Demonstrating Unity Aspect Runtime logging ===");
        System.Console.WriteLine();

        // Get the aspect runtime to demonstrate its logging capabilities
        var aspectRuntime = serviceProvider.GetRequiredService<IAspectRuntime>();
        var helloService = serviceProvider.GetRequiredService<IHelloService>();
        var calculatorService = serviceProvider.GetRequiredService<ICalculatorService>();

        // Demonstrate manual aspect runtime usage (simulating what the framework would do)
        System.Console.WriteLine("1. Simple service call with aspect runtime tracking:");
        using (var context = aspectRuntime.EnterMethod(typeof(IHelloService), nameof(IHelloService.SayHelloAsync), new object[] { "World" }))
        {
            try
            {
                var greeting = await helloService.SayHelloAsync("World");
                aspectRuntime.ExitMethod(context, greeting);
                System.Console.WriteLine($"   Result: {greeting}");
            }
            catch (Exception ex)
            {
                aspectRuntime.RecordException(context, ex);
                throw;
            }
        }
        System.Console.WriteLine();

        System.Console.WriteLine("2. Service call with parameters:");
        using (var context = aspectRuntime.EnterMethod(typeof(ICalculatorService), nameof(ICalculatorService.AddAsync), new object[] { 15, 27 }))
        {
            try
            {
                var sum = await calculatorService.AddAsync(15, 27);
                aspectRuntime.ExitMethod(context, sum);
                System.Console.WriteLine($"   Result: {sum}");
            }
            catch (Exception ex)
            {
                aspectRuntime.RecordException(context, ex);
                throw;
            }
        }
        System.Console.WriteLine();

        System.Console.WriteLine("3. Service call that throws exception:");
        using (var context = aspectRuntime.EnterMethod(typeof(ICalculatorService), nameof(ICalculatorService.DivideAsync), new object[] { 10.0, 0.0 }))
        {
            try
            {
                await calculatorService.DivideAsync(10, 0);
                aspectRuntime.ExitMethod(context, null);
            }
            catch (Exception ex)
            {
                aspectRuntime.RecordException(context, ex);
                System.Console.WriteLine($"   Caught: {ex.Message}");
            }
        }
        System.Console.WriteLine();

        System.Console.WriteLine("4. Custom operation tracking:");
        using (var operation = aspectRuntime.StartOperation("custom-calculation", new Dictionary<string, object>
        {
            ["operation"] = "complex-math",
            ["input"] = "42"
        }))
        {
            await Task.Delay(100); // Simulate work
            aspectRuntime.RecordMetric("calculation.complexity", 8.5, ("type", "advanced"), ("iterations", 100));
        }
        System.Console.WriteLine();

        System.Console.WriteLine("=== Testing adaptive runtime selection ===");
        System.Console.WriteLine();

        // Test adaptive runtime
        var adaptiveServices = new ServiceCollection();
        adaptiveServices.AddAdaptiveAspectRuntime("PintoBean.Demo", "PintoBean.Demo");
        adaptiveServices.AddTransient<IHelloService, DemoHelloService>();
        
        var adaptiveProvider = adaptiveServices.BuildServiceProvider();
        var adaptiveRuntime = adaptiveProvider.GetRequiredService<IAspectRuntime>();
        
        System.Console.WriteLine($"Adaptive runtime selected: {adaptiveRuntime.GetType().Name}");
        System.Console.WriteLine("(In .NET host with OpenTelemetry available, should be OtelAspectRuntime)");
        System.Console.WriteLine();

        System.Console.WriteLine("=== Demo completed ===");
    }
}

// Demo service interfaces and implementations
public interface IHelloService
{
    Task<string> SayHelloAsync(string name);
}

public interface ICalculatorService
{
    Task<int> AddAsync(int a, int b);
    Task<double> DivideAsync(double a, double b);
}

public class DemoHelloService : IHelloService
{
    public async Task<string> SayHelloAsync(string name)
    {
        await Task.Delay(50); // Simulate async work
        return $"Hello, {name}!";
    }
}

public class DemoCalculatorService : ICalculatorService
{
    public async Task<int> AddAsync(int a, int b)
    {
        await Task.Delay(25); // Simulate async work
        return a + b;
    }

    public async Task<double> DivideAsync(double a, double b)
    {
        await Task.Delay(30); // Simulate async work
        if (b == 0)
            throw new DivideByZeroException("Cannot divide by zero");
        return a / b;
    }
}