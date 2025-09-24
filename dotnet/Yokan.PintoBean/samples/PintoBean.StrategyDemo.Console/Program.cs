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

        System.Console.WriteLine("\n🚀 Demonstrating FanOut Error Policies:");
        DemonstrateFanOutErrorPolicies();

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

    private static void DemonstrateFanOutErrorPolicies()
    {
        System.Console.WriteLine("Creating demo providers with mixed success/failure behavior...");
        
        // Create some demo analytics providers - some will succeed, some will fail
        var providers = new IAnalyticsService[]
        {
            new ConsoleAnalyticsService("Provider1", shouldFail: false),
            new ConsoleAnalyticsService("Provider2", shouldFail: true, failureMessage: "Network timeout"),
            new ConsoleAnalyticsService("Provider3", shouldFail: false),
            new ConsoleAnalyticsService("Provider4", shouldFail: true, failureMessage: "Rate limit exceeded")
        };

        System.Console.WriteLine($"Created {providers.Length} providers (2 succeed, 2 fail)\n");

        // Demonstrate Continue policy (default)
        System.Console.WriteLine("🔄 Testing Continue Policy (default):");
        var continueOptions = FanOutAggregationOptions<string>.WithErrorPolicy(FanOutErrorPolicy.Continue);
        
        try
        {
            var continueResult = FanOutAggregator.Aggregate(
                providers,
                provider => ((IAnalyticsService)provider).ProcessEvent("user.login", new { userId = 123 }),
                continueOptions);
            
            System.Console.WriteLine($"   ✅ Continue policy result: {continueResult}");
        }
        catch (AggregateException ex)
        {
            System.Console.WriteLine($"   ⚠️  Continue policy collected {ex.InnerExceptions.Count} failures but returned successful results");
        }

        // Demonstrate FailFast policy
        System.Console.WriteLine("\n⚡ Testing FailFast Policy:");
        var failFastOptions = FanOutAggregationOptions<string>.WithErrorPolicy(FanOutErrorPolicy.FailFast);
        
        try
        {
            var failFastResult = FanOutAggregator.Aggregate(
                providers,
                provider => ((IAnalyticsService)provider).ProcessEvent("user.login", new { userId = 123 }),
                failFastOptions);
            
            System.Console.WriteLine($"   ✅ FailFast policy result: {failFastResult}");
        }
        catch (AggregateException ex)
        {
            System.Console.WriteLine($"   ❌ FailFast policy failed immediately: {ex.InnerExceptions.First().Message}");
        }

        // Demonstrate custom reduce function
        System.Console.WriteLine("\n🔗 Testing Custom Reduce Function:");
        var customOptions = FanOutAggregationOptions<string>.Create(
            FanOutErrorPolicy.Continue,
            results => $"Combined: [{string.Join(", ", results)}]");
        
        try
        {
            var customResult = FanOutAggregator.Aggregate(
                providers.Where(p => !((ConsoleAnalyticsService)p).ShouldFail), // Only successful providers
                provider => ((IAnalyticsService)provider).ProcessEvent("user.login", new { userId = 123 }),
                customOptions);
            
            System.Console.WriteLine($"   ✅ Custom reduce result: {customResult}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"   ❌ Custom reduce failed: {ex.Message}");
        }

        // Demonstrate void operations (fire-and-forget)
        System.Console.WriteLine("\n🔥 Testing Fire-and-Forget Operations:");
        
        System.Console.WriteLine("   Continue policy (void operations):");
        try
        {
            FanOutAggregator.ExecuteAll(
                providers,
                provider => ((IAnalyticsService)provider).TrackEvent("app.started", new { version = "1.0" }),
                FanOutErrorPolicy.Continue);
            System.Console.WriteLine("   ✅ Continue policy completed (some may have failed silently)");
        }
        catch (AggregateException ex)
        {
            System.Console.WriteLine($"   ⚠️  Continue policy completed with {ex.InnerExceptions.Count} failures");
        }

        System.Console.WriteLine("   FailFast policy (void operations):");
        try
        {
            FanOutAggregator.ExecuteAll(
                providers,
                provider => ((IAnalyticsService)provider).TrackEvent("app.started", new { version = "1.0" }),
                FanOutErrorPolicy.FailFast);
            System.Console.WriteLine("   ✅ FailFast policy completed successfully");
        }
        catch (AggregateException ex)
        {
            System.Console.WriteLine($"   ❌ FailFast policy failed immediately: {ex.InnerExceptions.First().Message}");
        }
    }
}

// Demo service interfaces and implementations

public interface IAnalyticsService
{
    void TrackEvent(string eventName, object data);
    string ProcessEvent(string eventName, object data);
}

public class ConsoleAnalyticsService : IAnalyticsService
{
    public string Name { get; }
    public bool ShouldFail { get; }
    public string FailureMessage { get; }

    public ConsoleAnalyticsService(string name = "Console", bool shouldFail = false, string failureMessage = "Operation failed")
    {
        Name = name;
        ShouldFail = shouldFail;
        FailureMessage = failureMessage;
    }

    public void TrackEvent(string eventName, object data)
    {
        if (ShouldFail)
            throw new InvalidOperationException($"[{Name}] {FailureMessage}");
        
        System.Console.WriteLine($"[{Name} Analytics] {eventName}: {data}");
    }

    public string ProcessEvent(string eventName, object data)
    {
        if (ShouldFail)
            throw new InvalidOperationException($"[{Name}] {FailureMessage}");
        
        var result = $"Processed {eventName} with {Name}";
        System.Console.WriteLine($"[{Name} Analytics] {result}");
        return result;
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