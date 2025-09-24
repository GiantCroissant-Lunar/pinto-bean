// Integration tests for provider selection cache with ProviderChanged event wiring

using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Integration tests for provider selection cache with service registry events.
/// </summary>
public class ProviderSelectionCacheIntegrationTests
{
    [Fact]
    public void PickOneStrategy_ProviderRegistrationChanged_InvalidatesCache()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var strategy = new PickOneSelectionStrategy<ITestCacheService>(registry, TimeSpan.FromMinutes(10));

        var provider1 = new TestCacheService("Provider1");
        var provider2 = new TestCacheService("Provider2");

        // Register first provider
        var capabilities1 = new ProviderCapabilities
        {
            ProviderId = "provider-1",
            Priority = Priority.Normal,
            RegisteredAt = DateTime.UtcNow,
            Platform = Platform.Any
        };
        var registration1 = registry.Register(provider1, capabilities1);

        var registrations = new List<IProviderRegistration> { registration1 };
        var context = new SelectionContext<ITestCacheService>(registrations);

        // Execute and cache result
        var result1 = strategy.SelectProviders(context);

        // Execute again to verify caching
        var result2 = strategy.SelectProviders(context);

        // Both results should be the same (from cache)
        Assert.Single(result1.SelectedProviders);
        Assert.Single(result2.SelectedProviders);
        Assert.Equal("Provider1", ((TestCacheService)result1.SelectedProviders[0]).Name);
        Assert.Equal("Provider1", ((TestCacheService)result2.SelectedProviders[0]).Name);

        // Act - Register second provider (should trigger ProviderChanged event)
        var capabilities2 = new ProviderCapabilities
        {
            ProviderId = "provider-2",
            Priority = Priority.High, // Higher priority
            RegisteredAt = DateTime.UtcNow,
            Platform = Platform.Any
        };
        var registration2 = registry.Register(provider2, capabilities2);

        var newRegistrations = new List<IProviderRegistration> { registration1, registration2 };
        var newContext = new SelectionContext<ITestCacheService>(newRegistrations);

        // Execute again - should not use cache due to provider change
        var result3 = strategy.SelectProviders(newContext);

        // Assert - Should now select the higher priority provider
        Assert.Single(result3.SelectedProviders);
        Assert.Equal("Provider2", ((TestCacheService)result3.SelectedProviders[0]).Name);

        // Cleanup
        strategy.Dispose();
    }

    [Fact]
    public void ShardedStrategy_ProviderRegistrationChanged_InvalidatesCache()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var keyExtractor = (IDictionary<string, object>? metadata) =>
            metadata?.TryGetValue("ShardKey", out var key) == true ? key.ToString()! : "default";

        var strategy = new ShardedSelectionStrategy<ITestCacheService>(keyExtractor, registry, TimeSpan.FromMinutes(10));

        var provider1 = new TestCacheService("Provider1");

        // Register first provider
        var capabilities1 = new ProviderCapabilities
        {
            ProviderId = "provider-1",
            Priority = Priority.Normal,
            RegisteredAt = DateTime.UtcNow,
            Platform = Platform.Any
        };
        var registration1 = registry.Register(provider1, capabilities1);

        var registrations = new List<IProviderRegistration> { registration1 };
        var metadata = new Dictionary<string, object> { ["ShardKey"] = "shard1" };
        var context = new SelectionContext<ITestCacheService>(registrations, metadata);

        // Execute and cache result
        var result1 = strategy.SelectProviders(context);
        var result2 = strategy.SelectProviders(context);

        // Both results should be the same (from cache)
        Assert.Single(result1.SelectedProviders);
        Assert.Single(result2.SelectedProviders);
        Assert.Equal("Provider1", ((TestCacheService)result1.SelectedProviders[0]).Name);
        Assert.Equal("Provider1", ((TestCacheService)result2.SelectedProviders[0]).Name);

        // Act - Register second provider
        var provider2 = new TestCacheService("Provider2");
        var capabilities2 = new ProviderCapabilities
        {
            ProviderId = "provider-2",
            Priority = Priority.Normal,
            RegisteredAt = DateTime.UtcNow.AddSeconds(1), // Later registration
            Platform = Platform.Any
        };
        var registration2 = registry.Register(provider2, capabilities2);

        var newRegistrations = new List<IProviderRegistration> { registration1, registration2 };
        var newContext = new SelectionContext<ITestCacheService>(newRegistrations, metadata);

        // Execute again - should not use cache due to provider change
        var result3 = strategy.SelectProviders(newContext);

        // Assert - Should select based on sharded routing algorithm
        Assert.Single(result3.SelectedProviders);
        Assert.Contains(((TestCacheService)result3.SelectedProviders[0]).Name, new[] { "Provider1", "Provider2" });

        // Cleanup
        strategy.Dispose();
    }

    [Fact]
    public void PickOneStrategy_ProviderUnregistration_InvalidatesCache()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var strategy = new PickOneSelectionStrategy<ITestCacheService>(registry, TimeSpan.FromMinutes(10));

        var provider1 = new TestCacheService("Provider1");
        var provider2 = new TestCacheService("Provider2");

        // Register both providers
        var capabilities1 = new ProviderCapabilities
        {
            ProviderId = "provider-1",
            Priority = Priority.Normal,
            RegisteredAt = DateTime.UtcNow,
            Platform = Platform.Any
        };
        var registration1 = registry.Register(provider1, capabilities1);

        var capabilities2 = new ProviderCapabilities
        {
            ProviderId = "provider-2",
            Priority = Priority.High,
            RegisteredAt = DateTime.UtcNow.AddSeconds(1),
            Platform = Platform.Any
        };
        var registration2 = registry.Register(provider2, capabilities2);

        var registrations = new List<IProviderRegistration> { registration1, registration2 };
        var context = new SelectionContext<ITestCacheService>(registrations);

        // Execute and cache result - should select high priority provider
        var result1 = strategy.SelectProviders(context);
        Assert.Equal("Provider2", ((TestCacheService)result1.SelectedProviders[0]).Name);

        // Act - Unregister high priority provider
        registry.Unregister(registration2);

        var newRegistrations = new List<IProviderRegistration> { registration1 };
        var newContext = new SelectionContext<ITestCacheService>(newRegistrations);

        // Execute again - should not use cache and select remaining provider
        var result2 = strategy.SelectProviders(newContext);

        // Assert
        Assert.Single(result2.SelectedProviders);
        Assert.Equal("Provider1", ((TestCacheService)result2.SelectedProviders[0]).Name);

        // Cleanup
        strategy.Dispose();
    }

    [Fact]
    public void ProviderSelectionCache_ThreadSafety_ConcurrentOperations()
    {
        // Arrange
        using var cache = new SelectionCache<ITestCacheService>(TimeSpan.FromMinutes(1));
        var provider = new TestCacheService("ThreadSafeProvider");
        var result = SelectionResult<ITestCacheService>.Single(provider, SelectionStrategyType.PickOne);

        var contexts = new List<ISelectionContext<ITestCacheService>>();
        for (int i = 0; i < 10; i++)
        {
            contexts.Add(CreateTestContext($"Provider{i}", $"provider-{i}"));
        }

        // Act - Perform concurrent set/get operations
        var tasks = new List<Thread>();
        var exceptions = new List<Exception>();

        for (int i = 0; i < 10; i++)
        {
            var index = i;
            var thread = new Thread(() =>
            {
                try
                {
                    // Set operation
                    cache.Set(contexts[index], result);

                    // Get operation
                    var cachedResult = cache.TryGet(contexts[index]);

                    // Remove operation
                    cache.Remove(contexts[index]);
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            tasks.Add(thread);
        }

        // Start all threads
        foreach (var task in tasks)
        {
            task.Start();
        }

        // Wait for all threads to complete
        foreach (var task in tasks)
        {
            task.Join(5000); // 5 second timeout
        }

        // Assert - No exceptions should have occurred
        Assert.Empty(exceptions);
    }

    [Fact]
    public void ProviderSelectionCache_ContextEquality_SameRegistrationsProduceSameKey()
    {
        // Arrange
        var provider1 = new TestCacheService("Provider1");
        var provider2 = new TestCacheService("Provider2");

        var registration1 = CreateRegistration(provider1, Priority.Normal, DateTime.UtcNow, "provider-1");
        var registration2 = CreateRegistration(provider2, Priority.High, DateTime.UtcNow.AddSeconds(1), "provider-2");

        var registrations = new List<IProviderRegistration> { registration1, registration2 };

        // Create two contexts with same registrations
        var context1 = new SelectionContext<ITestCacheService>(registrations);
        var context2 = new SelectionContext<ITestCacheService>(registrations);

        // Act & Assert - Hash codes should be the same
        var hash1 = SelectionContextHashHelper.GetHashCode(context1);
        var hash2 = SelectionContextHashHelper.GetHashCode(context2);
        Assert.Equal(hash1, hash2);

        // Equivalence should be true
        Assert.True(SelectionContextHashHelper.AreEquivalent(context1, context2));
    }

    [Fact]
    public void ProviderSelectionCache_ContextEquality_DifferentMetadataProducesDifferentKey()
    {
        // Arrange
        var provider = new TestCacheService("Provider1");
        var registration = CreateRegistration(provider, Priority.Normal, DateTime.UtcNow, "provider-1");
        var registrations = new List<IProviderRegistration> { registration };

        var metadata1 = new Dictionary<string, object> { ["Key1"] = "Value1" };
        var metadata2 = new Dictionary<string, object> { ["Key1"] = "Value2" };

        var context1 = new SelectionContext<ITestCacheService>(registrations, metadata1);
        var context2 = new SelectionContext<ITestCacheService>(registrations, metadata2);

        // Act & Assert - Hash codes should be different
        var hash1 = SelectionContextHashHelper.GetHashCode(context1);
        var hash2 = SelectionContextHashHelper.GetHashCode(context2);
        Assert.NotEqual(hash1, hash2);

        // Equivalence should be false
        Assert.False(SelectionContextHashHelper.AreEquivalent(context1, context2));
    }

    private ISelectionContext<ITestCacheService> CreateTestContext(string providerName, string providerId)
    {
        var provider = new TestCacheService(providerName);
        var registration = CreateRegistration(provider, Priority.Normal, DateTime.UtcNow, providerId);
        var registrations = new List<IProviderRegistration> { registration };

        return new SelectionContext<ITestCacheService>(registrations);
    }

    private static IProviderRegistration CreateRegistration(
        ITestCacheService provider,
        Priority priority,
        DateTime registeredAt,
        string providerId)
    {
        var capabilities = new ProviderCapabilities
        {
            ProviderId = providerId,
            Priority = priority,
            RegisteredAt = registeredAt,
            Platform = Platform.Any
        };

        return new TestProviderRegistration
        {
            ServiceType = typeof(ITestCacheService),
            Provider = provider,
            Capabilities = capabilities,
            IsActive = true
        };
    }

    /// <summary>
    /// Test service interface for integration testing.
    /// </summary>
    public interface ITestCacheService
    {
        string Name { get; }
    }

    /// <summary>
    /// Test service implementation.
    /// </summary>
    public class TestCacheService : ITestCacheService
    {
        public string Name { get; }

        public TestCacheService(string name)
        {
            Name = name;
        }
    }

    private sealed class TestProviderRegistration : IProviderRegistration
    {
        public Type ServiceType { get; init; } = null!;
        public object Provider { get; init; } = null!;
        public ProviderCapabilities Capabilities { get; init; } = null!;
        public bool IsActive { get; init; } = true;
    }
}
