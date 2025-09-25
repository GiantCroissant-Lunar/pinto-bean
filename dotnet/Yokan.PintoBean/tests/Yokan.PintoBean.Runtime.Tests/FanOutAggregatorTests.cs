using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for FanOut aggregation behavior demonstrating Continue vs FailFast error policies.
/// </summary>
public class FanOutAggregatorTests
{
    /// <summary>
    /// Test provider that can succeed or fail on demand.
    /// </summary>
    public class TestProvider
    {
        public string Name { get; }
        public bool ShouldFail { get; }
        public string Result { get; }
        public string FailureMessage { get; }

        public TestProvider(string name, bool shouldFail = false, string? result = null, string failureMessage = "Test failure")
        {
            Name = name;
            ShouldFail = shouldFail;
            Result = result ?? $"Result from {name}";
            FailureMessage = failureMessage;
        }

        public string GetResult()
        {
            if (ShouldFail)
                throw new InvalidOperationException(FailureMessage);
            return Result;
        }

        public void DoWork()
        {
            if (ShouldFail)
                throw new InvalidOperationException(FailureMessage);
        }

        public async Task<string> GetResultAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken); // Simulate async work
            if (ShouldFail)
                throw new InvalidOperationException(FailureMessage);
            return Result;
        }

        public async Task DoWorkAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken); // Simulate async work
            if (ShouldFail)
                throw new InvalidOperationException(FailureMessage);
        }
    }

    #region Synchronous Aggregation Tests

    [Fact]
    public void Aggregate_WithAllSuccessfulProviders_ContinuePolicy_ReturnsFirstResult()
    {
        // Arrange
        var providers = new[]
        {
            new TestProvider("Provider1", result: "Result1"),
            new TestProvider("Provider2", result: "Result2"),
            new TestProvider("Provider3", result: "Result3")
        };
        var options = FanOutAggregationOptions<string>.WithErrorPolicy(FanOutErrorPolicy.Continue);

        // Act
        var result = FanOutAggregator.Aggregate(
            providers,
            provider => ((TestProvider)provider).GetResult(),
            options);

        // Assert
        Assert.Equal("Result1", result); // Default reduction returns first successful result
    }

    [Fact]
    public void Aggregate_WithAllSuccessfulProviders_FailFastPolicy_ReturnsFirstResult()
    {
        // Arrange
        var providers = new[]
        {
            new TestProvider("Provider1", result: "Result1"),
            new TestProvider("Provider2", result: "Result2"),
            new TestProvider("Provider3", result: "Result3")
        };
        var options = FanOutAggregationOptions<string>.WithErrorPolicy(FanOutErrorPolicy.FailFast);

        // Act
        var result = FanOutAggregator.Aggregate(
            providers,
            provider => ((TestProvider)provider).GetResult(),
            options);

        // Assert
        Assert.Equal("Result1", result); // Default reduction returns first successful result
    }

    [Fact]
    public void Aggregate_WithSomeFailures_ContinuePolicy_ReturnsFirstSuccessfulResult()
    {
        // Arrange
        var providers = new[]
        {
            new TestProvider("Provider1", shouldFail: true),
            new TestProvider("Provider2", result: "Success2"),
            new TestProvider("Provider3", result: "Success3")
        };
        var options = FanOutAggregationOptions<string>.WithErrorPolicy(FanOutErrorPolicy.Continue);

        // Act
        var result = FanOutAggregator.Aggregate(
            providers,
            provider => ((TestProvider)provider).GetResult(),
            options);

        // Assert
        Assert.Equal("Success2", result); // First successful result after failure
    }

    [Fact]
    public void Aggregate_WithFirstProviderFailure_FailFastPolicy_ThrowsImmediately()
    {
        // Arrange
        var providers = new[]
        {
            new TestProvider("Provider1", shouldFail: true, failureMessage: "Provider1 failed"),
            new TestProvider("Provider2", result: "Success2"),
            new TestProvider("Provider3", result: "Success3")
        };
        var options = FanOutAggregationOptions<string>.WithErrorPolicy(FanOutErrorPolicy.FailFast);

        // Act & Assert
        var exception = Assert.Throws<AggregateException>(() =>
            FanOutAggregator.Aggregate(
                providers,
                provider => ((TestProvider)provider).GetResult(),
                options));

        Assert.Contains("FailFast policy", exception.Message);
        Assert.Contains("Provider1", exception.Message);
        Assert.Single(exception.InnerExceptions);
        Assert.Contains("Provider1 failed", exception.InnerExceptions.First().Message);
    }

    [Fact]
    public void Aggregate_WithAllFailures_ContinuePolicy_ThrowsAggregateException()
    {
        // Arrange
        var providers = new[]
        {
            new TestProvider("Provider1", shouldFail: true, failureMessage: "Error1"),
            new TestProvider("Provider2", shouldFail: true, failureMessage: "Error2"),
            new TestProvider("Provider3", shouldFail: true, failureMessage: "Error3")
        };
        var options = FanOutAggregationOptions<string>.WithErrorPolicy(FanOutErrorPolicy.Continue);

        // Act & Assert
        var exception = Assert.Throws<AggregateException>(() =>
            FanOutAggregator.Aggregate(
                providers,
                provider => ((TestProvider)provider).GetResult(),
                options));

        Assert.Contains("All providers failed", exception.Message);
        Assert.Equal(3, exception.InnerExceptions.Count);
    }

    [Fact]
    public void Aggregate_WithCustomReduceFunction_AppliesReduction()
    {
        // Arrange
        var providers = new[]
        {
            new TestProvider("Provider1", result: "A"),
            new TestProvider("Provider2", result: "B"),
            new TestProvider("Provider3", result: "C")
        };
        var options = FanOutAggregationOptions<string>.Create(
            FanOutErrorPolicy.Continue,
            results => string.Join(",", results.OrderBy(r => r)));

        // Act
        var result = FanOutAggregator.Aggregate(
            providers,
            provider => ((TestProvider)provider).GetResult(),
            options);

        // Assert
        Assert.Equal("A,B,C", result);
    }

    [Fact]
    public void Aggregate_WithCustomReduceFunctionAndFailures_ContinuePolicy_AppliesReductionToSuccessfulResults()
    {
        // Arrange
        var providers = new[]
        {
            new TestProvider("Provider1", shouldFail: true),
            new TestProvider("Provider2", result: "B"),
            new TestProvider("Provider3", result: "C")
        };
        var options = FanOutAggregationOptions<string>.Create(
            FanOutErrorPolicy.Continue,
            results => string.Join(",", results.OrderBy(r => r)));

        // Act
        var result = FanOutAggregator.Aggregate(
            providers,
            provider => ((TestProvider)provider).GetResult(),
            options);

        // Assert
        Assert.Equal("B,C", result);
    }

    #endregion

    #region Asynchronous Aggregation Tests

    [Fact]
    public async Task AggregateAsync_WithAllSuccessfulProviders_ContinuePolicy_ReturnsFirstResult()
    {
        // Arrange
        var providers = new[]
        {
            new TestProvider("Provider1", result: "Result1"),
            new TestProvider("Provider2", result: "Result2"),
            new TestProvider("Provider3", result: "Result3")
        };
        var options = FanOutAggregationOptions<string>.WithErrorPolicy(FanOutErrorPolicy.Continue);

        // Act
        var result = await FanOutAggregator.AggregateAsync(
            providers,
            (provider, ct) => ((TestProvider)provider).GetResultAsync(ct),
            options);

        // Assert
        Assert.Equal("Result1", result);
    }

    [Fact]
    public async Task AggregateAsync_WithFirstProviderFailure_FailFastPolicy_ThrowsImmediately()
    {
        // Arrange
        var providers = new[]
        {
            new TestProvider("Provider1", shouldFail: true, failureMessage: "Async failure"),
            new TestProvider("Provider2", result: "Success2"),
            new TestProvider("Provider3", result: "Success3")
        };
        var options = FanOutAggregationOptions<string>.WithErrorPolicy(FanOutErrorPolicy.FailFast);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AggregateException>(() =>
            FanOutAggregator.AggregateAsync(
                providers,
                (provider, ct) => ((TestProvider)provider).GetResultAsync(ct),
                options));

        Assert.Contains("FailFast policy", exception.Message);
        Assert.Contains("Async failure", exception.InnerExceptions.First().Message);
    }

    [Fact]
    public async Task AggregateAsync_WithSomeFailures_ContinuePolicy_ReturnsFirstSuccessfulResult()
    {
        // Arrange
        var providers = new[]
        {
            new TestProvider("Provider1", shouldFail: true),
            new TestProvider("Provider2", result: "AsyncSuccess2"),
            new TestProvider("Provider3", result: "AsyncSuccess3")
        };
        var options = FanOutAggregationOptions<string>.WithErrorPolicy(FanOutErrorPolicy.Continue);

        // Act
        var result = await FanOutAggregator.AggregateAsync(
            providers,
            (provider, ct) => ((TestProvider)provider).GetResultAsync(ct),
            options);

        // Assert
        Assert.Equal("AsyncSuccess2", result);
    }

    #endregion

    #region Void Operation Tests

    [Fact]
    public void ExecuteAll_WithAllSuccessfulProviders_ContinuePolicy_CompletesSuccessfully()
    {
        // Arrange
        var providers = new[]
        {
            new TestProvider("Provider1"),
            new TestProvider("Provider2"),
            new TestProvider("Provider3")
        };

        // Act & Assert - Should not throw
        FanOutAggregator.ExecuteAll(
            providers,
            provider => ((TestProvider)provider).DoWork(),
            FanOutErrorPolicy.Continue);
    }

    [Fact]
    public void ExecuteAll_WithFirstProviderFailure_FailFastPolicy_ThrowsImmediately()
    {
        // Arrange
        var providers = new[]
        {
            new TestProvider("Provider1", shouldFail: true, failureMessage: "DoWork failed"),
            new TestProvider("Provider2"),
            new TestProvider("Provider3")
        };

        // Act & Assert
        var exception = Assert.Throws<AggregateException>(() =>
            FanOutAggregator.ExecuteAll(
                providers,
                provider => ((TestProvider)provider).DoWork(),
                FanOutErrorPolicy.FailFast));

        Assert.Contains("FailFast policy", exception.Message);
        Assert.Contains("DoWork failed", exception.InnerExceptions.First().Message);
    }

    [Fact]
    public void ExecuteAll_WithSomeFailures_ContinuePolicy_ThrowsAggregateException()
    {
        // Arrange
        var providers = new[]
        {
            new TestProvider("Provider1", shouldFail: true),
            new TestProvider("Provider2"), // Success
            new TestProvider("Provider3", shouldFail: true)
        };

        // Act & Assert
        var exception = Assert.Throws<AggregateException>(() =>
            FanOutAggregator.ExecuteAll(
                providers,
                provider => ((TestProvider)provider).DoWork(),
                FanOutErrorPolicy.Continue));

        Assert.Contains("Some providers failed", exception.Message);
        Assert.Equal(2, exception.InnerExceptions.Count); // Two failures
    }

    [Fact]
    public async Task ExecuteAllAsync_WithAllSuccessfulProviders_ContinuePolicy_CompletesSuccessfully()
    {
        // Arrange
        var providers = new[]
        {
            new TestProvider("Provider1"),
            new TestProvider("Provider2"),
            new TestProvider("Provider3")
        };

        // Act & Assert - Should not throw
        await FanOutAggregator.ExecuteAllAsync(
            providers,
            (provider, ct) => ((TestProvider)provider).DoWorkAsync(ct),
            FanOutErrorPolicy.Continue);
    }

    [Fact]
    public async Task ExecuteAllAsync_WithFirstProviderFailure_FailFastPolicy_ThrowsImmediately()
    {
        // Arrange
        var providers = new[]
        {
            new TestProvider("Provider1", shouldFail: true, failureMessage: "Async DoWork failed"),
            new TestProvider("Provider2"),
            new TestProvider("Provider3")
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AggregateException>(() =>
            FanOutAggregator.ExecuteAllAsync(
                providers,
                (provider, ct) => ((TestProvider)provider).DoWorkAsync(ct),
                FanOutErrorPolicy.FailFast));

        Assert.Contains("FailFast policy", exception.Message);
        Assert.Contains("Async DoWork failed", exception.InnerExceptions.First().Message);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Aggregate_WithNullProviders_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            FanOutAggregator.Aggregate<string>(
                null!,
                provider => "result"));
    }

    [Fact]
    public void Aggregate_WithNullInvokeFunc_ThrowsArgumentNullException()
    {
        // Arrange
        var providers = new[] { new TestProvider("Provider1") };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            FanOutAggregator.Aggregate<string>(
                providers,
                null!));
    }

    [Fact]
    public void Aggregate_WithEmptyProviders_ThrowsInvalidOperationException()
    {
        // Arrange
        var providers = Array.Empty<TestProvider>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            FanOutAggregator.Aggregate(
                providers,
                provider => ((TestProvider)provider).GetResult()));

        Assert.Contains("No providers were available", exception.Message);
    }

    [Fact]
    public void Aggregate_WithNullOptions_UsesDefaults()
    {
        // Arrange
        var providers = new[]
        {
            new TestProvider("Provider1", result: "Result1"),
            new TestProvider("Provider2", result: "Result2")
        };

        // Act
        var result = FanOutAggregator.Aggregate(
            providers,
            provider => ((TestProvider)provider).GetResult(),
            options: null); // Null options should use defaults

        // Assert
        Assert.Equal("Result1", result); // Default behavior: first result, Continue policy
    }

    #endregion
}