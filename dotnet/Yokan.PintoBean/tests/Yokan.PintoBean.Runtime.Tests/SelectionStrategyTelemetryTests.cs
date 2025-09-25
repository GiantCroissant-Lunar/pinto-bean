using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests that verify strategy telemetry hooks are invoked with correct metrics.
/// </summary>
public class SelectionStrategyTelemetryTests
{
    /// <summary>
    /// Test implementation of IAspectRuntime that collects recorded metrics for validation.
    /// </summary>
    public class TestAspectRuntime : IAspectRuntime
    {
        public List<MetricRecord> RecordedMetrics { get; } = new();

        public IDisposable EnterMethod(Type serviceType, string methodName, object?[] parameters)
        {
            return new TestContext();
        }

        public void ExitMethod(IDisposable context, object? result)
        {
            // No-op for testing
        }

        public void RecordException(IDisposable context, Exception exception)
        {
            // No-op for testing
        }

        public void RecordMetric(string name, double value, params (string Key, object Value)[] tags)
        {
            RecordedMetrics.Add(new MetricRecord(name, value, tags?.ToDictionary(t => t.Key, t => t.Value) ?? new Dictionary<string, object>()));
        }

        public IDisposable StartOperation(string operationName, IReadOnlyDictionary<string, object>? metadata = null)
        {
            return new TestContext();
        }

        private class TestContext : IDisposable
        {
            public void Dispose() { }
        }
    }

    public record MetricRecord(string Name, double Value, Dictionary<string, object> Tags);

    /// <summary>
    /// Test service interface for telemetry tests.
    /// </summary>
    public interface ITestTelemetryService
    {
        string GetData(string key);
    }

    /// <summary>
    /// Test provider implementation.
    /// </summary>
    public class TestProvider : ITestTelemetryService
    {
        public string Name { get; }

        public TestProvider(string name)
        {
            Name = name;
        }

        public string GetData(string key) => $"{Name}:{key}";
    }

    [Fact]
    public void PickOneStrategy_WithCacheMiss_ShouldRecordCacheMissAndProviderSelected()
    {
        // Arrange
        var testRuntime = new TestAspectRuntime();
        var provider = new TestProvider("TestProvider1");
        var registration = CreateTestRegistration(provider, "provider1");
        var context = CreateTestContext([registration]);

        var strategy = new PickOneSelectionStrategy<ITestTelemetryService>(
            aspectRuntime: testRuntime);

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.Single(result.SelectedProviders);
        Assert.Equal(provider, result.SelectedProviders[0]);

        // Verify telemetry
        var cacheMetrics = testRuntime.RecordedMetrics.Where(m => m.Name.Contains("cache")).ToList();
        var providerMetrics = testRuntime.RecordedMetrics.Where(m => m.Name.Contains("provider")).ToList();

        Assert.Single(cacheMetrics.Where(m => m.Name == "strategy.pickone.cache.miss"));
        Assert.Single(providerMetrics.Where(m => m.Name == "strategy.pickone.provider.selected"));

        var providerMetric = providerMetrics.First();
        Assert.Equal(1.0, providerMetric.Value);
        Assert.Equal("ITestTelemetryService", providerMetric.Tags["service"]);
        Assert.Equal("provider1", providerMetric.Tags["provider"]);
        Assert.Equal("selection", providerMetric.Tags["source"]);
    }

    [Fact]
    public void FanOutStrategy_WithMultipleProviders_ShouldRecordFanoutSizeAndDuration()
    {
        // Arrange
        var testRuntime = new TestAspectRuntime();
        var provider1 = new TestProvider("Provider1");
        var provider2 = new TestProvider("Provider2");
        var registrations = new[]
        {
            CreateTestRegistration(provider1, "provider1"),
            CreateTestRegistration(provider2, "provider2")
        };
        var context = CreateTestContext(registrations);

        var strategy = new FanOutSelectionStrategy<ITestTelemetryService>(
            aspectRuntime: testRuntime);

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.Equal(2, result.SelectedProviders.Count);

        // Verify telemetry
        var sizeMetrics = testRuntime.RecordedMetrics.Where(m => m.Name == "strategy.fanout.size").ToList();
        var durationMetrics = testRuntime.RecordedMetrics.Where(m => m.Name == "strategy.fanout.duration").ToList();

        Assert.Single(sizeMetrics);
        Assert.Single(durationMetrics);

        var sizeMetric = sizeMetrics.First();
        Assert.Equal(2.0, sizeMetric.Value);
        Assert.Equal("ITestTelemetryService", sizeMetric.Tags["service"]);
        Assert.Equal("FanOut", sizeMetric.Tags["strategy"]);

        var durationMetric = durationMetrics.First();
        Assert.True(durationMetric.Value > 0); // Duration should be positive
        Assert.Equal("ITestTelemetryService", durationMetric.Tags["service"]);
        Assert.Equal("FanOut", durationMetric.Tags["strategy"]);
    }

    [Fact]
    public void ShardedStrategy_WithShardKey_ShouldRecordShardKeyAndTarget()
    {
        // Arrange
        var testRuntime = new TestAspectRuntime();
        var provider = new TestProvider("ShardedProvider");
        var registration = CreateTestRegistration(provider, "shard-provider");
        var context = CreateTestContext([registration], new Dictionary<string, object>
        {
            ["EventName"] = "player.level.complete"
        });

        var strategy = new ShardedSelectionStrategy<ITestTelemetryService>(
            keyExtractor: DefaultSelectionStrategies.ExtractAnalyticsShardKey,
            aspectRuntime: testRuntime);

        // Act
        var result = strategy.SelectProviders(context);

        // Assert
        Assert.Single(result.SelectedProviders);
        Assert.Equal(provider, result.SelectedProviders[0]);

        // Verify telemetry
        var shardKeyMetrics = testRuntime.RecordedMetrics.Where(m => m.Name == "strategy.sharded.shard_key").ToList();
        var targetMetrics = testRuntime.RecordedMetrics.Where(m => m.Name == "strategy.sharded.target").ToList();

        Assert.Single(shardKeyMetrics);
        Assert.Single(targetMetrics);

        var shardKeyMetric = shardKeyMetrics.First();
        Assert.Equal(1.0, shardKeyMetric.Value);
        Assert.Equal("ITestTelemetryService", shardKeyMetric.Tags["service"]);
        Assert.Equal("player", shardKeyMetric.Tags["shard_key"]);
        Assert.Equal("Sharded", shardKeyMetric.Tags["strategy"]);

        var targetMetric = targetMetrics.First();
        Assert.Equal(1.0, targetMetric.Value);
        Assert.Equal("ITestTelemetryService", targetMetric.Tags["service"]);
        Assert.Equal("player", targetMetric.Tags["shard_key"]);
        Assert.Equal("shard-provider", targetMetric.Tags["target_provider"]);
    }

    [Fact]
    public void NoOpAspectRuntime_ShouldCompileAndNotThrow()
    {
        // Arrange
        var noOpRuntime = NoOpAspectRuntime.Instance;
        var provider = new TestProvider("NoOpProvider");
        var registration = CreateTestRegistration(provider, "noop-provider");
        var context = CreateTestContext([registration]);

        var strategy = new PickOneSelectionStrategy<ITestTelemetryService>(
            aspectRuntime: noOpRuntime);

        // Act & Assert - Should not throw
        var result = strategy.SelectProviders(context);
        Assert.Single(result.SelectedProviders);
        Assert.Equal(provider, result.SelectedProviders[0]);

        // Verify no-op behavior doesn't interfere with functionality
        Assert.Equal(SelectionStrategyType.PickOne, result.StrategyType);
    }

    [Fact]
    public void IAspectRuntime_StartOperation_ShouldReturnDisposableContext()
    {
        // Arrange
        var testRuntime = new TestAspectRuntime();
        var metadata = new Dictionary<string, object>
        {
            ["operation_type"] = "custom",
            ["user_id"] = "test_user"
        };

        // Act
        using var context = testRuntime.StartOperation("test_operation", metadata);

        // Assert
        Assert.NotNull(context);
        // Verify disposal doesn't throw
        // The using statement will dispose the context when it goes out of scope
    }

    [Fact] 
    public void NoOpAspectRuntime_StartOperation_ShouldReturnNoOpContext()
    {
        // Arrange
        var noOpRuntime = NoOpAspectRuntime.Instance;
        var metadata = new Dictionary<string, object>
        {
            ["test_key"] = "test_value"
        };

        // Act
        using var context = noOpRuntime.StartOperation("noop_operation", metadata);

        // Assert  
        Assert.NotNull(context);
        // Verify no-op doesn't interfere with functionality
        // The using statement will dispose the context when it goes out of scope
    }

    private static IProviderRegistration CreateTestRegistration(ITestTelemetryService provider, string providerId)
    {
        var capabilities = new ProviderCapabilities
        {
            ProviderId = providerId,
            Priority = Priority.Normal,
            RegisteredAt = DateTime.UtcNow,
            Platform = Platform.Any
        };

        return new TestProviderRegistration
        {
            ServiceType = typeof(ITestTelemetryService),
            Provider = provider,
            Capabilities = capabilities,
            IsActive = true
        };
    }

    private static ISelectionContext<ITestTelemetryService> CreateTestContext(
        IReadOnlyList<IProviderRegistration> registrations,
        IDictionary<string, object>? metadata = null)
    {
        return new TestSelectionContext(registrations, metadata);
    }

    private sealed class TestProviderRegistration : IProviderRegistration
    {
        public Type ServiceType { get; init; } = null!;
        public object Provider { get; init; } = null!;
        public ProviderCapabilities Capabilities { get; init; } = null!;
        public bool IsActive { get; init; } = true;
    }

    private class TestSelectionContext : ISelectionContext<ITestTelemetryService>
    {
        public Type ServiceType => typeof(ITestTelemetryService);
        public IReadOnlyList<IProviderRegistration> Registrations { get; }
        public IDictionary<string, object>? Metadata { get; }
        public System.Threading.CancellationToken CancellationToken => System.Threading.CancellationToken.None;

        public TestSelectionContext(IReadOnlyList<IProviderRegistration> registrations, IDictionary<string, object>? metadata = null)
        {
            Registrations = registrations;
            Metadata = metadata;
        }
    }
}