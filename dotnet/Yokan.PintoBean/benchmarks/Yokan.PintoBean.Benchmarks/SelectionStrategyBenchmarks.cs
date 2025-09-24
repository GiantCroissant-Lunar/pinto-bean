using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Benchmarks;

/// <summary>
/// Micro-benchmarks for selection strategy overhead:
/// - PickOne cache hit/miss scenarios
/// - FanOut with varying provider counts 
/// - Sharded routing performance
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[MarkdownExporter]
public class SelectionStrategyBenchmarks
{
    private IList<IProviderRegistration> _registrations = null!;
    private PickOneSelectionStrategy<IHelloService> _pickOneStrategy = null!;
    private FanOutSelectionStrategy<IHelloService> _fanOutStrategy = null!;
    private ShardedSelectionStrategy<IHelloService> _shardedStrategy = null!;
    private ISelectionContext<IHelloService> _selectionContext = null!;
    private ISelectionContext<IHelloService> _shardedContext = null!;
    
    // Pre-warmed cache context for hit scenarios
    private ISelectionContext<IHelloService> _cachedContext = null!;

    [Params(1, 5, 10, 20)]
    public int ProviderCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        SetupProviders();
        SetupStrategies();
        SetupContexts();
        WarmupCache();
    }

    private void SetupProviders()
    {
        _registrations = new List<IProviderRegistration>();
        
        // Create multiple providers with different priorities and capabilities
        for (int i = 0; i < ProviderCount; i++)
        {
            var provider = new BenchmarkHelloProvider($"provider-{i}");
            var capabilities = ProviderCapabilities.Create($"provider-{i}")
                .WithPriority(i == 0 ? Priority.High : Priority.Normal)
                .WithPlatform(Platform.Any)
                .WithTags("benchmark", "test")
                .AddMetadata("provider_type", "benchmark")
                .AddMetadata("index", i);

            _registrations.Add(new BenchmarkProviderRegistration
            {
                ServiceType = typeof(IHelloService),
                Provider = provider,
                Capabilities = capabilities,
                IsActive = true
            });
        }
    }

    private void SetupStrategies()
    {
        // Use no-op aspect runtime to avoid telemetry overhead
        var aspectRuntime = NoOpAspectRuntime.Instance;
        
        _pickOneStrategy = new PickOneSelectionStrategy<IHelloService>(
            aspectRuntime: aspectRuntime);
        
        _fanOutStrategy = new FanOutSelectionStrategy<IHelloService>(
            aspectRuntime: aspectRuntime);
            
        _shardedStrategy = new ShardedSelectionStrategy<IHelloService>(
            keyExtractor: metadata => metadata?.TryGetValue("shard_key", out var key) == true ? key?.ToString() ?? "default" : "default",
            aspectRuntime: aspectRuntime);
    }

    private void SetupContexts()
    {
        _selectionContext = new BenchmarkSelectionContext(_registrations);
        
        // Context with shard key for sharded strategy
        var shardedMetadata = new Dictionary<string, object>
        {
            ["shard_key"] = "player_123"
        };
        _shardedContext = new BenchmarkSelectionContext(_registrations, shardedMetadata);
        
        // Context for cache hit scenarios (different instance but same data)
        _cachedContext = new BenchmarkSelectionContext(_registrations);
    }

    private void WarmupCache()
    {
        // Warm up the cache for PickOne strategy to test cache hits
        _pickOneStrategy.SelectProviders(_cachedContext);
    }

    [Benchmark]
    public ISelectionResult<IHelloService> PickOne_CacheMiss()
    {
        return _pickOneStrategy.SelectProviders(_selectionContext);
    }

    [Benchmark]
    public ISelectionResult<IHelloService> PickOne_CacheHit()
    {
        return _pickOneStrategy.SelectProviders(_cachedContext);
    }

    [Benchmark]
    public ISelectionResult<IHelloService> FanOut_MultipleProviders()
    {
        return _fanOutStrategy.SelectProviders(_selectionContext);
    }

    [Benchmark]
    public ISelectionResult<IHelloService> Sharded_Routing()
    {
        return _shardedStrategy.SelectProviders(_shardedContext);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _pickOneStrategy?.Dispose();
        _fanOutStrategy?.Dispose();
        _shardedStrategy?.Dispose();
    }
}

/// <summary>
/// Minimal hello service provider for benchmarking performance.
/// </summary>
internal class BenchmarkHelloProvider : IHelloService
{
    public string ProviderId { get; }

    public BenchmarkHelloProvider(string providerId)
    {
        ProviderId = providerId;
    }

    public Task<HelloResponse> SayHelloAsync(HelloRequest request, System.Threading.CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HelloResponse
        {
            Message = $"Hello {request.Name}!",
            ServiceInfo = ProviderId,
            Language = request.Language ?? "en"
        });
    }

    public Task<HelloResponse> SayGoodbyeAsync(HelloRequest request, System.Threading.CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HelloResponse
        {
            Message = $"Goodbye {request.Name}!",
            ServiceInfo = ProviderId,
            Language = request.Language ?? "en"
        });
    }
}

/// <summary>
/// Minimal provider registration for benchmarking.
/// </summary>
internal class BenchmarkProviderRegistration : IProviderRegistration
{
    public Type ServiceType { get; init; } = null!;
    public object Provider { get; init; } = null!;
    public ProviderCapabilities Capabilities { get; init; } = null!;
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Minimal selection context for benchmarking.
/// </summary>
internal class BenchmarkSelectionContext : ISelectionContext<IHelloService>
{
    public Type ServiceType => typeof(IHelloService);
    public IReadOnlyList<IProviderRegistration> Registrations { get; }
    public IDictionary<string, object>? Metadata { get; }
    public System.Threading.CancellationToken CancellationToken => System.Threading.CancellationToken.None;

    public BenchmarkSelectionContext(IList<IProviderRegistration> registrations, IDictionary<string, object>? metadata = null)
    {
        Registrations = registrations.ToList();
        Metadata = metadata;
    }
}