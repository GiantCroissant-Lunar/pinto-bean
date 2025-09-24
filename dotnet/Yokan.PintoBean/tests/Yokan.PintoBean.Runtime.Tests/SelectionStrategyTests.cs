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