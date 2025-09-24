using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for selection strategy abstractions and implementations.
/// </summary>
public class SelectionStrategyTests
{
    /// <summary>
    /// Test service interface for selection strategy testing.
    /// </summary>
    public interface ITestSelectionService
    {
        string GetName();
    }

    /// <summary>
    /// Test service implementation.
    /// </summary>
    public class TestSelectionService : ITestSelectionService
    {
        public string Name { get; }

        public TestSelectionService(string name)
        {
            Name = name;
        }

        public string GetName() => Name;
    }

    [Fact]
    public void PickOneSelectionStrategy_WithMultipleProviders_SelectsHighestPriority()
    {
        // Arrange
        var lowPriorityProvider = new TestSelectionService("LowPriority");
        var highPriorityProvider = new TestSelectionService("HighPriority");
        var normalPriorityProvider = new TestSelectionService("NormalPriority");

        var baseTime = DateTime.UtcNow;
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(lowPriorityProvider, Priority.Low, baseTime.AddSeconds(1)),
            CreateRegistration(highPriorityProvider, Priority.High, baseTime.AddSeconds(2)),
            CreateRegistration(normalPriorityProvider, Priority.Normal, baseTime.AddSeconds(3))
        };

        var context = new SelectionContext<ITestSelectionService>(registrations);
        var strategy = new PickOneSelectionStrategy<ITestSelectionService>();

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.Single(result.SelectedProviders);
        Assert.Equal("HighPriority", result.SelectedProviders.First().GetName());
        Assert.Equal(SelectionStrategyType.PickOne, result.StrategyType);
        Assert.NotNull(result.SelectionMetadata);
    }

    [Fact]
    public void PickOneSelectionStrategy_WithSamePriority_SelectsDeterministically()
    {
        // Arrange
        var firstProvider = new TestSelectionService("First");
        var secondProvider = new TestSelectionService("Second");

        var baseTime = DateTime.UtcNow;
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(secondProvider, Priority.Normal, baseTime.AddSeconds(2)),
            CreateRegistration(firstProvider, Priority.Normal, baseTime.AddSeconds(1))
        };

        var context = new SelectionContext<ITestSelectionService>(registrations);
        var strategy = new PickOneSelectionStrategy<ITestSelectionService>();

        // Act - Run multiple times to verify deterministic behavior
        var results = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            var result = strategy.SelectProviders(context);
            results.Add(result.SelectedProviders.First().GetName());
        }

        // Assert
        Assert.True(results.All(r => r == results[0]), "Selection should be deterministic");
        Assert.Equal(SelectionStrategyType.PickOne, strategy.StrategyType);
    }

    [Fact]
    public void PickOneSelectionStrategy_WithNoProviders_ThrowsException()
    {
        // Arrange
        var emptyRegistrations = new List<IProviderRegistration>();
        var context = new SelectionContext<ITestSelectionService>(emptyRegistrations);
        var strategy = new PickOneSelectionStrategy<ITestSelectionService>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => strategy.SelectProviders(context));
        Assert.Contains("No providers registered", exception.Message);
        Assert.Contains("ITestSelectionService", exception.Message);
    }

    [Fact]
    public void PickOneSelectionStrategy_CanHandle_ReturnsTrueForNonEmptyContext()
    {
        // Arrange
        var provider = new TestSelectionService("Test");
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(provider, Priority.Normal, DateTime.UtcNow)
        };
        var context = new SelectionContext<ITestSelectionService>(registrations);
        var strategy = new PickOneSelectionStrategy<ITestSelectionService>();

        // Act
        var canHandle = strategy.CanHandle(context);

        // Assert
        Assert.True(canHandle);
    }

    [Fact]
    public void PickOneSelectionStrategy_CanHandle_ReturnsFalseForEmptyContext()
    {
        // Arrange
        var emptyRegistrations = new List<IProviderRegistration>();
        var context = new SelectionContext<ITestSelectionService>(emptyRegistrations);
        var strategy = new PickOneSelectionStrategy<ITestSelectionService>();

        // Act
        var canHandle = strategy.CanHandle(context);

        // Assert
        Assert.False(canHandle);
    }

    [Fact]
    public void SelectionContext_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var provider = new TestSelectionService("Test");
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(provider, Priority.Normal, DateTime.UtcNow)
        };
        var metadata = new Dictionary<string, object> { ["key"] = "value" };
        var cancellationToken = new CancellationToken(true);

        // Act
        var context = new SelectionContext<ITestSelectionService>(registrations, metadata, cancellationToken);

        // Assert
        Assert.Equal(typeof(ITestSelectionService), context.ServiceType);
        Assert.Same(registrations, context.Registrations);
        Assert.Same(metadata, context.Metadata);
        Assert.Equal(cancellationToken, context.CancellationToken);
    }

    [Fact]  
    public void SelectionResult_Single_CreatesResultWithSingleProvider()
    {
        // Arrange
        var provider = new TestSelectionService("Test");
        var metadata = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        var result = SelectionResult<ITestSelectionService>.Single(
            provider, SelectionStrategyType.PickOne, metadata);

        // Assert
        Assert.Single(result.SelectedProviders);
        Assert.Same(provider, result.SelectedProviders.First());
        Assert.Equal(SelectionStrategyType.PickOne, result.StrategyType);
        Assert.Same(metadata, result.SelectionMetadata);
    }

    [Fact]
    public void DefaultSelectionStrategies_CreatePickOne_ReturnsPickOneStrategy()
    {
        // Act
        var strategy = DefaultSelectionStrategies.CreatePickOne<ITestSelectionService>();

        // Assert
        Assert.NotNull(strategy);
        Assert.Equal(SelectionStrategyType.PickOne, strategy.StrategyType);
        Assert.Equal(typeof(ITestSelectionService), ((ISelectionStrategy)strategy).ServiceType);
    }

    [Fact]
    public void DefaultSelectionStrategies_CreateFanOut_ReturnsFanOutStrategy()
    {
        // Act
        var strategy = DefaultSelectionStrategies.CreateFanOut<ITestSelectionService>();

        // Assert
        Assert.NotNull(strategy);
        Assert.Equal(SelectionStrategyType.FanOut, strategy.StrategyType);
        Assert.Equal(typeof(ITestSelectionService), ((ISelectionStrategy)strategy).ServiceType);
    }

    [Fact]
    public void FanOutSelectionStrategy_WithMultipleProviders_SelectsAllProviders()
    {
        // Arrange
        var provider1 = new TestSelectionService("Provider1");
        var provider2 = new TestSelectionService("Provider2");
        var provider3 = new TestSelectionService("Provider3");

        var baseTime = DateTime.UtcNow;
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(provider1, Priority.Low, baseTime.AddSeconds(1)),
            CreateRegistration(provider2, Priority.High, baseTime.AddSeconds(2)),
            CreateRegistration(provider3, Priority.Normal, baseTime.AddSeconds(3))
        };

        var context = new SelectionContext<ITestSelectionService>(registrations);
        var strategy = new FanOutSelectionStrategy<ITestSelectionService>();

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.Equal(3, result.SelectedProviders.Count);
        Assert.Equal(SelectionStrategyType.FanOut, result.StrategyType);
        Assert.NotNull(result.SelectionMetadata);
        Assert.Equal(3, result.SelectionMetadata["ProviderCount"]);
        
        // Verify all providers are included
        var providerNames = result.SelectedProviders.Select(p => p.GetName()).ToHashSet();
        Assert.Contains("Provider1", providerNames);
        Assert.Contains("Provider2", providerNames);
        Assert.Contains("Provider3", providerNames);
    }

    [Fact]
    public void FanOutSelectionStrategy_WithCapabilityFilter_SelectsOnlyMatchingProviders()
    {
        // Arrange
        var analyticsProvider = new TestSelectionService("Analytics");
        var telemetryProvider = new TestSelectionService("Telemetry");
        var regularProvider = new TestSelectionService("Regular");

        var baseTime = DateTime.UtcNow;
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistrationWithTags(analyticsProvider, Priority.Normal, baseTime, "analytics"),
            CreateRegistrationWithTags(telemetryProvider, Priority.Normal, baseTime.AddSeconds(1), "telemetry"),
            CreateRegistration(regularProvider, Priority.Normal, baseTime.AddSeconds(2))
        };

        var metadata = new Dictionary<string, object>
        {
            ["RequiredTags"] = new[] { "analytics" }
        };
        var context = new SelectionContext<ITestSelectionService>(registrations, metadata);
        var strategy = new FanOutSelectionStrategy<ITestSelectionService>();

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.Single(result.SelectedProviders);
        Assert.Equal("Analytics", result.SelectedProviders.First().GetName());
        Assert.Equal(SelectionStrategyType.FanOut, result.StrategyType);
        Assert.NotNull(result.SelectionMetadata);
        Assert.Equal(1, result.SelectionMetadata["ProviderCount"]);
    }

    [Fact]
    public void FanOutSelectionStrategy_WithMultipleRequiredTags_SelectsProvidersWithAllTags()
    {
        // Arrange
        var fullFeaturedProvider = new TestSelectionService("FullFeatured");
        var analyticsOnlyProvider = new TestSelectionService("AnalyticsOnly");
        var telemetryOnlyProvider = new TestSelectionService("TelemetryOnly");

        var baseTime = DateTime.UtcNow;
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistrationWithTags(fullFeaturedProvider, Priority.Normal, baseTime, "analytics", "telemetry"),
            CreateRegistrationWithTags(analyticsOnlyProvider, Priority.Normal, baseTime.AddSeconds(1), "analytics"),
            CreateRegistrationWithTags(telemetryOnlyProvider, Priority.Normal, baseTime.AddSeconds(2), "telemetry")
        };

        var metadata = new Dictionary<string, object>
        {
            ["RequiredTags"] = new[] { "analytics", "telemetry" }
        };
        var context = new SelectionContext<ITestSelectionService>(registrations, metadata);
        var strategy = new FanOutSelectionStrategy<ITestSelectionService>();

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.Single(result.SelectedProviders);
        Assert.Equal("FullFeatured", result.SelectedProviders.First().GetName());
        Assert.Equal(SelectionStrategyType.FanOut, result.StrategyType);
    }

    [Fact]
    public void FanOutSelectionStrategy_WithNoProviders_ThrowsException()
    {
        // Arrange
        var emptyRegistrations = new List<IProviderRegistration>();
        var context = new SelectionContext<ITestSelectionService>(emptyRegistrations);
        var strategy = new FanOutSelectionStrategy<ITestSelectionService>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => strategy.SelectProviders(context));
        Assert.Contains("No providers registered", exception.Message);
        Assert.Contains("ITestSelectionService", exception.Message);
    }

    [Fact]
    public void FanOutSelectionStrategy_WithNoMatchingProvidersBundleFilter_ThrowsException()
    {
        // Arrange
        var provider = new TestSelectionService("NoTags");
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(provider, Priority.Normal, DateTime.UtcNow)
        };
        
        var metadata = new Dictionary<string, object>
        {
            ["RequiredTags"] = new[] { "nonexistent" }
        };
        var context = new SelectionContext<ITestSelectionService>(registrations, metadata);
        var strategy = new FanOutSelectionStrategy<ITestSelectionService>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => strategy.SelectProviders(context));
        Assert.Contains("No compatible providers found", exception.Message);
        Assert.Contains("ITestSelectionService", exception.Message);
    }

    [Fact]
    public void FanOutSelectionStrategy_CanHandle_ReturnsTrueForNonEmptyContext()
    {
        // Arrange
        var provider = new TestSelectionService("Test");
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(provider, Priority.Normal, DateTime.UtcNow)
        };
        var context = new SelectionContext<ITestSelectionService>(registrations);
        var strategy = new FanOutSelectionStrategy<ITestSelectionService>();

        // Act
        var canHandle = strategy.CanHandle(context);

        // Assert
        Assert.True(canHandle);
    }

    [Fact]
    public void FanOutSelectionStrategy_CanHandle_ReturnsFalseForEmptyContext()
    {
        // Arrange
        var emptyRegistrations = new List<IProviderRegistration>();
        var context = new SelectionContext<ITestSelectionService>(emptyRegistrations);
        var strategy = new FanOutSelectionStrategy<ITestSelectionService>();

        // Act
        var canHandle = strategy.CanHandle(context);

        // Assert
        Assert.False(canHandle);
    }

    [Fact]
    public void FanOutSelectionStrategy_SelectionMetadata_IncludesProviderDetails()
    {
        // Arrange
        var provider1 = new TestSelectionService("Provider1");
        var provider2 = new TestSelectionService("Provider2");

        var baseTime = DateTime.UtcNow;
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(provider1, Priority.Normal, baseTime),
            CreateRegistration(provider2, Priority.High, baseTime.AddSeconds(1))
        };

        var context = new SelectionContext<ITestSelectionService>(registrations);
        var strategy = new FanOutSelectionStrategy<ITestSelectionService>();

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.NotNull(result.SelectionMetadata);
        Assert.Equal(2, result.SelectionMetadata["ProviderCount"]);
        Assert.Equal("FanOut (all compatible providers)", result.SelectionMetadata["SelectionMethod"]);
        Assert.Equal("FanOut", result.SelectionMetadata["StrategyType"]);
        
        var providerIds = (List<string>)result.SelectionMetadata["ProviderIds"];
        Assert.Equal(2, providerIds.Count);
        Assert.All(providerIds, id => Assert.StartsWith("test-provider-", id));
    }

    [Fact]
    public void FanOutSelectionStrategy_WithInactiveProviders_SkipsInactiveProviders()
    {
        // Arrange
        var activeProvider = new TestSelectionService("Active");
        var inactiveProvider = new TestSelectionService("Inactive");

        var baseTime = DateTime.UtcNow;
        var registrations = new List<IProviderRegistration>
        {
            CreateActiveRegistration(activeProvider, Priority.Normal, baseTime, true),
            CreateActiveRegistration(inactiveProvider, Priority.High, baseTime.AddSeconds(1), false)
        };

        var context = new SelectionContext<ITestSelectionService>(registrations);
        var strategy = new FanOutSelectionStrategy<ITestSelectionService>();

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.Single(result.SelectedProviders);
        Assert.Equal("Active", result.SelectedProviders.First().GetName());
        Assert.Equal(SelectionStrategyType.FanOut, result.StrategyType);
        Assert.NotNull(result.SelectionMetadata);
        Assert.Equal(1, result.SelectionMetadata["ProviderCount"]);
    }

    [Fact]
    public void FanOutSelectionStrategy_WithMixedTagRequirements_FiltersCorrectly()
    {
        // Arrange
        var analyticsProvider = new TestSelectionService("Analytics");
        var telemetryProvider = new TestSelectionService("Telemetry");
        var bothProvider = new TestSelectionService("Both");
        var neitherProvider = new TestSelectionService("Neither");

        var baseTime = DateTime.UtcNow;
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistrationWithTags(analyticsProvider, Priority.Normal, baseTime, "analytics"),
            CreateRegistrationWithTags(telemetryProvider, Priority.Normal, baseTime.AddSeconds(1), "telemetry"),
            CreateRegistrationWithTags(bothProvider, Priority.Normal, baseTime.AddSeconds(2), "analytics", "telemetry"),
            CreateRegistration(neitherProvider, Priority.Normal, baseTime.AddSeconds(3))
        };

        // Test with single tag requirement
        var metadata = new Dictionary<string, object> { ["RequiredTags"] = new[] { "analytics" } };
        var context = new SelectionContext<ITestSelectionService>(registrations, metadata);
        var strategy = new FanOutSelectionStrategy<ITestSelectionService>();

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.Equal(2, result.SelectedProviders.Count);
        var providerNames = result.SelectedProviders.Select(p => p.GetName()).ToHashSet();
        Assert.Contains("Analytics", providerNames);
        Assert.Contains("Both", providerNames);
        Assert.DoesNotContain("Telemetry", providerNames);
        Assert.DoesNotContain("Neither", providerNames);
    }

    [Fact]
    public void ShardedSelectionStrategy_WithAnalyticsEventName_ExtractsCorrectShardKey()
    {
        // Arrange
        var firstProvider = new TestSelectionService("FirstProvider");
        var secondProvider = new TestSelectionService("SecondProvider");

        var baseTime = DateTime.UtcNow;
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(firstProvider, Priority.Normal, baseTime.AddSeconds(1)),
            CreateRegistration(secondProvider, Priority.Normal, baseTime.AddSeconds(2))
        };

        var metadata = new Dictionary<string, object>
        {
            ["EventName"] = "player.level.complete"
        };

        var context = new SelectionContext<ITestSelectionService>(registrations, metadata);
        var strategy = DefaultSelectionStrategies.CreateAnalyticsSharded<ITestSelectionService>();

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.Single(result.SelectedProviders);
        Assert.Equal(SelectionStrategyType.Sharded, result.StrategyType);
        Assert.NotNull(result.SelectionMetadata);
        Assert.Equal("player", result.SelectionMetadata!["ShardKey"]);
    }

    [Fact]
    public void ShardedSelectionStrategy_WithExplicitShardKey_UsesShardKeyDirectly()
    {
        // Arrange
        var provider = new TestSelectionService("TestProvider");
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(provider, Priority.Normal, DateTime.UtcNow)
        };

        var metadata = new Dictionary<string, object>
        {
            ["ShardKey"] = "custom-shard"
        };

        var context = new SelectionContext<ITestSelectionService>(registrations, metadata);
        var strategy = DefaultSelectionStrategies.CreateAnalyticsSharded<ITestSelectionService>();

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.Single(result.SelectedProviders);
        Assert.Equal(SelectionStrategyType.Sharded, result.StrategyType);
        Assert.Equal("custom-shard", result.SelectionMetadata!["ShardKey"]);
    }

    [Fact]
    public void ShardedSelectionStrategy_WithConsistentHashing_SelectsSameProviderForSameKey()
    {
        // Arrange
        var firstProvider = new TestSelectionService("Provider1");
        var secondProvider = new TestSelectionService("Provider2");
        var thirdProvider = new TestSelectionService("Provider3");

        var baseTime = DateTime.UtcNow;
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(firstProvider, Priority.Normal, baseTime.AddSeconds(1)),
            CreateRegistration(secondProvider, Priority.Normal, baseTime.AddSeconds(2)),
            CreateRegistration(thirdProvider, Priority.Normal, baseTime.AddSeconds(3))
        };

        var metadata = new Dictionary<string, object>
        {
            ["EventName"] = "player.death.pvp"
        };

        var context = new SelectionContext<ITestSelectionService>(registrations, metadata);
        var strategy = DefaultSelectionStrategies.CreateAnalyticsSharded<ITestSelectionService>();

        // Act - Run multiple times to verify consistent selection
        var results = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            var result = strategy.SelectProviders(context);
            results.Add(result.SelectedProviders.First().GetName());
        }

        // Assert
        Assert.True(results.All(r => r == results[0]), "Sharded selection should be consistent for the same shard key");
        // Verify that the shard key was correctly extracted
        var firstResult = strategy.SelectProviders(context);
        Assert.Equal("player", firstResult.SelectionMetadata!["ShardKey"]);
    }

    [Fact]
    public void ShardedSelectionStrategy_WithDifferentShardKeys_CanSelectDifferentProviders()
    {
        // Arrange
        var firstProvider = new TestSelectionService("Provider1");
        var secondProvider = new TestSelectionService("Provider2");

        var baseTime = DateTime.UtcNow;
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(firstProvider, Priority.Normal, baseTime.AddSeconds(1)),
            CreateRegistration(secondProvider, Priority.Normal, baseTime.AddSeconds(2))
        };

        var context1 = new SelectionContext<ITestSelectionService>(
            registrations, 
            new Dictionary<string, object> { ["EventName"] = "player.level.complete" });
        
        var context2 = new SelectionContext<ITestSelectionService>(
            registrations, 
            new Dictionary<string, object> { ["EventName"] = "game.session.start" });

        var strategy = DefaultSelectionStrategies.CreateAnalyticsSharded<ITestSelectionService>();

        // Act
        var result1 = strategy.SelectProviders(context1);
        var result2 = strategy.SelectProviders(context2);

        // Assert
        Assert.Single(result1.SelectedProviders);
        Assert.Single(result2.SelectedProviders);
        Assert.Equal("player", result1.SelectionMetadata!["ShardKey"]);
        Assert.Equal("game", result2.SelectionMetadata!["ShardKey"]);
        
        // Different shard keys may select different providers (this is probabilistic)
        // but each should be consistent
        Assert.Equal(SelectionStrategyType.Sharded, result1.StrategyType);
        Assert.Equal(SelectionStrategyType.Sharded, result2.StrategyType);
    }

    [Fact]
    public void ShardedSelectionStrategy_WithCustomKeyExtractor_UsesCustomLogic()
    {
        // Arrange
        var provider = new TestSelectionService("TestProvider");
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(provider, Priority.Normal, DateTime.UtcNow)
        };

        var metadata = new Dictionary<string, object>
        {
            ["CustomField"] = "custom-value-123"
        };

        // Custom key extractor that extracts the numeric suffix
        var customKeyExtractor = new Func<IDictionary<string, object>?, string>(meta =>
        {
            if (meta?.TryGetValue("CustomField", out var value) == true && value is string str)
            {
                var parts = str.Split('-');
                return parts.Length > 2 ? parts[2] : "default";
            }
            return "default";
        });

        var context = new SelectionContext<ITestSelectionService>(registrations, metadata);
        var strategy = DefaultSelectionStrategies.CreateSharded<ITestSelectionService>(customKeyExtractor);

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.Single(result.SelectedProviders);
        Assert.Equal(SelectionStrategyType.Sharded, result.StrategyType);
        Assert.Equal("123", result.SelectionMetadata!["ShardKey"]);
    }

    [Fact]
    public void ShardedSelectionStrategy_WithNoMetadata_ThrowsException()
    {
        // Arrange
        var provider = new TestSelectionService("TestProvider");
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(provider, Priority.Normal, DateTime.UtcNow)
        };

        var context = new SelectionContext<ITestSelectionService>(registrations);
        var strategy = DefaultSelectionStrategies.CreateAnalyticsSharded<ITestSelectionService>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => strategy.SelectProviders(context));
        Assert.Contains("requires metadata with EventName or ShardKey", exception.Message);
    }

    [Fact]
    public void ShardedSelectionStrategy_CanHandle_ReturnsTrueForValidContext()
    {
        // Arrange
        var provider = new TestSelectionService("TestProvider");
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(provider, Priority.Normal, DateTime.UtcNow)
        };

        var metadata = new Dictionary<string, object>
        {
            ["EventName"] = "player.level.complete"
        };

        var context = new SelectionContext<ITestSelectionService>(registrations, metadata);
        var strategy = DefaultSelectionStrategies.CreateAnalyticsSharded<ITestSelectionService>();

        // Act
        var canHandle = strategy.CanHandle(context);

        // Assert
        Assert.True(canHandle);
    }

    [Fact]
    public void ShardedSelectionStrategy_CanHandle_ReturnsFalseForInvalidContext()
    {
        // Arrange
        var provider = new TestSelectionService("TestProvider");
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(provider, Priority.Normal, DateTime.UtcNow)
        };

        var context = new SelectionContext<ITestSelectionService>(registrations); // No metadata
        var strategy = DefaultSelectionStrategies.CreateAnalyticsSharded<ITestSelectionService>();

        // Act
        var canHandle = strategy.CanHandle(context);

        // Assert
        Assert.False(canHandle);
    }

    [Fact]
    public void ExtractAnalyticsShardKey_WithEventName_ExtractsPrefix()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            ["EventName"] = "player.level.complete"
        };

        // Act
        var shardKey = DefaultSelectionStrategies.ExtractAnalyticsShardKey(metadata);

        // Assert
        Assert.Equal("player", shardKey);
    }

    [Fact]
    public void ExtractAnalyticsShardKey_WithEventNameNoDot_ReturnsFullName()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            ["EventName"] = "login"
        };

        // Act
        var shardKey = DefaultSelectionStrategies.ExtractAnalyticsShardKey(metadata);

        // Assert
        Assert.Equal("login", shardKey);
    }

    [Fact]
    public void ExtractAnalyticsShardKey_WithExplicitShardKey_PrefersSharKey()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            ["EventName"] = "player.level.complete",
            ["ShardKey"] = "custom-shard"
        };

        // Act
        var shardKey = DefaultSelectionStrategies.ExtractAnalyticsShardKey(metadata);

        // Assert
        Assert.Equal("custom-shard", shardKey);
    }

    [Fact]
    public void ExtractAnalyticsShardKey_WithNoValidData_ThrowsException()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            ["SomeOtherField"] = "value"
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            DefaultSelectionStrategies.ExtractAnalyticsShardKey(metadata));
        Assert.Contains("requires metadata with EventName or ShardKey", exception.Message);
    }

    [Fact]
    public void DefaultSelectionStrategies_CreateAnalyticsSharded_ReturnsShardedStrategy()
    {
        // Act
        var strategy = DefaultSelectionStrategies.CreateAnalyticsSharded<ITestSelectionService>();

        // Assert
        Assert.NotNull(strategy);
        Assert.Equal(SelectionStrategyType.Sharded, strategy.StrategyType);
        Assert.Equal(typeof(ITestSelectionService), ((ISelectionStrategy)strategy).ServiceType);
    }

    private static IProviderRegistration CreateRegistration(
        ITestSelectionService provider, 
        Priority priority, 
        DateTime registeredAt)
    {
        var capabilities = new ProviderCapabilities
        {
            ProviderId = $"test-provider-{Guid.NewGuid()}",
            Priority = priority,
            RegisteredAt = registeredAt,
            Platform = Platform.Any
        };

        return new TestProviderRegistration
        {
            ServiceType = typeof(ITestSelectionService),
            Provider = provider,
            Capabilities = capabilities,
            IsActive = true
        };
    }

    private static IProviderRegistration CreateRegistrationWithTags(
        ITestSelectionService provider, 
        Priority priority, 
        DateTime registeredAt,
        params string[] tags)
    {
        var capabilities = new ProviderCapabilities
        {
            ProviderId = $"test-provider-{Guid.NewGuid()}",
            Priority = priority,
            RegisteredAt = registeredAt,
            Platform = Platform.Any
        }.WithTags(tags);

        return new TestProviderRegistration
        {
            ServiceType = typeof(ITestSelectionService),
            Provider = provider,
            Capabilities = capabilities,
            IsActive = true
        };
    }

    private static IProviderRegistration CreateActiveRegistration(
        ITestSelectionService provider, 
        Priority priority, 
        DateTime registeredAt,
        bool isActive)
    {
        var capabilities = new ProviderCapabilities
        {
            ProviderId = $"test-provider-{Guid.NewGuid()}",
            Priority = priority,
            RegisteredAt = registeredAt,
            Platform = Platform.Any
        };

        return new TestProviderRegistration
        {
            ServiceType = typeof(ITestSelectionService),
            Provider = provider,
            Capabilities = capabilities,
            IsActive = isActive
        };
    }

    /// <summary>
    /// Test implementation of IProviderRegistration for testing purposes.
    /// </summary>
    private sealed class TestProviderRegistration : IProviderRegistration
    {
        public Type ServiceType { get; init; } = null!;
        public object Provider { get; init; } = null!;
        public ProviderCapabilities Capabilities { get; init; } = null!;
        public bool IsActive { get; init; } = true;
    }
}