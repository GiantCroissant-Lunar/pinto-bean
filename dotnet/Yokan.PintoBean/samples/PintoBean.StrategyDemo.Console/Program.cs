using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Yokan.PintoBean.Runtime;

namespace PintoBean.StrategyDemo.ConsoleApp;

/// <summary>
/// Demo program showing how to use selection strategies with DI integration.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        System.Console.WriteLine("=== PintoBean Selection Strategies Demo ===\n");

        // Create a host builder with DI
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                // Example 1: Use default RFC-0003 category mappings
                services.AddSelectionStrategies();

                // Example 2: Override category defaults
                services.AddSelectionStrategies(options =>
                {
                    // Override Analytics to use PickOne instead of default FanOut
                    options.SetCategoryDefault(ServiceCategory.Analytics, SelectionStrategyType.PickOne);
                });

                // Example 3: Use helper methods for specific services
                services.UsePickOneFor<IAnalyticsService>()    // Override Analytics default
                        .UseFanOutFor<IResourceService>()      // Override Resources default
                        .UseShardedFor<IAIService>();          // Override AI default

                // Register some demo service implementations
                services.AddSingleton<IAnalyticsService, ConsoleAnalyticsService>();
                services.AddSingleton<IResourceService, FileResourceService>();
                services.AddSingleton<IAIService, OpenAIService>();
                services.AddSingleton<ISceneFlowService, UnitySceneFlowService>();
            })
            .Build();

        // Get the strategy factory and demonstrate different strategies
        var factory = host.Services.GetRequiredService<ISelectionStrategyFactory>();
        
        System.Console.WriteLine("📊 Demonstrating Strategy Selection by Category:");
        DemonstrateStrategySelection(factory);

        System.Console.WriteLine("\n🔧 Demonstrating Strategy Configuration:");
        DemonstrateStrategyConfiguration(host.Services);

        System.Console.WriteLine("\n✅ Demo completed successfully!");
    }

    private static void DemonstrateStrategySelection(ISelectionStrategyFactory factory)
    {
        // Analytics services - should get FanOut by default (unless overridden)
        var analyticsStrategy = factory.CreateStrategy<IAnalyticsService>();
        System.Console.WriteLine($"Analytics Service: {analyticsStrategy.StrategyType}");

        // Resource services - should get PickOne by default  
        var resourceStrategy = factory.CreateStrategy<IResourceService>();
        System.Console.WriteLine($"Resource Service: {resourceStrategy.StrategyType}");

        // AI services - should get PickOne by default
        var aiStrategy = factory.CreateStrategy<IAIService>();
        System.Console.WriteLine($"AI Service: {aiStrategy.StrategyType}");

        // Scene flow services - should get PickOne by default
        var sceneFlowStrategy = factory.CreateStrategy<ISceneFlowService>();
        System.Console.WriteLine($"SceneFlow Service: {sceneFlowStrategy.StrategyType}");
    }

    private static void DemonstrateStrategyConfiguration(IServiceProvider services)
    {
        var options = services.GetRequiredService<SelectionStrategyOptions>();
        
        System.Console.WriteLine("Category defaults:");
        System.Console.WriteLine($"  Analytics: {options.GetDefaultForCategory(ServiceCategory.Analytics)}");
        System.Console.WriteLine($"  Resources: {options.GetDefaultForCategory(ServiceCategory.Resources)}");
        System.Console.WriteLine($"  SceneFlow: {options.GetDefaultForCategory(ServiceCategory.SceneFlow)}");
        System.Console.WriteLine($"  AI: {options.GetDefaultForCategory(ServiceCategory.AI)}");

        System.Console.WriteLine("\nService-specific overrides:");
        var overrides = options.GetAllStrategyOverrides();
        foreach (var (serviceType, strategy) in overrides)
        {
            System.Console.WriteLine($"  {serviceType.Name}: {strategy}");
        }
    }
}

// Demo service interfaces and implementations

public interface IAnalyticsService
{
    void TrackEvent(string eventName, object data);
}

public class ConsoleAnalyticsService : IAnalyticsService
{
    public void TrackEvent(string eventName, object data)
    {
        System.Console.WriteLine($"[Analytics] {eventName}: {data}");
    }
}

public interface IResourceService
{
    string LoadResource(string key);
}

public class FileResourceService : IResourceService
{
    public string LoadResource(string key)
    {
        return $"Resource content for {key}";
    }
}

public interface IAIService
{
    string GenerateResponse(string prompt);
}

public class OpenAIService : IAIService
{
    public string GenerateResponse(string prompt)
    {
        return $"AI generated response to: {prompt}";
    }
}

public interface ISceneFlowService
{
    void LoadScene(string sceneName);
}

public class UnitySceneFlowService : ISceneFlowService
{
    public void LoadScene(string sceneName)
    {
        System.Console.WriteLine($"[Unity] Loading scene: {sceneName}");
    }
}