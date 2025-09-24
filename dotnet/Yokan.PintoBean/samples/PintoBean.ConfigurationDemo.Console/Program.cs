using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Yokan.PintoBean.Runtime;

namespace PintoBean.ConfigurationDemo.Console;

public class Program
{
    public static void Main(string[] args)
    {
        // Build a host with configuration and services
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register selection strategies with configuration binding
                services.AddSelectionStrategies(context.Configuration, "SelectionStrategies");
                
                // Register some example services
                services.AddTransient<IAnalyticsService, AnalyticsServiceA>();
                services.AddTransient<IAnalyticsService, AnalyticsServiceB>();
                services.AddTransient<IResourceService, ResourceServicePrimary>();
                services.AddTransient<IResourceService, ResourceServiceFallback>();
            })
            .Build();

        // Demonstrate the configuration binding
        var options = host.Services.GetRequiredService<SelectionStrategyOptions>();
        var iOptions = host.Services.GetRequiredService<IOptions<SelectionStrategyOptions>>();
        var registry = host.Services.GetRequiredService<IServiceRegistry>();

        System.Console.WriteLine("=== SelectionStrategyOptions Configuration Demo ===");
        System.Console.WriteLine();
        
        System.Console.WriteLine("Configuration loaded from appsettings.json:");
        System.Console.WriteLine($"  Analytics: {options.Analytics}");
        System.Console.WriteLine($"  Resources: {options.Resources}");
        System.Console.WriteLine($"  SceneFlow: {options.SceneFlow}");
        System.Console.WriteLine($"  AI: {options.AI}");
        System.Console.WriteLine();

        System.Console.WriteLine("IOptions pattern working:");
        System.Console.WriteLine($"  Same instance: {ReferenceEquals(options, iOptions.Value)}");
        System.Console.WriteLine();

        System.Console.WriteLine("Strategy resolution:");
        System.Console.WriteLine($"  Analytics default: {options.GetDefaultForCategory(ServiceCategory.Analytics)}");
        System.Console.WriteLine($"  Resources default: {options.GetDefaultForCategory(ServiceCategory.Resources)}");
        System.Console.WriteLine();

        System.Console.WriteLine("Service registrations:");
        System.Console.WriteLine($"  IAnalyticsService providers: {registry.GetRegistrations<IAnalyticsService>().Count()}");
        System.Console.WriteLine($"  IResourceService providers: {registry.GetRegistrations<IResourceService>().Count()}");
        System.Console.WriteLine();

        System.Console.WriteLine("Demo completed successfully!");
    }
}

// Example service contracts and implementations
public interface IAnalyticsService
{
    string Name { get; }
}

public class AnalyticsServiceA : IAnalyticsService
{
    public string Name => "Analytics Service A";
}

public class AnalyticsServiceB : IAnalyticsService
{
    public string Name => "Analytics Service B";
}

public interface IResourceService
{
    string Name { get; }
}

public class ResourceServicePrimary : IResourceService
{
    public string Name => "Primary Resource Service";
}

public class ResourceServiceFallback : IResourceService
{
    public string Name => "Fallback Resource Service";
}