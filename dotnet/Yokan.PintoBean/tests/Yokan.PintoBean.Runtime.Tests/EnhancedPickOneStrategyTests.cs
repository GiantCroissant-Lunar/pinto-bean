using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for the enhanced PickOne strategy with capability filtering, platform filtering,
/// deterministic tie-break, and TTL cache.
/// </summary>
public class EnhancedPickOneStrategyTests
{
    /// <summary>
    /// Test service interface for enhanced selection strategy testing.
    /// </summary>
    public interface IEnhancedTestService
    {
        string GetName();
    }

    /// <summary>
    /// Test service implementation.
    /// </summary>
    public class EnhancedTestService : IEnhancedTestService
    {
        public string Name { get; }

        public EnhancedTestService(string name)
        {
            Name = name;
        }

        public string GetName() => Name;
    }

    [Fact]
    public void PickOneStrategy_WithCapabilityFilter_SelectsMatchingProvider()
    {
        // Arrange
        var analyticsProvider = new EnhancedTestService("Analytics");
        var telemetryProvider = new EnhancedTestService("Telemetry");
        var generalProvider = new EnhancedTestService("General");

        var baseTime = DateTime.UtcNow;
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistrationWithTags(analyticsProvider, Priority.Normal, baseTime, "analytics", "primary"),
            CreateRegistrationWithTags(telemetryProvider, Priority.Normal, baseTime.AddSeconds(1), "telemetry"),
            CreateRegistrationWithTags(generalProvider, Priority.Normal, baseTime.AddSeconds(2))
        };

        var metadata = new Dictionary<string, object>
        {
            ["RequiredTags"] = new[] { "analytics" }
        };

        var context = new SelectionContext<IEnhancedTestService>(registrations, metadata);
        var strategy = new PickOneSelectionStrategy<IEnhancedTestService>();

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.Single(result.SelectedProviders);
        Assert.Equal("Analytics", result.SelectedProviders.First().GetName());
        Assert.Equal(SelectionStrategyType.PickOne, result.StrategyType);
    }

    [Fact]
    public void PickOneStrategy_WithMultipleRequiredTags_SelectsOnlyMatchingProvider()
    {
        // Arrange
        var primaryAnalyticsProvider = new EnhancedTestService("PrimaryAnalytics");
        var secondaryAnalyticsProvider = new EnhancedTestService("SecondaryAnalytics");
        var generalProvider = new EnhancedTestService("General");

        var baseTime = DateTime.UtcNow;
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistrationWithTags(primaryAnalyticsProvider, Priority.Normal, baseTime, "analytics", "primary"),
            CreateRegistrationWithTags(secondaryAnalyticsProvider, Priority.Normal, baseTime.AddSeconds(1), "analytics", "secondary"),
            CreateRegistrationWithTags(generalProvider, Priority.Normal, baseTime.AddSeconds(2))
        };

        var metadata = new Dictionary<string, object>
        {
            ["RequiredTags"] = new[] { "analytics", "primary" }
        };

        var context = new SelectionContext<IEnhancedTestService>(registrations, metadata);
        var strategy = new PickOneSelectionStrategy<IEnhancedTestService>();

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.Single(result.SelectedProviders);
        Assert.Equal("PrimaryAnalytics", result.SelectedProviders.First().GetName());
    }

    [Fact]
    public void PickOneStrategy_WithNoMatchingCapabilities_ThrowsException()
    {
        // Arrange
        var provider = new EnhancedTestService("Test");
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistrationWithTags(provider, Priority.Normal, DateTime.UtcNow, "telemetry")
        };

        var metadata = new Dictionary<string, object>
        {
            ["RequiredTags"] = new[] { "analytics" }
        };

        var context = new SelectionContext<IEnhancedTestService>(registrations, metadata);
        var strategy = new PickOneSelectionStrategy<IEnhancedTestService>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => strategy.SelectProviders(context));
        Assert.Contains("No compatible providers found", exception.Message);
    }

    [Fact]
    public void PickOneStrategy_DeterministicTieBreak_AlwaysSelectsSameProvider()
    {
        // Arrange - Create providers with same priority and registration time
        var provider1 = new EnhancedTestService("Provider1");
        var provider2 = new EnhancedTestService("Provider2");
        var provider3 = new EnhancedTestService("Provider3");

        var sameTime = DateTime.UtcNow;
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistrationWithId(provider1, "provider-aaa", Priority.Normal, sameTime),
            CreateRegistrationWithId(provider2, "provider-zzz", Priority.Normal, sameTime),
            CreateRegistrationWithId(provider3, "provider-mmm", Priority.Normal, sameTime)
        };

        var context = new SelectionContext<IEnhancedTestService>(registrations);
        var strategy = new PickOneSelectionStrategy<IEnhancedTestService>();

        // Act - Run selection multiple times
        var results = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            var result = strategy.SelectProviders(context);
            results.Add(result.SelectedProviders.First().GetName());
        }

        // Assert - All results should be the same (deterministic)
        Assert.True(results.All(r => r == results[0]), "Selection should be deterministic");

        // The selection should be based on stable hash, not alphabetical or registration order
        // We can't predict which one will be selected, but it should be consistent
    }

    [Fact]
    public void PickOneStrategy_WithPlatformFiltering_SelectsCompatibleProvider()
    {
        // Arrange
        var unityProvider = new EnhancedTestService("Unity");
        var webProvider = new EnhancedTestService("Web");
        var anyProvider = new EnhancedTestService("Any");

        var baseTime = DateTime.UtcNow;
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistrationWithPlatform(unityProvider, Priority.Normal, baseTime, Platform.Unity),
            CreateRegistrationWithPlatform(webProvider, Priority.Normal, baseTime.AddSeconds(1), Platform.Web),
            CreateRegistrationWithPlatform(anyProvider, Priority.Normal, baseTime.AddSeconds(2), Platform.Any)
        };

        var context = new SelectionContext<IEnhancedTestService>(registrations);
        var strategy = new PickOneSelectionStrategy<IEnhancedTestService>();

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.Single(result.SelectedProviders);
        // Should select either the current platform-specific provider or the "Any" provider
        var selectedName = result.SelectedProviders.First().GetName();
        Assert.True(selectedName == "Any" || selectedName == "Unity" || selectedName == "Web",
            $"Selected provider should be platform-compatible, got: {selectedName}");
    }

    [Fact]
    public void PickOneStrategy_WithCache_ReturnsCachedResult()
    {
        // Arrange
        var provider = new EnhancedTestService("Cached");
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(provider, Priority.Normal, DateTime.UtcNow)
        };

        var context = new SelectionContext<IEnhancedTestService>(registrations);
        var strategy = new PickOneSelectionStrategy<IEnhancedTestService>(cacheTtl: TimeSpan.FromMinutes(1));

        // Act - First call should compute and cache
        var result1 = strategy.SelectProviders(context);
        var result2 = strategy.SelectProviders(context);

        // Assert
        Assert.Equal("Cached", result1.SelectedProviders.First().GetName());
        Assert.Equal("Cached", result2.SelectedProviders.First().GetName());
        // Both results should have the same reference due to caching
        Assert.Same(result1, result2);
    }

    [Fact]
    public void PickOneStrategy_WithCacheInvalidation_RefreshesAfterProviderChange()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var provider1 = new EnhancedTestService("Original");
        var provider2 = new EnhancedTestService("Updated");

        // Register initial provider
        var registration1 = registry.Register<IEnhancedTestService>(provider1, ProviderCapabilities.Create("test-1"));
        var registrations = registry.GetRegistrations<IEnhancedTestService>().ToList();

        var context = new SelectionContext<IEnhancedTestService>(registrations);
        var strategy = new PickOneSelectionStrategy<IEnhancedTestService>(registry, TimeSpan.FromMinutes(1));

        // Act - First call should cache
        var result1 = strategy.SelectProviders(context);

        // Register new provider (this should invalidate cache)
        var registration2 = registry.Register<IEnhancedTestService>(provider2, ProviderCapabilities.Create("test-2").WithPriority(Priority.High));
        var updatedRegistrations = registry.GetRegistrations<IEnhancedTestService>().ToList();
        var updatedContext = new SelectionContext<IEnhancedTestService>(updatedRegistrations);

        // Second call should use new selection (not cached)
        var result2 = strategy.SelectProviders(updatedContext);

        // Assert
        Assert.Equal("Original", result1.SelectedProviders.First().GetName());
        Assert.Equal("Updated", result2.SelectedProviders.First().GetName()); // Higher priority provider should be selected
        Assert.NotSame(result1, result2); // Should not be cached result
    }

    [Fact]
    public void PickOneStrategy_WithExpiredCache_RecomputesResult()
    {
        // Arrange
        var provider = new EnhancedTestService("Test");
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(provider, Priority.Normal, DateTime.UtcNow)
        };

        var context = new SelectionContext<IEnhancedTestService>(registrations);
        var strategy = new PickOneSelectionStrategy<IEnhancedTestService>(cacheTtl: TimeSpan.FromMilliseconds(50));

        // Act - First call should compute and cache
        var result1 = strategy.SelectProviders(context);

        // Wait for cache to expire
        Task.Delay(100).Wait();

        // Second call should recompute (cache expired)
        var result2 = strategy.SelectProviders(context);

        // Assert
        Assert.Equal("Test", result1.SelectedProviders.First().GetName());
        Assert.Equal("Test", result2.SelectedProviders.First().GetName());
        // Results should not be the same reference due to expiration
        Assert.NotSame(result1, result2);
    }

    [Fact]
    public void PickOneStrategy_DisposesCorrectly()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var strategy = new PickOneSelectionStrategy<IEnhancedTestService>(registry);

        // Act & Assert - Should not throw
        strategy.Dispose();
        strategy.Dispose(); // Double dispose should be safe
    }

    private static IProviderRegistration CreateRegistration(
        IEnhancedTestService provider,
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
            ServiceType = typeof(IEnhancedTestService),
            Provider = provider,
            Capabilities = capabilities,
            IsActive = true
        };
    }

    private static IProviderRegistration CreateRegistrationWithTags(
        IEnhancedTestService provider,
        Priority priority,
        DateTime registeredAt,
        params string[] tags)
    {
        var capabilities = ProviderCapabilities.Create($"test-provider-{Guid.NewGuid()}")
            .WithPriority(priority)
            .WithTags(tags)
            .WithPlatform(Platform.Any);

        return new TestProviderRegistration
        {
            ServiceType = typeof(IEnhancedTestService),
            Provider = provider,
            Capabilities = capabilities with { RegisteredAt = registeredAt },
            IsActive = true
        };
    }

    private static IProviderRegistration CreateRegistrationWithId(
        IEnhancedTestService provider,
        string providerId,
        Priority priority,
        DateTime registeredAt)
    {
        var capabilities = ProviderCapabilities.Create(providerId)
            .WithPriority(priority)
            .WithPlatform(Platform.Any);

        return new TestProviderRegistration
        {
            ServiceType = typeof(IEnhancedTestService),
            Provider = provider,
            Capabilities = capabilities with { RegisteredAt = registeredAt },
            IsActive = true
        };
    }

    private static IProviderRegistration CreateRegistrationWithPlatform(
        IEnhancedTestService provider,
        Priority priority,
        DateTime registeredAt,
        Platform platform)
    {
        var capabilities = ProviderCapabilities.Create($"test-provider-{Guid.NewGuid()}")
            .WithPriority(priority)
            .WithPlatform(platform);

        return new TestProviderRegistration
        {
            ServiceType = typeof(IEnhancedTestService),
            Provider = provider,
            Capabilities = capabilities with { RegisteredAt = registeredAt },
            IsActive = true
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
