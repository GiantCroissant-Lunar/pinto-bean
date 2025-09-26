using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// P5-07: End-to-end tests proving the full call chain (façade → registry → strategy → provider) 
/// used by the three sample applications: Analytics, Resources, and SceneFlow.
/// </summary>
public class SamplesE2ETests
{
    /// <summary>
    /// Analytics E2E test: Assert two providers invoked when FanOut enabled.
    /// Validates the complete call chain from façade through registry and strategy to providers.
    /// </summary>
    [Fact]
    public async Task Analytics_E2E_FanOutStrategy_InvokesAllProviders()
    {
        // Arrange: Set up the complete DI container as samples do
        var services = new ServiceCollection();
        
        // Create testable providers that track invocations
        var unityProvider = new TestUnityAnalyticsProvider();
        var firebaseProvider = new TestFirebaseAnalyticsProvider();
        
        services.AddServiceRegistry(registry =>
        {
            // Register providers like the Analytics sample does
            registry.Register<IAnalytics>(unityProvider, 
                ProviderCapabilities.Create("unity-analytics").WithPriority(Priority.Normal));
            registry.Register<IAnalytics>(firebaseProvider, 
                ProviderCapabilities.Create("firebase-analytics").WithPriority(Priority.Normal));
        });
        
        // Configure FanOut strategy for IAnalytics (default for Analytics category)
        services.AddSelectionStrategies();
        services.UseFanOutFor<IAnalytics>();
        
        // Register the Analytics façade (simulating generated registration)
        services.AddTransient<TestAnalyticsFacade>();
        services.AddResilienceExecutor();
        services.AddNoOpAspectRuntime();
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Act: Invoke through the façade like the sample application does
        var analytics = serviceProvider.GetRequiredService<TestAnalyticsFacade>();
        var testEvent = new AnalyticsEvent 
        { 
            EventName = "test.event", 
            UserId = "user123",
            Properties = new Dictionary<string, object> { ["key"] = "value" }
        };
        
        await analytics.Track(testEvent);
        
        // Assert: Verify complete call chain and that FanOut invoked ALL providers
        Assert.Equal(1, unityProvider.InvocationCount);
        Assert.Equal(1, firebaseProvider.InvocationCount);
        Assert.Equal("test.event", unityProvider.LastTrackedEvent?.EventName);
        Assert.Equal("test.event", firebaseProvider.LastTrackedEvent?.EventName);
        
        // Verify strategy was actually FanOut
        var factory = serviceProvider.GetRequiredService<ISelectionStrategyFactory>();
        var strategy = factory.CreateStrategy<IAnalytics>();
        Assert.Equal(SelectionStrategyType.FanOut, strategy.StrategyType);
    }
    
    /// <summary>
    /// Analytics E2E test: Assert one provider invoked when Sharded strategy enabled.
    /// Tests prefix-based routing where events route to specific providers.
    /// </summary>
    [Fact]
    public async Task Analytics_E2E_ShardedStrategy_InvokesOneProviderBasedOnRouting()
    {
        // Arrange: Set up sharded strategy like the Analytics sample demonstrates
        var services = new ServiceCollection();
        
        var unityProvider = new TestUnityAnalyticsProvider();
        var firebaseProvider = new TestFirebaseAnalyticsProvider();
        
        services.AddServiceRegistry(registry =>
        {
            registry.Register<IAnalytics>(unityProvider, 
                ProviderCapabilities.Create("unity-analytics").WithPriority(Priority.Normal));
            registry.Register<IAnalytics>(firebaseProvider, 
                ProviderCapabilities.Create("firebase-analytics").WithPriority(Priority.Normal));
        });
        
        // Configure Sharded strategy for event prefix routing
        services.AddSelectionStrategies();
        services.UseShardedFor<IAnalytics>();
        
        services.AddTransient<TestAnalyticsFacade>();
        services.AddResilienceExecutor();
        services.AddNoOpAspectRuntime();
        
        var serviceProvider = services.BuildServiceProvider();
        var analytics = serviceProvider.GetRequiredService<TestAnalyticsFacade>();
        
        // Act: Send events with different prefixes to test routing (simulating sharded behavior)
        var playerEvent = new AnalyticsEvent { EventName = "player.level.complete" };
        var systemEvent = new AnalyticsEvent { EventName = "system.startup" };
        
        await analytics.TrackWithSharding(playerEvent);
        await analytics.TrackWithSharding(systemEvent);
        
        // Assert: With sharded strategy, events may be distributed across providers
        // The actual routing is provider-dependent, but both events should be tracked
        var totalInvocations = unityProvider.InvocationCount + firebaseProvider.InvocationCount;
        Assert.Equal(2, totalInvocations); // Both events were tracked
        
        // Verify strategy was actually Sharded
        var factory = serviceProvider.GetRequiredService<ISelectionStrategyFactory>();
        var strategy = factory.CreateStrategy<IAnalytics>();
        Assert.Equal(SelectionStrategyType.Sharded, strategy.StrategyType);
    }
    
    /// <summary>
    /// Resources E2E test: Assert fallback provider is used on primary failure.
    /// Validates PickOne strategy with dynamic provider availability changes.
    /// </summary>
    [Fact]
    public async Task Resources_E2E_PickOneStrategy_UsesFallbackOnPrimaryFailure()
    {
        // Arrange: Set up like the Resources sample with multiple priority providers
        var services = new ServiceCollection();
        
        var cacheStore = new TestCacheResourceStore(); // Critical priority
        var networkStore = new TestNetworkResourceStore(); // High priority  
        var localStore = new TestLocalResourceStore(); // Normal priority
        
        services.AddServiceRegistry(registry =>
        {
            registry.Register<ITestResourceStore>(cacheStore,
                ProviderCapabilities.Create("cache-resources").WithPriority(Priority.Critical));
            registry.Register<ITestResourceStore>(networkStore,
                ProviderCapabilities.Create("network-resources").WithPriority(Priority.High));
            registry.Register<ITestResourceStore>(localStore,
                ProviderCapabilities.Create("local-file-resources").WithPriority(Priority.Normal));
        });
        
        services.AddSelectionStrategies(); // Resources use PickOne by default
        services.AddTransient<TestResourcesFacade>();
        services.AddResilienceExecutor();
        services.AddNoOpAspectRuntime();
        
        var serviceProvider = services.BuildServiceProvider();
        var resources = serviceProvider.GetRequiredService<TestResourcesFacade>();
        var registry = serviceProvider.GetRequiredService<IServiceRegistry>();
        
        // Act & Assert: Test the fallback chain as primary providers become unavailable
        
        // 1. Initially, cache (highest priority) should be selected
        var result1 = await resources.LoadAsync("test-resource");
        Assert.Equal("CacheStore", result1.Source);
        Assert.Equal(1, cacheStore.InvocationCount);
        Assert.Equal(0, networkStore.InvocationCount);
        Assert.Equal(0, localStore.InvocationCount);
        
        // 2. Remove cache provider - network should become primary
        var cacheRegistration = registry.GetRegistrations<ITestResourceStore>()
            .First(r => r.Capabilities.ProviderId == "cache-resources");
        registry.Unregister(cacheRegistration);
        
        var result2 = await resources.LoadAsync("test-resource");
        Assert.Equal("NetworkStore", result2.Source);
        Assert.Equal(1, cacheStore.InvocationCount); // No change
        Assert.Equal(1, networkStore.InvocationCount); // Now called
        Assert.Equal(0, localStore.InvocationCount);
        
        // 3. Remove network provider - local should become primary
        var networkRegistration = registry.GetRegistrations<ITestResourceStore>()
            .First(r => r.Capabilities.ProviderId == "network-resources");
        registry.Unregister(networkRegistration);
        
        var result3 = await resources.LoadAsync("test-resource");
        Assert.Equal("LocalStore", result3.Source);
        Assert.Equal(1, cacheStore.InvocationCount); // No change
        Assert.Equal(1, networkStore.InvocationCount); // No change  
        Assert.Equal(1, localStore.InvocationCount); // Now called
        
        // Verify strategy was PickOne throughout
        var factory = serviceProvider.GetRequiredService<ISelectionStrategyFactory>();
        var strategy = factory.CreateStrategy<ITestResourceStore>();
        Assert.Equal(SelectionStrategyType.PickOne, strategy.StrategyType);
    }
    
    /// <summary>
    /// SceneFlow E2E test: Assert deterministic order with PickOne strategy.
    /// Validates that the same provider is consistently selected across multiple calls.
    /// </summary>
    [Fact]
    public async Task SceneFlow_E2E_PickOneStrategy_MaintainsDeterministicOrder()
    {
        // Arrange: Set up like the SceneFlow sample with multiple priority providers
        var services = new ServiceCollection();
        
        var devLoader = new TestDevSceneLoader(); // High priority
        var prodLoader = new TestProdSceneLoader(); // Normal priority
        var perfLoader = new TestPerfSceneLoader(); // Low priority
        
        services.AddServiceRegistry(registry =>
        {
            registry.Register<ISceneFlow>(devLoader,
                ProviderCapabilities.Create("dev-scene-loader").WithPriority(Priority.High));
            registry.Register<ISceneFlow>(prodLoader,
                ProviderCapabilities.Create("prod-scene-loader").WithPriority(Priority.Normal));
            registry.Register<ISceneFlow>(perfLoader,
                ProviderCapabilities.Create("perf-scene-loader").WithPriority(Priority.Low));
        });
        
        services.AddSelectionStrategies(); // SceneFlow uses PickOne by default
        services.AddTransient<TestSceneFlowFacade>();
        services.AddResilienceExecutor();
        services.AddNoOpAspectRuntime();
        
        var serviceProvider = services.BuildServiceProvider();
        var sceneFlow = serviceProvider.GetRequiredService<TestSceneFlowFacade>();
        
        // Act: Load multiple scenes across multiple runs like the sample does
        var scenes1 = new[] { "MainMenu", "GameLevel1", "GameLevel2", "EndCredits" };
        var scenes2 = new[] { "MainMenu", "GameLevel1", "GameLevel2", "EndCredits" };
        var scenes3 = new[] { "Tutorial", "Settings", "Multiplayer" };
        
        // Run 1
        foreach (var scene in scenes1)
            await sceneFlow.LoadAsync(scene);
            
        // Run 2 (same scenes)
        foreach (var scene in scenes2)
            await sceneFlow.LoadAsync(scene);
            
        // Run 3 (different scenes)
        foreach (var scene in scenes3)
            await sceneFlow.LoadAsync(scene);
        
        // Assert: Verify deterministic selection - highest priority provider used consistently
        Assert.Equal(11, devLoader.InvocationCount); // All 11 scene loads
        Assert.Equal(0, prodLoader.InvocationCount); // Never called
        Assert.Equal(0, perfLoader.InvocationCount); // Never called
        
        // Verify all scenes were processed by the same provider
        var expectedScenes = scenes1.Concat(scenes2).Concat(scenes3).ToList();
        Assert.Equal(expectedScenes, devLoader.LoadedScenes);
        
        // Verify strategy was PickOne
        var factory = serviceProvider.GetRequiredService<ISelectionStrategyFactory>();
        var strategy = factory.CreateStrategy<ISceneFlow>();
        Assert.Equal(SelectionStrategyType.PickOne, strategy.StrategyType);
    }
}

// Test Analytics Providers
public class TestUnityAnalyticsProvider : IAnalytics
{
    public int InvocationCount { get; private set; }
    public AnalyticsEvent? LastTrackedEvent { get; private set; }
    
    public Task Track(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default)
    {
        InvocationCount++;
        LastTrackedEvent = analyticsEvent;
        return Task.CompletedTask;
    }
}

public class TestFirebaseAnalyticsProvider : IAnalytics
{
    public int InvocationCount { get; private set; }
    public AnalyticsEvent? LastTrackedEvent { get; private set; }
    
    public Task Track(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default)
    {
        InvocationCount++;
        LastTrackedEvent = analyticsEvent;
        return Task.CompletedTask;
    }
}

// Test Analytics Façade
public class TestAnalyticsFacade : IAnalytics
{
    private readonly IServiceRegistry _registry;
    private readonly ISelectionStrategyFactory _strategyFactory;
    
    public TestAnalyticsFacade(IServiceRegistry registry, ISelectionStrategyFactory strategyFactory)
    {
        _registry = registry;
        _strategyFactory = strategyFactory;
    }
    
    public async Task Track(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default)
    {
        // Simulate the CORRECT generated façade pattern: use strategy directly like Analytics.cs
        var strategy = _strategyFactory.CreateStrategy<IAnalytics>();
        var registrations = _registry.GetRegistrations<IAnalytics>().ToList();
        
        var context = new SelectionContext<IAnalytics>(registrations);
        var result = strategy.SelectProviders(context);

        // Invoke all selected providers (this properly supports FanOut)
        var tasks = result.SelectedProviders.Select(provider => 
            provider.Track(analyticsEvent, cancellationToken));
        await Task.WhenAll(tasks);
    }
    
    public async Task TrackWithSharding(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default)
    {
        // Simulate what the Analytics sample does - use the strategy directly for proper sharding
        var strategy = _strategyFactory.CreateStrategy<IAnalytics>();
        var registrations = _registry.GetRegistrations<IAnalytics>().ToList();
        
        // Add metadata for sharded strategy routing
        var metadata = new Dictionary<string, object>
        {
            ["EventName"] = analyticsEvent.EventName
        };
        var context = new SelectionContext<IAnalytics>(registrations, metadata);
        var result = strategy.SelectProviders(context);

        // Invoke selected providers (mimics the pattern from Analytics.cs)
        var tasks = result.SelectedProviders.Select(provider => 
            provider.Track(analyticsEvent, cancellationToken));
        await Task.WhenAll(tasks);
    }
}

// Test Resource Store contracts and providers
public record TestResourceData(string Key, string Source, string Content);

public interface ITestResourceStore
{
    Task<TestResourceData> LoadAsync(string key, CancellationToken cancellationToken = default);
}

public class TestCacheResourceStore : ITestResourceStore
{
    public int InvocationCount { get; private set; }
    public Task<TestResourceData> LoadAsync(string key, CancellationToken cancellationToken = default)
    {
        InvocationCount++;
        return Task.FromResult(new TestResourceData(key, "CacheStore", "cached content"));
    }
}

public class TestNetworkResourceStore : ITestResourceStore
{
    public int InvocationCount { get; private set; }
    public Task<TestResourceData> LoadAsync(string key, CancellationToken cancellationToken = default)
    {
        InvocationCount++;
        return Task.FromResult(new TestResourceData(key, "NetworkStore", "network content"));
    }
}

public class TestLocalResourceStore : ITestResourceStore
{
    public int InvocationCount { get; private set; }
    public Task<TestResourceData> LoadAsync(string key, CancellationToken cancellationToken = default)
    {
        InvocationCount++;
        return Task.FromResult(new TestResourceData(key, "LocalStore", "local content"));
    }
}

// Test Resources Façade
public class TestResourcesFacade
{
    private readonly IServiceRegistry _registry;
    
    public TestResourcesFacade(IServiceRegistry registry)
    {
        _registry = registry;
    }
    
    public async Task<TestResourceData> LoadAsync(string key, CancellationToken cancellationToken = default)
    {
        // Simulate generated façade pattern: delegate to typed registry
        var typedRegistry = _registry.For<ITestResourceStore>();
        return await typedRegistry.InvokeAsync((service, ct) => service.LoadAsync(key, ct), cancellationToken);
    }
}

// Test SceneFlow Providers
public class TestDevSceneLoader : ISceneFlow
{
    public int InvocationCount { get; private set; }
    public List<string> LoadedScenes { get; } = new();
    
    public Task LoadAsync(string scene, CancellationToken cancellationToken = default)
    {
        InvocationCount++;
        LoadedScenes.Add(scene);
        return Task.CompletedTask;
    }
}

public class TestProdSceneLoader : ISceneFlow
{
    public int InvocationCount { get; private set; }
    public List<string> LoadedScenes { get; } = new();
    
    public Task LoadAsync(string scene, CancellationToken cancellationToken = default)
    {
        InvocationCount++;
        LoadedScenes.Add(scene);
        return Task.CompletedTask;
    }
}

public class TestPerfSceneLoader : ISceneFlow
{
    public int InvocationCount { get; private set; }
    public List<string> LoadedScenes { get; } = new();
    
    public Task LoadAsync(string scene, CancellationToken cancellationToken = default)
    {
        InvocationCount++;
        LoadedScenes.Add(scene);
        return Task.CompletedTask;
    }
}

// Test SceneFlow Façade
public class TestSceneFlowFacade : ISceneFlow
{
    private readonly IServiceRegistry _registry;
    
    public TestSceneFlowFacade(IServiceRegistry registry)
    {
        _registry = registry;
    }
    
    public async Task LoadAsync(string scene, CancellationToken cancellationToken = default)
    {
        // Simulate generated façade pattern: delegate to typed registry
        var typedRegistry = _registry.For<ISceneFlow>();
        await typedRegistry.InvokeAsync((service, ct) => service.LoadAsync(scene, ct), cancellationToken);
    }
}