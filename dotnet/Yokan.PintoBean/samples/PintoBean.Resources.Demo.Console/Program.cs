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
        services.AddPollyResilience(options =>
        {
            options.DefaultTimeoutSeconds = 2.0; // Short timeout for demo
            options.MaxRetryAttempts = 2; // Allow retries before giving up
            options.BaseRetryDelayMilliseconds = 100.0; // Quick retries
            options.EnableCircuitBreaker = false; // Keep simple for demo
        });

        // Register the service registry to manage providers
        services.AddServiceRegistry(registry =>
        {
            System.Console.WriteLine("📝 Registering resource store providers:");

            // Register primary resource store (high priority, can fail)
            var primaryStore = new NetworkResourceStore("NetworkStore");
            var primaryCapabilities = ProviderCapabilities.Create("network-resources")
                .WithPriority(Priority.High)
                .WithPlatform(Platform.Any)
                .WithTags("primary", "network", "fast");

            registry.Register<IResourceStore>(primaryStore, primaryCapabilities);
            System.Console.WriteLine($"  ✅ Registered: {primaryCapabilities.ProviderId} (Priority: {primaryCapabilities.Priority})");

            // Register fallback resource store (lower priority, reliable)
            var fallbackStore = new LocalFileResourceStore("LocalFileStore");
            var fallbackCapabilities = ProviderCapabilities.Create("local-file-resources")
                .WithPriority(Priority.Normal)
                .WithPlatform(Platform.Any)
                .WithTags("fallback", "local", "reliable");

            registry.Register<IResourceStore>(fallbackStore, fallbackCapabilities);
            System.Console.WriteLine($"  ✅ Registered: {fallbackCapabilities.ProviderId} (Priority: {fallbackCapabilities.Priority})");

            // Register cache store (highest priority when available)
            var cacheStore = new CacheResourceStore("CacheStore");
            var cacheCapabilities = ProviderCapabilities.Create("cache-resources")
                .WithPriority(Priority.Critical)
                .WithPlatform(Platform.Any)
                .WithTags("cache", "memory", "fastest");

            registry.Register<IResourceStore>(cacheStore, cacheCapabilities);
            System.Console.WriteLine($"  ✅ Registered: {cacheCapabilities.ProviderId} (Priority: {cacheCapabilities.Priority})");
        });

        // Configure selection strategies - Resources use PickOne by default
        services.AddSelectionStrategies();

        var serviceProvider = services.BuildServiceProvider();

        await DemonstrateResourceLoading(serviceProvider);
        
        System.Console.WriteLine("\n✅ P5-02 Resources demo completed successfully!");
    }

    private static async Task DemonstrateResourceLoading(IServiceProvider services)
    {
        var registry = services.GetRequiredService<IServiceRegistry>();
        var resilienceExecutor = services.GetRequiredService<IResilienceExecutor>();
        var typedRegistry = registry.For<IResourceStore>();

        System.Console.WriteLine("\n🔍 Demonstrating PickOne Selection Strategy with Resilience:");
        System.Console.WriteLine("   PickOne strategy selects the highest priority available provider");
        System.Console.WriteLine("   Resilience patterns handle transient failures within selected provider");
        System.Console.WriteLine("   For true fallback, providers can be unregistered/registered dynamically");

        // Test 1: Normal operation - should select cache store (highest priority)
        System.Console.WriteLine("\n1️⃣  Testing normal operation:");
        var playerConfig = await LoadResourceWithResilience(typedRegistry, resilienceExecutor, "player-config.json");
        System.Console.WriteLine($"   📄 Loaded: {playerConfig.ResourceKey} from {playerConfig.Source}");
        System.Console.WriteLine($"   📊 Content preview: {playerConfig.Content.Substring(0, Math.Min(50, playerConfig.Content.Length))}...");

        // Test 2: Transient failure with retry - same provider, but recovers
        System.Console.WriteLine("\n2️⃣  Testing transient failure with resilience (same provider retries):");
        CacheResourceStore.SimulateTransientFailure = true; // Fail first 1-2 attempts, then succeed
        
        var gameSettings = await LoadResourceWithResilience(typedRegistry, resilienceExecutor, "game-settings.json");
        System.Console.WriteLine($"   📄 Loaded: {gameSettings.ResourceKey} from {gameSettings.Source}");
        System.Console.WriteLine($"   📊 Content preview: {gameSettings.Content.Substring(0, Math.Min(50, gameSettings.Content.Length))}...");
        
        CacheResourceStore.SimulateTransientFailure = false; // Reset

        // Test 3: Provider fallback via dynamic registration (simulate cache unavailable)
        System.Console.WriteLine("\n3️⃣  Testing provider fallback (remove cache, network becomes primary):");
        
        // Find and remove the cache provider to simulate it being unavailable
        var cacheRegistration = typedRegistry.GetRegistrations()
            .FirstOrDefault(r => r.Capabilities.ProviderId == "cache-resources");
        
        if (cacheRegistration != null)
        {
            registry.Unregister(cacheRegistration);
            System.Console.WriteLine("   ➖ Removed cache provider (simulating service unavailable)");
        }
        
        var levelData = await LoadResourceWithResilience(typedRegistry, resilienceExecutor, "level-001.json");
        System.Console.WriteLine($"   📄 Loaded: {levelData.ResourceKey} from {levelData.Source}");
        System.Console.WriteLine($"   📊 Content preview: {levelData.Content.Substring(0, Math.Min(50, levelData.Content.Length))}...");

        // Test 4: Further fallback - remove network too, falls back to local file
        System.Console.WriteLine("\n4️⃣  Testing deeper fallback (network also fails, falls back to local):");
        
        var networkRegistration = typedRegistry.GetRegistrations()
            .FirstOrDefault(r => r.Capabilities.ProviderId == "network-resources");
        
        if (networkRegistration != null)
        {
            registry.Unregister(networkRegistration);
            System.Console.WriteLine("   ➖ Removed network provider (simulating network outage)");
        }
        
        var assetManifest = await LoadResourceWithResilience(typedRegistry, resilienceExecutor, "asset-manifest.json");
        System.Console.WriteLine($"   📄 Loaded: {assetManifest.ResourceKey} from {assetManifest.Source}");
        System.Console.WriteLine($"   📊 Content preview: {assetManifest.Content.Substring(0, Math.Min(50, assetManifest.Content.Length))}...");

        // Test 5: Recovery - re-register providers to show dynamic recovery
        System.Console.WriteLine("\n5️⃣  Testing service recovery (network restored):");
        
        // Re-register network provider
        var networkStore = new NetworkResourceStore("NetworkStore");
        var networkCapabilities = ProviderCapabilities.Create("network-resources")
            .WithPriority(Priority.High)
            .WithPlatform(Platform.Any)
            .WithTags("primary", "network", "fast");

        registry.Register<IResourceStore>(networkStore, networkCapabilities);
        System.Console.WriteLine("   ➕ Re-registered network provider (service recovered)");
        
        var recoveredData = await LoadResourceWithResilience(typedRegistry, resilienceExecutor, "recovered-data.json");
        System.Console.WriteLine($"   📄 Loaded: {recoveredData.ResourceKey} from {recoveredData.Source}");
        System.Console.WriteLine($"   📊 Content preview: {recoveredData.Content.Substring(0, Math.Min(50, recoveredData.Content.Length))}...");

        // Test 6: Total failure - all providers unavailable
        System.Console.WriteLine("\n6️⃣  Testing total failure (all providers unavailable):");
        
        // Remove all remaining providers
        var allRegistrations = typedRegistry.GetRegistrations().ToList();
        foreach (var reg in allRegistrations)
        {
            registry.Unregister(reg);
        }
        System.Console.WriteLine("   ➖ Removed all providers (simulating total service outage)");

        try
        {
            await LoadResourceWithResilience(typedRegistry, resilienceExecutor, "impossible-resource.json");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"   ❌ Expected failure: {ex.GetType().Name}: {ex.Message}");
            System.Console.WriteLine("   💡 This demonstrates that when no providers are available, the system fails gracefully");
        }
    }

    private static async Task<ResourceData> LoadResourceWithResilience(
        IServiceRegistry<IResourceStore> registry,
        IResilienceExecutor resilienceExecutor,
        string resourceKey)
    {
        var startTime = DateTime.UtcNow;
        
        var result = await registry.InvokeAsync(async (store, ct) =>
            await resilienceExecutor.ExecuteAsync(async (innerCt) =>
                await store.LoadResourceAsync(resourceKey, innerCt), ct));

        var elapsed = DateTime.UtcNow - startTime;
        System.Console.WriteLine($"   ⏱️  Load time: {elapsed.TotalMilliseconds:F0}ms");
        
        return result;
    }

}

// ===== Resource Store Interfaces and Models =====

/// <summary>
/// Resource store interface for loading various types of resources asynchronously.
/// This represents Tier-1 service contract as mentioned in the issue.
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

/// <summary>
/// Interface for simulating failures in demo providers.
/// </summary>
public interface IFailureSimulator
{
    void SetFailureMode(bool shouldFail);
}

// ===== Resource Store Implementations =====

/// <summary>
/// Cache-based resource store with highest priority.
/// </summary>
public class CacheResourceStore : IResourceStore, IFailureSimulator
{
    private readonly Dictionary<string, string> _cache = new();
    public static bool SimulateFailure = false;
    public static bool SimulateTransientFailure = false;
    private static int _attemptCount = 0;
    
    public string StoreName { get; }

    public CacheResourceStore(string name)
    {
        StoreName = name;
        
        // Pre-populate cache with some resources
        _cache["player-config.json"] = """
        {
            "playerName": "CachedPlayer",
            "level": 10,
            "experience": 1500,
            "source": "cache"
        }
        """;
    }

    public void SetFailureMode(bool shouldFail)
    {
        SimulateFailure = shouldFail;
    }

    public async Task<ResourceData> LoadResourceAsync(string resourceKey, CancellationToken cancellationToken = default)
    {
        if (SimulateFailure)
        {
            await Task.Delay(50, cancellationToken); // Simulate network delay before failure
            throw new InvalidOperationException($"Cache store failure simulated for {resourceKey}");
        }

        if (SimulateTransientFailure)
        {
            _attemptCount++;
            if (_attemptCount <= 1) // Fail on first attempt, succeed on retry
            {
                await Task.Delay(30, cancellationToken);
                throw new TimeoutException($"Transient cache timeout for {resourceKey} (attempt {_attemptCount})");
            }
            // Reset for next test
            if (_attemptCount >= 2)
            {
                _attemptCount = 0;
                SimulateTransientFailure = false;
            }
        }

        await Task.Delay(10, cancellationToken); // Simulate fast cache access
        
        // For demo purposes, assume cache can serve any requested resource
        if (!_cache.TryGetValue(resourceKey, out var content))
        {
            // Generate dynamic content for requested resource
            content = $$"""
            {
                "resourceKey": "{{resourceKey}}",
                "source": "cache",
                "cached": true,
                "timestamp": "{{DateTime.UtcNow:O}}",
                "data": "Cached content for {{resourceKey}}"
            }
            """;
        }
        
        return new ResourceData(resourceKey, content, StoreName, DateTime.UtcNow);
    }

    public async Task<bool> ResourceExistsAsync(string resourceKey, CancellationToken cancellationToken = default)
    {
        if (SimulateFailure) return false;
        
        await Task.Delay(5, cancellationToken);
        return true; // For demo, assume cache can provide any resource
    }
}

/// <summary>
/// Network-based resource store with high priority.
/// </summary>
public class NetworkResourceStore : IResourceStore, IFailureSimulator
{
    public static bool SimulateFailure = false;
    
    public string StoreName { get; }

    public NetworkResourceStore(string name)
    {
        StoreName = name;
    }

    public void SetFailureMode(bool shouldFail)
    {
        SimulateFailure = shouldFail;
    }

    public async Task<ResourceData> LoadResourceAsync(string resourceKey, CancellationToken cancellationToken = default)
    {
        if (SimulateFailure)
        {
            await Task.Delay(200, cancellationToken); // Simulate network timeout
            throw new HttpRequestException($"Network failure: Unable to load {resourceKey} from remote server");
        }

        await Task.Delay(100, cancellationToken); // Simulate network latency

        // Simulate loading from network
        var networkContent = $$"""
        {
            "resource": "{{resourceKey}}",
            "loadedFrom": "network",
            "timestamp": "{{DateTime.UtcNow:O}}",
            "version": "1.2.0",
            "data": "Network-sourced content for {{resourceKey}}"
        }
        """;

        return new ResourceData(resourceKey, networkContent, StoreName, DateTime.UtcNow);
    }

    public async Task<bool> ResourceExistsAsync(string resourceKey, CancellationToken cancellationToken = default)
    {
        if (SimulateFailure) return false;
        
        await Task.Delay(50, cancellationToken); // Network check delay
        return true; // For demo, assume network can provide any resource when not failing
    }
}

/// <summary>
/// Local file-based resource store with normal priority (reliable fallback).
/// </summary>
public class LocalFileResourceStore : IResourceStore, IFailureSimulator
{
    public static bool SimulateFailure = false;
    
    public string StoreName { get; }

    public LocalFileResourceStore(string name)
    {
        StoreName = name;
    }

    public void SetFailureMode(bool shouldFail)
    {
        SimulateFailure = shouldFail;
    }

    public async Task<ResourceData> LoadResourceAsync(string resourceKey, CancellationToken cancellationToken = default)
    {
        if (SimulateFailure)
        {
            await Task.Delay(50, cancellationToken);
            throw new IOException($"Local file system error: Cannot read {resourceKey}");
        }

        await Task.Delay(30, cancellationToken); // Simulate file I/O

        // Simulate loading from local file system
        var fileContent = $$"""
        {
            "resource": "{{resourceKey}}",
            "loadedFrom": "local-file",
            "path": "/local/resources/{{resourceKey}}",
            "lastModified": "{{DateTime.UtcNow.AddDays(-1):O}}",
            "data": "Local file content for {{resourceKey}} (fallback)"
        }
        """;

        return new ResourceData(resourceKey, fileContent, StoreName, DateTime.UtcNow);
    }

    public async Task<bool> ResourceExistsAsync(string resourceKey, CancellationToken cancellationToken = default)
    {
        if (SimulateFailure) return false;
        
        await Task.Delay(20, cancellationToken); // File check delay
        return true; // For demo, assume local files can provide any resource when not failing
    }
}