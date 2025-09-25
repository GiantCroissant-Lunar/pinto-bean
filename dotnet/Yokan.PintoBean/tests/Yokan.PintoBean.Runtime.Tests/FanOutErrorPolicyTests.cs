using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for FanOut error policy functionality and result aggregation behavior.
/// </summary>
public class FanOutErrorPolicyTests
{
    /// <summary>
    /// Test service interface for error policy testing.
    /// </summary>
    public interface ITestErrorService
    {
        string GetResult();
        void DoWork();
    }

    /// <summary>
    /// Test service implementation that can succeed or fail.
    /// </summary>
    public class TestErrorService : ITestErrorService
    {
        public string Name { get; }
        public bool ShouldFail { get; }
        public string FailureMessage { get; }

        public TestErrorService(string name, bool shouldFail = false, string failureMessage = "Test failure")
        {
            Name = name;
            ShouldFail = shouldFail;
            FailureMessage = failureMessage;
        }

        public string GetResult()
        {
            if (ShouldFail)
                throw new InvalidOperationException(FailureMessage);
            return $"Result from {Name}";
        }

        public void DoWork()
        {
            if (ShouldFail)
                throw new InvalidOperationException(FailureMessage);
        }
    }

    [Fact]
    public void FanOutSelectionStrategy_DefaultErrorPolicy_IsContinue()
    {
        // Arrange
        var provider = new TestErrorService("TestProvider");
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(provider, Priority.Normal, DateTime.UtcNow)
        };

        var context = new SelectionContext<ITestErrorService>(registrations);
        var strategy = new FanOutSelectionStrategy<ITestErrorService>();

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.NotNull(result.SelectionMetadata);
        Assert.Equal("Continue", result.SelectionMetadata!["ErrorPolicy"]);
    }

    [Fact]
    public void FanOutSelectionStrategy_WithContinueErrorPolicy_IncludesErrorPolicyInMetadata()
    {
        // Arrange
        var provider = new TestErrorService("TestProvider");
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(provider, Priority.Normal, DateTime.UtcNow)
        };

        var context = new SelectionContext<ITestErrorService>(registrations);
        var strategy = new FanOutSelectionStrategy<ITestErrorService>(errorPolicy: FanOutErrorPolicy.Continue);

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.NotNull(result.SelectionMetadata);
        Assert.Equal("Continue", result.SelectionMetadata!["ErrorPolicy"]);
        Assert.Equal(SelectionStrategyType.FanOut, result.StrategyType);
    }

    [Fact]
    public void FanOutSelectionStrategy_WithFailFastErrorPolicy_IncludesErrorPolicyInMetadata()
    {
        // Arrange
        var provider = new TestErrorService("TestProvider");
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(provider, Priority.Normal, DateTime.UtcNow)
        };

        var context = new SelectionContext<ITestErrorService>(registrations);
        var strategy = new FanOutSelectionStrategy<ITestErrorService>(errorPolicy: FanOutErrorPolicy.FailFast);

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.NotNull(result.SelectionMetadata);
        Assert.Equal("FailFast", result.SelectionMetadata!["ErrorPolicy"]);
        Assert.Equal(SelectionStrategyType.FanOut, result.StrategyType);
    }

    [Fact]
    public void DefaultSelectionStrategies_CreateFanOut_WithDefaultErrorPolicy_CreatesStrategyWithContinue()
    {
        // Act
        var strategy = DefaultSelectionStrategies.CreateFanOut<ITestErrorService>();

        // Arrange
        var provider = new TestErrorService("TestProvider");
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(provider, Priority.Normal, DateTime.UtcNow)
        };
        var context = new SelectionContext<ITestErrorService>(registrations);

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.NotNull(strategy);
        Assert.Equal(SelectionStrategyType.FanOut, strategy.StrategyType);
        Assert.Equal("Continue", result.SelectionMetadata!["ErrorPolicy"]);
    }

    [Fact]
    public void DefaultSelectionStrategies_CreateFanOut_WithExplicitContinuePolicy_CreatesStrategyCorrectly()
    {
        // Act
        var strategy = DefaultSelectionStrategies.CreateFanOut<ITestErrorService>(
            errorPolicy: FanOutErrorPolicy.Continue);

        // Arrange
        var provider = new TestErrorService("TestProvider");
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(provider, Priority.Normal, DateTime.UtcNow)
        };
        var context = new SelectionContext<ITestErrorService>(registrations);

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.NotNull(strategy);
        Assert.Equal(SelectionStrategyType.FanOut, strategy.StrategyType);
        Assert.Equal("Continue", result.SelectionMetadata!["ErrorPolicy"]);
    }

    [Fact]
    public void DefaultSelectionStrategies_CreateFanOut_WithFailFastPolicy_CreatesStrategyCorrectly()
    {
        // Act
        var strategy = DefaultSelectionStrategies.CreateFanOut<ITestErrorService>(
            errorPolicy: FanOutErrorPolicy.FailFast);

        // Arrange
        var provider = new TestErrorService("TestProvider");
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(provider, Priority.Normal, DateTime.UtcNow)
        };
        var context = new SelectionContext<ITestErrorService>(registrations);

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.NotNull(strategy);
        Assert.Equal(SelectionStrategyType.FanOut, strategy.StrategyType);
        Assert.Equal("FailFast", result.SelectionMetadata!["ErrorPolicy"]);
    }

    [Fact]
    public void FanOutSelectionStrategy_WithMultipleProviders_SelectsAllProvidersRegardlessOfErrorPolicy()
    {
        // Arrange
        var provider1 = new TestErrorService("Provider1");
        var provider2 = new TestErrorService("Provider2", shouldFail: true);
        var provider3 = new TestErrorService("Provider3");

        var baseTime = DateTime.UtcNow;
        var registrations = new List<IProviderRegistration>
        {
            CreateRegistration(provider1, Priority.Normal, baseTime),
            CreateRegistration(provider2, Priority.Normal, baseTime.AddSeconds(1)),
            CreateRegistration(provider3, Priority.Normal, baseTime.AddSeconds(2))
        };

        // Test both error policies - selection behavior should be the same
        var continueContext = new SelectionContext<ITestErrorService>(registrations);
        var failFastContext = new SelectionContext<ITestErrorService>(registrations);
        
        var continueStrategy = new FanOutSelectionStrategy<ITestErrorService>(errorPolicy: FanOutErrorPolicy.Continue);
        var failFastStrategy = new FanOutSelectionStrategy<ITestErrorService>(errorPolicy: FanOutErrorPolicy.FailFast);

        // Act
        var continueResult = continueStrategy.SelectProviders(continueContext);
        var failFastResult = failFastStrategy.SelectProviders(failFastContext);

        // Assert - both should select all providers (error policy affects execution, not selection)
        Assert.Equal(3, continueResult.SelectedProviders.Count);
        Assert.Equal(3, failFastResult.SelectedProviders.Count);
        Assert.Equal("Continue", continueResult.SelectionMetadata!["ErrorPolicy"]);
        Assert.Equal("FailFast", failFastResult.SelectionMetadata!["ErrorPolicy"]);
    }

    [Fact]
    public void FanOutAggregationOptions_Default_HasCorrectDefaults()
    {
        // Act
        var options = FanOutAggregationOptions<string>.Default();

        // Assert
        Assert.Equal(FanOutErrorPolicy.Continue, options.OnError);
        Assert.Null(options.ReduceFunction);
    }

    [Fact]
    public void FanOutAggregationOptions_WithErrorPolicy_SetsErrorPolicyCorrectly()
    {
        // Act
        var continueOptions = FanOutAggregationOptions<string>.WithErrorPolicy(FanOutErrorPolicy.Continue);
        var failFastOptions = FanOutAggregationOptions<string>.WithErrorPolicy(FanOutErrorPolicy.FailFast);

        // Assert
        Assert.Equal(FanOutErrorPolicy.Continue, continueOptions.OnError);
        Assert.Equal(FanOutErrorPolicy.FailFast, failFastOptions.OnError);
        Assert.Null(continueOptions.ReduceFunction);
        Assert.Null(failFastOptions.ReduceFunction);
    }

    [Fact]
    public void FanOutAggregationOptions_WithReduceFunction_SetsReduceFunctionCorrectly()
    {
        // Arrange
        var reduceFunction = new Func<IEnumerable<string>, string>(results => results.First());

        // Act
        var options = FanOutAggregationOptions<string>.WithReduceFunction(reduceFunction);

        // Assert
        Assert.Equal(FanOutErrorPolicy.Continue, options.OnError); // Default
        Assert.Same(reduceFunction, options.ReduceFunction);
    }

    [Fact]
    public void FanOutAggregationOptions_Create_WithBothParameters_SetsAllCorrectly()
    {
        // Arrange
        var reduceFunction = new Func<IEnumerable<string>, string>(results => results.Last());

        // Act
        var options = FanOutAggregationOptions<string>.Create(FanOutErrorPolicy.FailFast, reduceFunction);

        // Assert
        Assert.Equal(FanOutErrorPolicy.FailFast, options.OnError);
        Assert.Same(reduceFunction, options.ReduceFunction);
    }

    [Fact]
    public void FanOutAggregationOptions_Create_WithOnlyErrorPolicy_SetsErrorPolicyAndNullReduceFunction()
    {
        // Act
        var options = FanOutAggregationOptions<string>.Create(FanOutErrorPolicy.FailFast);

        // Assert
        Assert.Equal(FanOutErrorPolicy.FailFast, options.OnError);
        Assert.Null(options.ReduceFunction);
    }

    [Fact]
    public void FanOutErrorPolicy_EnumValues_AreCorrect()
    {
        // Assert enum values are as expected
        Assert.Equal(0, (int)FanOutErrorPolicy.Continue);
        Assert.Equal(1, (int)FanOutErrorPolicy.FailFast);
    }

    private static IProviderRegistration CreateRegistration(
        ITestErrorService provider,
        Priority priority,
        DateTime registeredAt)
    {
        var capabilities = new ProviderCapabilities
        {
            ProviderId = $"test-error-provider-{Guid.NewGuid()}",
            Priority = priority,
            RegisteredAt = registeredAt,
            Platform = Platform.Any
        };

        return new TestProviderRegistration
        {
            ServiceType = typeof(ITestErrorService),
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