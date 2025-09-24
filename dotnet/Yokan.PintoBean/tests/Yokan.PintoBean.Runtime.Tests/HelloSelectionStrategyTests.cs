// P2-08: Strategy tests using HelloProviderA/B for IHelloService

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for selection strategies using the IHelloService sample providers.
/// Validates PickOne, FanOut, and Sharded strategies with HelloProviderA/B.
/// </summary>
public class HelloSelectionStrategyTests
{
    [Fact]
    public async Task PickOneStrategy_WithIHelloService_SelectsHighestPriorityProvider()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var setup = HelloTestProviderHelpers.RegisterTestProviders(registry);
        
        var context = new SelectionContext<IHelloService>(setup.Registrations);
        var strategy = DefaultSelectionStrategies.CreatePickOne<IHelloService>();

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.Single(result.SelectedProviders);
        Assert.Equal(SelectionStrategyType.PickOne, result.StrategyType);
        
        // Should select the critical provider (highest priority)
        var selectedProvider = result.SelectedProviders.First();
        var request = new HelloRequest { Name = "Test" };
        var response = await selectedProvider.SayHelloAsync(request);
        
        Assert.Contains("Provider B", response.Message);
        Assert.Equal("provider-b-critical", response.ServiceInfo);
        
        // Verify only the critical provider was selected
        Assert.Equal(1, setup.CriticalB.CallCount);
        Assert.Equal(0, setup.HighPriorityA.CallCount);
        Assert.Equal(0, setup.NormalPriorityB.CallCount);
        Assert.Equal(0, setup.FallbackA.CallCount);
    }

    [Fact]
    public async Task PickOneStrategy_WithCapabilityFilter_SelectsMatchingProvider()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var setup = HelloTestProviderHelpers.RegisterTestProviders(registry);
        
        var metadata = new Dictionary<string, object>
        {
            ["RequiredTags"] = new[] { "analytics" }
        };
        var context = new SelectionContext<IHelloService>(setup.Registrations, metadata);
        var strategy = DefaultSelectionStrategies.CreatePickOne<IHelloService>();

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.Single(result.SelectedProviders);
        
        // Should select the high priority analytics provider
        var selectedProvider = result.SelectedProviders.First();
        var request = new HelloRequest { Name = "Analytics" };
        var response = await selectedProvider.SayHelloAsync(request);
        
        Assert.Contains("Provider A", response.Message);
        Assert.Equal("provider-a-high", response.ServiceInfo);
        
        // Verify the correct provider was selected
        Assert.Equal(1, setup.HighPriorityA.CallCount);
        Assert.Equal(0, setup.CriticalB.CallCount);
    }

    [Fact] 
    public async Task PickOneStrategy_WithPriorityTieBreak_SelectsDeterministically()
    {
        // Arrange - Create two providers with same priority
        var registry = new ServiceRegistry();
        var (providerA, capabilitiesA) = HelloTestProviderHelpers.CreateNormalPriorityProviderB("provider-same-1");
        var (providerB, capabilitiesB) = HelloTestProviderHelpers.CreateNormalPriorityProviderB("provider-same-2");
        
        var registrations = new List<IProviderRegistration>
        {
            registry.Register<IHelloService>(providerA, capabilitiesA),
            registry.Register<IHelloService>(providerB, capabilitiesB)
        };
        
        var context = new SelectionContext<IHelloService>(registrations);
        var strategy = DefaultSelectionStrategies.CreatePickOne<IHelloService>();

        // Act - Run multiple times to verify deterministic behavior
        var results = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            var result = strategy.SelectProviders(context);
            var selectedProvider = result.SelectedProviders.First();
            var request = new HelloRequest { Name = "Test" };
            var response = await selectedProvider.SayHelloAsync(request);
            results.Add(response.ServiceInfo!);
        }

        // Assert - Should always select the same provider
        Assert.True(results.All(r => r == results[0]), "PickOne selection should be deterministic");
        Assert.Equal(SelectionStrategyType.PickOne, strategy.StrategyType);
    }

    [Fact]
    public async Task FanOutStrategy_WithIHelloService_SelectsAllProviders()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var setup = HelloTestProviderHelpers.RegisterTestProviders(registry);
        
        var context = new SelectionContext<IHelloService>(setup.Registrations);
        var strategy = DefaultSelectionStrategies.CreateFanOut<IHelloService>();

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.Equal(6, result.SelectedProviders.Count); // All 6 registered providers
        Assert.Equal(SelectionStrategyType.FanOut, result.StrategyType);
        Assert.NotNull(result.SelectionMetadata);
        Assert.Equal(6, result.SelectionMetadata["ProviderCount"]);

        // Test that all providers can be invoked
        var request = new HelloRequest { Name = "FanOut" };
        var responses = new List<HelloResponse>();
        
        foreach (var provider in result.SelectedProviders)
        {
            var response = await provider.SayHelloAsync(request);
            responses.Add(response);
        }

        // Verify all providers responded
        Assert.Equal(6, responses.Count);
        var serviceInfos = responses.Select(r => r.ServiceInfo).ToHashSet();
        Assert.Contains("provider-a-high", serviceInfos);
        Assert.Contains("provider-b-normal", serviceInfos);
        Assert.Contains("provider-a-fallback", serviceInfos);
        Assert.Contains("provider-b-critical", serviceInfos);
        Assert.Contains("provider-a-analytics", serviceInfos);
        Assert.Contains("provider-b-game", serviceInfos);
    }

    [Fact]
    public async Task FanOutStrategy_WithCapabilityFilter_SelectsMatchingProviders()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var setup = HelloTestProviderHelpers.RegisterTestProviders(registry);
        
        var metadata = new Dictionary<string, object>
        {
            ["RequiredTags"] = new[] { "analytics" }
        };
        var context = new SelectionContext<IHelloService>(setup.Registrations, metadata);
        var strategy = DefaultSelectionStrategies.CreateFanOut<IHelloService>();

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.Equal(3, result.SelectedProviders.Count); // 3 providers with analytics tag
        Assert.Equal(SelectionStrategyType.FanOut, result.StrategyType);
        
        // Test that all analytics providers can be invoked
        var request = new HelloRequest { Name = "Analytics" };
        var responses = new List<HelloResponse>();
        
        foreach (var provider in result.SelectedProviders)
        {
            var response = await provider.SayHelloAsync(request);
            responses.Add(response);
        }

        // Verify only analytics providers responded
        var serviceInfos = responses.Select(r => r.ServiceInfo).ToHashSet();
        Assert.Contains("provider-a-high", serviceInfos);  // has analytics tag
        Assert.Contains("provider-a-analytics", serviceInfos);  // has analytics tag
        Assert.Contains("provider-b-game", serviceInfos);  // has analytics tag
        Assert.DoesNotContain("provider-b-normal", serviceInfos);  // no analytics tag
        Assert.DoesNotContain("provider-a-fallback", serviceInfos);  // no analytics tag
    }

    [Fact]
    public async Task FanOutStrategy_WithMultipleTagRequirements_SelectsProvidersWithAllTags()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var setup = HelloTestProviderHelpers.RegisterTestProviders(registry);
        
        var metadata = new Dictionary<string, object>
        {
            ["RequiredTags"] = new[] { "analytics", "events" }
        };
        var context = new SelectionContext<IHelloService>(setup.Registrations, metadata);
        var strategy = DefaultSelectionStrategies.CreateFanOut<IHelloService>();

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.Equal(2, result.SelectedProviders.Count); // Only providers with both tags
        Assert.Equal(SelectionStrategyType.FanOut, result.StrategyType);
        
        // Test the selected providers
        var request = new HelloRequest { Name = "Events" };
        var responses = new List<HelloResponse>();
        
        foreach (var provider in result.SelectedProviders)
        {
            var response = await provider.SayHelloAsync(request);
            responses.Add(response);
        }

        // Verify only providers with both analytics and events tags responded
        var serviceInfos = responses.Select(r => r.ServiceInfo).ToHashSet();
        Assert.Contains("provider-a-analytics", serviceInfos);  // has both tags
        Assert.Contains("provider-b-game", serviceInfos);  // has both tags
    }

    [Fact]
    public async Task ShardedStrategy_WithPlayerEventPrefix_SelectsPlayerProvider()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var setup = HelloTestProviderHelpers.RegisterTestProviders(registry);
        
        var metadata = new Dictionary<string, object>
        {
            ["EventName"] = "player.level.completed"
        };
        var context = new SelectionContext<IHelloService>(setup.Registrations, metadata);
        var strategy = DefaultSelectionStrategies.CreateAnalyticsSharded<IHelloService>();

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.Single(result.SelectedProviders);
        Assert.Equal(SelectionStrategyType.Sharded, result.StrategyType);
        Assert.NotNull(result.SelectionMetadata);
        Assert.Equal("player", result.SelectionMetadata["ShardKey"]);
        
        // Test the selected provider
        var selectedProvider = result.SelectedProviders.First();
        var request = new HelloRequest { Name = "Player" };
        var response = await selectedProvider.SayHelloAsync(request);
        
        // Should consistently select the same provider for player events
        Assert.NotNull(response.ServiceInfo);
    }

    [Fact] 
    public async Task ShardedStrategy_WithGameEventPrefix_SelectsGameProvider()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var setup = HelloTestProviderHelpers.RegisterTestProviders(registry);
        
        var metadata = new Dictionary<string, object>
        {
            ["EventName"] = "game.session.started"
        };
        var context = new SelectionContext<IHelloService>(setup.Registrations, metadata);
        var strategy = DefaultSelectionStrategies.CreateAnalyticsSharded<IHelloService>();

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.Single(result.SelectedProviders);
        Assert.Equal(SelectionStrategyType.Sharded, result.StrategyType);
        Assert.NotNull(result.SelectionMetadata);
        Assert.Equal("game", result.SelectionMetadata["ShardKey"]);
        
        // Test the selected provider
        var selectedProvider = result.SelectedProviders.First();
        var request = new HelloRequest { Name = "Game" };
        var response = await selectedProvider.SayHelloAsync(request);
        
        Assert.NotNull(response.ServiceInfo);
    }

    [Fact]
    public async Task ShardedStrategy_WithConsistentHashing_SelectsSameProviderForSameKey()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var setup = HelloTestProviderHelpers.RegisterTestProviders(registry);
        
        var metadata = new Dictionary<string, object>
        {
            ["EventName"] = "player.achievement.unlocked"
        };
        var context = new SelectionContext<IHelloService>(setup.Registrations, metadata);
        var strategy = DefaultSelectionStrategies.CreateAnalyticsSharded<IHelloService>();

        // Act - Run multiple times to verify consistent selection
        var results = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            var result = strategy.SelectProviders(context);
            var selectedProvider = result.SelectedProviders.First();
            var request = new HelloRequest { Name = "Consistency" };
            var response = await selectedProvider.SayHelloAsync(request);
            results.Add(response.ServiceInfo!);
        }

        // Assert - Should always select the same provider for the same shard key
        Assert.True(results.All(r => r == results[0]), "Sharded selection should be consistent for the same shard key");
        Assert.Equal(SelectionStrategyType.Sharded, strategy.StrategyType);
    }

    [Fact]
    public async Task ShardedStrategy_WithExplicitShardKey_UsesDirectShardKey()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var setup = HelloTestProviderHelpers.RegisterTestProviders(registry);
        
        var metadata = new Dictionary<string, object>
        {
            ["ShardKey"] = "custom-analytics-shard"
        };
        var context = new SelectionContext<IHelloService>(setup.Registrations, metadata);
        var strategy = DefaultSelectionStrategies.CreateAnalyticsSharded<IHelloService>();

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.Single(result.SelectedProviders);
        Assert.Equal(SelectionStrategyType.Sharded, result.StrategyType);
        Assert.NotNull(result.SelectionMetadata);
        Assert.Equal("custom-analytics-shard", result.SelectionMetadata["ShardKey"]);
        
        // Test the selected provider
        var selectedProvider = result.SelectedProviders.First();
        var request = new HelloRequest { Name = "Custom" };
        var response = await selectedProvider.SayHelloAsync(request);
        
        Assert.NotNull(response.ServiceInfo);
    }

    [Fact]
    public async Task AllStrategies_WithHelloProviders_ProduceValidResponses()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var setup = HelloTestProviderHelpers.RegisterTestProviders(registry);
        
        var context = new SelectionContext<IHelloService>(setup.Registrations);
        var pickOneStrategy = DefaultSelectionStrategies.CreatePickOne<IHelloService>();
        var fanOutStrategy = DefaultSelectionStrategies.CreateFanOut<IHelloService>();
        
        var shardedContext = new SelectionContext<IHelloService>(
            setup.Registrations, 
            new Dictionary<string, object> { ["EventName"] = "player.login" });
        var shardedStrategy = DefaultSelectionStrategies.CreateAnalyticsSharded<IHelloService>();

        // Act & Assert for PickOne
        var pickOneResult = pickOneStrategy.SelectProviders(context);
        Assert.Single(pickOneResult.SelectedProviders);
        
        var pickOneResponse = await pickOneResult.SelectedProviders.First()
            .SayHelloAsync(new HelloRequest { Name = "PickOne" });
        Assert.NotNull(pickOneResponse.Message);
        Assert.NotNull(pickOneResponse.ServiceInfo);

        // Act & Assert for FanOut
        var fanOutResult = fanOutStrategy.SelectProviders(context);
        Assert.Equal(6, fanOutResult.SelectedProviders.Count);
        
        foreach (var provider in fanOutResult.SelectedProviders)
        {
            var fanOutResponse = await provider.SayHelloAsync(new HelloRequest { Name = "FanOut" });
            Assert.NotNull(fanOutResponse.Message);
            Assert.NotNull(fanOutResponse.ServiceInfo);
        }

        // Act & Assert for Sharded
        var shardedResult = shardedStrategy.SelectProviders(shardedContext);
        Assert.Single(shardedResult.SelectedProviders);
        
        var shardedResponse = await shardedResult.SelectedProviders.First()
            .SayHelloAsync(new HelloRequest { Name = "Sharded" });
        Assert.NotNull(shardedResponse.Message);
        Assert.NotNull(shardedResponse.ServiceInfo);
    }

    [Fact]
    public void HelloTestProviderHelpers_CreateProviders_WithCorrectCapabilities()
    {
        // Test HighPriorityProviderA
        var (providerA, capabilitiesA) = HelloTestProviderHelpers.CreateHighPriorityProviderA();
        Assert.Equal(Priority.High, capabilitiesA.Priority);
        Assert.True(capabilitiesA.HasTags("analytics", "primary", "greeting"));
        Assert.Equal("A", capabilitiesA.Metadata["provider_type"]);

        // Test NormalPriorityProviderB  
        var (providerB, capabilitiesB) = HelloTestProviderHelpers.CreateNormalPriorityProviderB();
        Assert.Equal(Priority.Normal, capabilitiesB.Priority);
        Assert.True(capabilitiesB.HasTags("telemetry", "secondary", "greeting"));
        Assert.Equal("B", capabilitiesB.Metadata["provider_type"]);

        // Test CriticalProviderB
        var (providerC, capabilitiesC) = HelloTestProviderHelpers.CreateCriticalProviderB();
        Assert.Equal(Priority.Critical, capabilitiesC.Priority);
        Assert.True(capabilitiesC.HasTags("critical", "runtime", "greeting"));

        // Test AnalyticsProviderA
        var (providerD, capabilitiesD) = HelloTestProviderHelpers.CreateAnalyticsProviderA();
        Assert.True(capabilitiesD.HasTags("analytics", "events", "player"));
        Assert.Equal("player", capabilitiesD.Metadata["shard_category"]);

        // Test GameProviderB
        var (providerE, capabilitiesE) = HelloTestProviderHelpers.CreateGameProviderB();
        Assert.True(capabilitiesE.HasTags("analytics", "events", "game"));
        Assert.Equal("game", capabilitiesE.Metadata["shard_category"]);
    }
}