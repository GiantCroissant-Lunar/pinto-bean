// Unit tests for IProviderSelectionCache interface and SelectionCache implementation

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for IProviderSelectionCache interface and SelectionCache implementation.
/// </summary>
public class ProviderSelectionCacheTests
{
    [Fact]
    public void Constructor_WithDefaultTtl_SetsCorrectDefaultTtl()
    {
        // Arrange & Act
        using var cache = new SelectionCache<ITestSelectionService>();

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(5), cache.DefaultTtl);
    }

    [Fact]
    public void Constructor_WithCustomTtl_SetsCorrectCustomTtl()
    {
        // Arrange
        var customTtl = TimeSpan.FromMinutes(10);

        // Act
        using var cache = new SelectionCache<ITestSelectionService>(customTtl);

        // Assert
        Assert.Equal(customTtl, cache.DefaultTtl);
    }

    [Fact]
    public void TryGet_EmptyCache_ReturnsNull()
    {
        // Arrange
        using var cache = new SelectionCache<ITestSelectionService>();
        var context = CreateTestContext();

        // Act
        var result = cache.TryGet(context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Set_AndTryGet_ValidEntry_ReturnsResult()
    {
        // Arrange
        using var cache = new SelectionCache<ITestSelectionService>();
        var context = CreateTestContext();
        var provider = new TestSelectionService("Test");
        var expectedResult = SelectionResult<ITestSelectionService>.Single(provider, SelectionStrategyType.PickOne);

        // Act
        cache.Set(context, expectedResult);
        var actualResult = cache.TryGet(context);

        // Assert
        Assert.NotNull(actualResult);
        Assert.Equal(expectedResult.StrategyType, actualResult.StrategyType);
        Assert.Single(actualResult.SelectedProviders);
        Assert.Equal("Test", ((TestSelectionService)actualResult.SelectedProviders[0]).Name);
    }

    [Fact]
    public void Get_ValidEntry_ReturnsResult()
    {
        // Arrange
        using var cache = new SelectionCache<ITestSelectionService>();
        var context = CreateTestContext();
        var provider = new TestSelectionService("Test");
        var expectedResult = SelectionResult<ITestSelectionService>.Single(provider, SelectionStrategyType.PickOne);

        // Act
        cache.Set(context, expectedResult);
        var actualResult = cache.Get(context);

        // Assert
        Assert.NotNull(actualResult);
        Assert.Equal(expectedResult.StrategyType, actualResult.StrategyType);
    }

    [Fact]
    public void Get_EmptyCache_ThrowsInvalidOperationException()
    {
        // Arrange
        using var cache = new SelectionCache<ITestSelectionService>();
        var context = CreateTestContext();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => cache.Get(context));
    }

    [Fact]
    public void Set_WithCustomTtl_UsesCustomTtl()
    {
        // Arrange
        using var cache = new SelectionCache<ITestSelectionService>(TimeSpan.FromHours(1));
        var context = CreateTestContext();
        var provider = new TestSelectionService("Test");
        var result = SelectionResult<ITestSelectionService>.Single(provider, SelectionStrategyType.PickOne);
        var customTtl = TimeSpan.FromSeconds(1);

        // Act
        cache.Set(context, result, customTtl);

        // Assert - Entry should be cached initially
        Assert.NotNull(cache.TryGet(context));

        // Wait for custom TTL to expire
        Thread.Sleep(1100);

        // Assert - Entry should be expired
        Assert.Null(cache.TryGet(context));
    }

    [Fact]
    public void TryGet_ExpiredEntry_ReturnsNullAndRemovesEntry()
    {
        // Arrange
        using var cache = new SelectionCache<ITestSelectionService>(TimeSpan.FromMilliseconds(10));
        var context = CreateTestContext();
        var provider = new TestSelectionService("Test");
        var result = SelectionResult<ITestSelectionService>.Single(provider, SelectionStrategyType.PickOne);

        // Act
        cache.Set(context, result);

        // Wait for expiration
        Thread.Sleep(50);

        var resultAfterExpiry = cache.TryGet(context);

        // Assert
        Assert.Null(resultAfterExpiry);
        Assert.Equal(0, cache.Count); // Should be removed from cache
    }

    [Fact]
    public void Remove_ExistingEntry_ReturnsTrueAndRemovesEntry()
    {
        // Arrange
        using var cache = new SelectionCache<ITestSelectionService>();
        var context = CreateTestContext();
        var provider = new TestSelectionService("Test");
        var result = SelectionResult<ITestSelectionService>.Single(provider, SelectionStrategyType.PickOne);
        cache.Set(context, result);

        // Act
        var removed = cache.Remove(context);
        var afterRemoval = cache.TryGet(context);

        // Assert
        Assert.True(removed);
        Assert.Null(afterRemoval);
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void Remove_NonExistentEntry_ReturnsFalse()
    {
        // Arrange
        using var cache = new SelectionCache<ITestSelectionService>();
        var context = CreateTestContext();

        // Act
        var removed = cache.Remove(context);

        // Assert
        Assert.False(removed);
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        // Arrange
        using var cache = new SelectionCache<ITestSelectionService>();
        var context1 = CreateTestContext("Provider1");
        var context2 = CreateTestContext("Provider2");
        var provider1 = new TestSelectionService("Test1");
        var provider2 = new TestSelectionService("Test2");
        var result1 = SelectionResult<ITestSelectionService>.Single(provider1, SelectionStrategyType.PickOne);
        var result2 = SelectionResult<ITestSelectionService>.Single(provider2, SelectionStrategyType.PickOne);

        cache.Set(context1, result1);
        cache.Set(context2, result2);

        // Act
        cache.Clear();

        // Assert
        Assert.Null(cache.TryGet(context1));
        Assert.Null(cache.TryGet(context2));
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void Count_ReflectsNumberOfCachedEntries()
    {
        // Arrange
        using var cache = new SelectionCache<ITestSelectionService>();
        var context1 = CreateTestContext("Provider1");
        var context2 = CreateTestContext("Provider2");
        var provider1 = new TestSelectionService("Test1");
        var provider2 = new TestSelectionService("Test2");
        var result1 = SelectionResult<ITestSelectionService>.Single(provider1, SelectionStrategyType.PickOne);
        var result2 = SelectionResult<ITestSelectionService>.Single(provider2, SelectionStrategyType.PickOne);

        // Act & Assert
        Assert.Equal(0, cache.Count);

        cache.Set(context1, result1);
        Assert.Equal(1, cache.Count);

        cache.Set(context2, result2);
        Assert.Equal(2, cache.Count);

        cache.Remove(context1);
        Assert.Equal(1, cache.Count);

        cache.Clear();
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void CleanupExpired_RemovesOnlyExpiredEntries()
    {
        // Arrange
        using var cache = new SelectionCache<ITestSelectionService>();
        var context1 = CreateTestContext("Provider1");
        var context2 = CreateTestContext("Provider2");
        var provider1 = new TestSelectionService("Test1");
        var provider2 = new TestSelectionService("Test2");
        var result1 = SelectionResult<ITestSelectionService>.Single(provider1, SelectionStrategyType.PickOne);
        var result2 = SelectionResult<ITestSelectionService>.Single(provider2, SelectionStrategyType.PickOne);

        // Set one with short TTL, one with long TTL
        cache.Set(context1, result1, TimeSpan.FromMilliseconds(10));
        cache.Set(context2, result2, TimeSpan.FromHours(1));

        // Wait for first to expire
        Thread.Sleep(50);

        // Act
        cache.CleanupExpired();

        // Assert
        Assert.Null(cache.TryGet(context1)); // Should be removed
        Assert.NotNull(cache.TryGet(context2)); // Should still be there
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public async Task TimerBasedEviction_AutomaticallyRemovesExpiredEntries()
    {
        // Arrange
        using var cache = new SelectionCache<ITestSelectionService>();
        var context = CreateTestContext();
        var provider = new TestSelectionService("Test");
        var result = SelectionResult<ITestSelectionService>.Single(provider, SelectionStrategyType.PickOne);

        // Set with very short TTL
        cache.Set(context, result, TimeSpan.FromMilliseconds(10));
        Assert.Equal(1, cache.Count);

        // Wait for expiration
        await Task.Delay(50);

        // Manually trigger cleanup (simulating timer callback)
        cache.CleanupExpired();

        // Assert
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void Dispose_DisposesTimerAndClearsCache()
    {
        // Arrange
        var cache = new SelectionCache<ITestSelectionService>();
        var context = CreateTestContext();
        var provider = new TestSelectionService("Test");
        var result = SelectionResult<ITestSelectionService>.Single(provider, SelectionStrategyType.PickOne);
        cache.Set(context, result);

        // Act
        cache.Dispose();

        // Assert
        Assert.Equal(0, cache.Count);
        Assert.Throws<ObjectDisposedException>(() => cache.TryGet(context));
        Assert.Throws<ObjectDisposedException>(() => cache.Set(context, result));
        Assert.Throws<ObjectDisposedException>(() => cache.Remove(context));
    }

    [Fact]
    public void SelectionContextHashHelper_GetHashCode_SameContexts_ProduceSameHash()
    {
        // Arrange
        var context1 = CreateTestContext("Provider1");
        var context2 = CreateTestContext("Provider1"); // Same provider

        // Act
        var hash1 = SelectionContextHashHelper.GetHashCode(context1);
        var hash2 = SelectionContextHashHelper.GetHashCode(context2);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void SelectionContextHashHelper_GetHashCode_DifferentContexts_ProduceDifferentHashes()
    {
        // Arrange
        var context1 = CreateTestContext("Provider1");
        var context2 = CreateTestContext("Provider2");

        // Act
        var hash1 = SelectionContextHashHelper.GetHashCode(context1);
        var hash2 = SelectionContextHashHelper.GetHashCode(context2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void SelectionContextHashHelper_AreEquivalent_SameContexts_ReturnsTrue()
    {
        // Arrange
        var context1 = CreateTestContext("Provider1");
        var context2 = CreateTestContext("Provider1");

        // Act
        var areEquivalent = SelectionContextHashHelper.AreEquivalent(context1, context2);

        // Assert
        Assert.True(areEquivalent);
    }

    [Fact]
    public void SelectionContextHashHelper_AreEquivalent_DifferentContexts_ReturnsFalse()
    {
        // Arrange
        var context1 = CreateTestContext("Provider1");
        var context2 = CreateTestContext("Provider2");

        // Act
        var areEquivalent = SelectionContextHashHelper.AreEquivalent(context1, context2);

        // Assert
        Assert.False(areEquivalent);
    }

    [Fact]
    public void SelectionContextHashHelper_AreEquivalent_NullContexts_HandlesCorrectly()
    {
        // Arrange
        var context = CreateTestContext();

        // Act & Assert
        Assert.True(SelectionContextHashHelper.AreEquivalent<ITestSelectionService>(null!, null!));
        Assert.False(SelectionContextHashHelper.AreEquivalent(context, null!));
        Assert.False(SelectionContextHashHelper.AreEquivalent(null!, context));
    }

    [Fact]
    public void SelectionContextHashHelper_GetHashCode_NullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            SelectionContextHashHelper.GetHashCode<ITestSelectionService>(null!));
    }

    private ISelectionContext<ITestSelectionService> CreateTestContext(string? providerId = null)
    {
        var provider = new TestSelectionService(providerId ?? "TestProvider");
        var registration = CreateRegistration(provider, Priority.Normal, DateTime.UtcNow, providerId);
        var registrations = new List<IProviderRegistration> { registration };

        return new SelectionContext<ITestSelectionService>(registrations);
    }

    private static IProviderRegistration CreateRegistration(
        ITestSelectionService provider,
        Priority priority,
        DateTime registeredAt,
        string? providerId = null)
    {
        var capabilities = new ProviderCapabilities
        {
            ProviderId = providerId ?? $"test-provider-{provider.Name}",
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

    /// <summary>
    /// Test service interface for cache testing.
    /// </summary>
    public interface ITestSelectionService
    {
        string Name { get; }
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
    }

    private sealed class TestProviderRegistration : IProviderRegistration
    {
        public Type ServiceType { get; init; } = null!;
        public object Provider { get; init; } = null!;
        public ProviderCapabilities Capabilities { get; init; } = null!;
        public bool IsActive { get; init; } = true;
    }
}
