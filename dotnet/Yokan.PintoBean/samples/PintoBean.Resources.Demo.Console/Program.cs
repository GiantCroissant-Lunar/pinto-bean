using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime;

namespace PintoBean.Resources.Demo.Console;

/// <summary>
/// P5-02: Resources sample demonstrating PickOne selection strategy with resilience fallback.
/// Shows deterministic single-provider selection with automatic fallback on failure.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        System.Console.WriteLine("=== P5-02: Resources Sample (PickOne + Fallback via Resilience) ===");
        System.Console.WriteLine("Demonstrating deterministic single-provider selection with fallback on failure\n");

        System.Console.WriteLine("🔧 Setting up Dependency Injection with Polly Resilience...");

        var services = new ServiceCollection();

        // Add resilience executor for fallback behavior
        services.AddPollyResilience();

        // Add service registry and selection strategies
        services.AddServiceRegistry(registry =>
        {
            // Register primary resource store with high priority
            var primaryStore = new PrimaryResourceStore();
            var primaryCapabilities = ProviderCapabilities.Create("primary-store")
                .WithPriority(Priority.High)
                .WithPlatform(Platform.Any);
            registry.Register<IResourceStore>(primaryStore, primaryCapabilities);

            // Register backup resource store with normal priority
            var backupStore = new BackupResourceStore();
            var backupCapabilities = ProviderCapabilities.Create("backup-store")
                .WithPriority(Priority.Normal)
                .WithPlatform(Platform.Any);
            registry.Register<IResourceStore>(backupStore, backupCapabilities);
        });

        services.AddSelectionStrategies();

        var host = services.BuildServiceProvider();

        // Demonstrate resource loading with resilience
        var registry = host.GetRequiredService<IServiceRegistry>();
        var typedRegistry = registry.For<IResourceStore>();
        var resilienceExecutor = host.GetRequiredService<IResilienceExecutor>();

        System.Console.WriteLine("\n🎯 Testing Resource Loading with PickOne Strategy:");

        try
        {
            var result = await typedRegistry.InvokeAsync(async (store, ct) =>
                await resilienceExecutor.ExecuteAsync(async (innerCt) => 
                    await store.LoadAsync("test-resource.json", innerCt), ct));

            System.Console.WriteLine($"✅ Successfully loaded resource: {result}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ Failed to load resource: {ex.Message}");
        }

        System.Console.WriteLine("\n✅ Demo completed successfully!");
    }
}

// Demo interfaces and implementations
public interface IResourceStore
{
    Task<string> LoadAsync(string resourceKey, CancellationToken cancellationToken = default);
}

public class PrimaryResourceStore : IResourceStore
{
    public async Task<string> LoadAsync(string resourceKey, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);
        System.Console.WriteLine($"[Primary Store] Loading resource: {resourceKey}");
        return $"Primary data for {resourceKey}";
    }
}

public class BackupResourceStore : IResourceStore
{
    public async Task<string> LoadAsync(string resourceKey, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        System.Console.WriteLine($"[Backup Store] Loading resource: {resourceKey}");
        return $"Backup data for {resourceKey}";
    }
}