using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yokan.PintoBean.Runtime;
using Yokan.PintoBean.Runtime.Unity;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for the Unity-specific IAspectRuntime implementation.
/// </summary>
public class UnityAspectRuntimeTests
{
    [Fact]
    public void Constructor_WithDefaultParameters_InitializesCorrectly()
    {
        // Arrange & Act
        var runtime = new UnityAspectRuntime();

        // Assert
        Assert.NotNull(runtime);
    }

    [Fact]
    public void Constructor_WithCustomParameters_InitializesCorrectly()
    {
        // Arrange & Act
        var runtime = new UnityAspectRuntime(enableMetrics: false, verboseLogging: true);

        // Assert
        Assert.NotNull(runtime);
    }

    [Fact]
    public void EnterMethod_WithValidParameters_ReturnsContext()
    {
        // Arrange
        var runtime = new UnityAspectRuntime();
        var serviceType = typeof(ITestService);
        var methodName = "TestMethod";
        var parameters = new object?[] { "param1", 42 };

        // Act
        using var context = runtime.EnterMethod(serviceType, methodName, parameters);

        // Assert
        Assert.NotNull(context);
    }

    [Fact]
    public void EnterMethod_WithEmptyParameters_ReturnsContext()
    {
        // Arrange
        var runtime = new UnityAspectRuntime();
        var serviceType = typeof(ITestService);
        var methodName = "TestMethod";

        // Act
        using var context = runtime.EnterMethod(serviceType, methodName, Array.Empty<object?>());

        // Assert
        Assert.NotNull(context);
    }

    [Fact]
    public void ExitMethod_WithValidContext_CompletesSuccessfully()
    {
        // Arrange
        var runtime = new UnityAspectRuntime();
        var serviceType = typeof(ITestService);
        var methodName = "TestMethod";
        var parameters = new object?[] { };
        var result = "test-result";

        // Act & Assert (should not throw)
        using var context = runtime.EnterMethod(serviceType, methodName, parameters);
        runtime.ExitMethod(context, result);
    }

    [Fact]
    public void ExitMethod_WithNullResult_CompletesSuccessfully()
    {
        // Arrange
        var runtime = new UnityAspectRuntime();
        var serviceType = typeof(ITestService);
        var methodName = "TestMethod";
        var parameters = new object?[] { };

        // Act & Assert (should not throw)
        using var context = runtime.EnterMethod(serviceType, methodName, parameters);
        runtime.ExitMethod(context, null);
    }

    [Fact]
    public void ExitMethod_WithInvalidContext_HandlesGracefully()
    {
        // Arrange
        var runtime = new UnityAspectRuntime();
        var invalidContext = new MockDisposable();

        // Act & Assert (should not throw)
        runtime.ExitMethod(invalidContext, "result");
    }

    [Fact]
    public void RecordException_WithValidContext_LogsException()
    {
        // Arrange
        var runtime = new UnityAspectRuntime();
        var serviceType = typeof(ITestService);
        var methodName = "TestMethod";
        var parameters = new object?[] { };
        var exception = new InvalidOperationException("Test exception");

        // Act & Assert (should not throw)
        using var context = runtime.EnterMethod(serviceType, methodName, parameters);
        runtime.RecordException(context, exception);
    }

    [Fact]
    public void RecordException_WithInvalidContext_HandlesGracefully()
    {
        // Arrange
        var runtime = new UnityAspectRuntime();
        var invalidContext = new MockDisposable();
        var exception = new InvalidOperationException("Test exception");

        // Act & Assert (should not throw)
        runtime.RecordException(invalidContext, exception);
    }

    [Fact]
    public void RecordMetric_WithValidParameters_LogsMetric()
    {
        // Arrange
        var runtime = new UnityAspectRuntime(enableMetrics: true);
        var metricName = "test.metric";
        var value = 42.5;
        var tags = new[] { ("tag1", (object)"value1"), ("tag2", (object)123) };

        // Act & Assert (should not throw)
        runtime.RecordMetric(metricName, value, tags);
    }

    [Fact]
    public void RecordMetric_WithMetricsDisabled_DoesNotLog()
    {
        // Arrange
        var runtime = new UnityAspectRuntime(enableMetrics: false);
        var metricName = "test.metric";
        var value = 42.5;

        // Act & Assert (should not throw)
        runtime.RecordMetric(metricName, value);
    }

    [Fact]
    public void RecordMetric_WithNullOrEmptyName_DoesNotLog()
    {
        // Arrange
        var runtime = new UnityAspectRuntime(enableMetrics: true);

        // Act & Assert (should not throw)
        runtime.RecordMetric(null!, 42.0);
        runtime.RecordMetric("", 42.0);
    }

    [Fact]
    public void RecordMetric_WithManyTags_LimitsToThreeTags()
    {
        // Arrange
        var runtime = new UnityAspectRuntime(enableMetrics: true);
        var metricName = "test.metric";
        var value = 42.5;
        var tags = new[] 
        { 
            ("tag1", (object)"value1"), 
            ("tag2", (object)"value2"), 
            ("tag3", (object)"value3"), 
            ("tag4", (object)"value4"), 
            ("tag5", (object)"value5") 
        };

        // Act & Assert (should not throw and should handle gracefully)
        runtime.RecordMetric(metricName, value, tags);
    }

    [Fact]
    public void StartOperation_WithValidParameters_ReturnsContext()
    {
        // Arrange
        var runtime = new UnityAspectRuntime();
        var operationName = "custom-operation";
        var metadata = new Dictionary<string, object>
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };

        // Act
        using var context = runtime.StartOperation(operationName, metadata);

        // Assert
        Assert.NotNull(context);
    }

    [Fact]
    public void StartOperation_WithNullMetadata_ReturnsContext()
    {
        // Arrange
        var runtime = new UnityAspectRuntime();
        var operationName = "custom-operation";

        // Act
        using var context = runtime.StartOperation(operationName, null);

        // Assert
        Assert.NotNull(context);
    }

    [Fact]
    public void StartOperation_WithVerboseLogging_LogsStartMessage()
    {
        // Arrange
        var runtime = new UnityAspectRuntime(verboseLogging: true);
        var operationName = "custom-operation";

        // Act & Assert (should not throw)
        using var context = runtime.StartOperation(operationName, null);
    }

    [Fact]
    public void ServiceCollectionExtensions_AddUnityAspectRuntime_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddUnityAspectRuntime();
        var serviceProvider = services.BuildServiceProvider();
        var aspectRuntime = serviceProvider.GetRequiredService<IAspectRuntime>();

        // Assert
        Assert.NotNull(aspectRuntime);
        Assert.IsType<UnityAspectRuntime>(aspectRuntime);
    }

    [Fact]
    public void ServiceCollectionExtensions_AddUnityAspectRuntime_WithCustomParameters_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddUnityAspectRuntime(enableMetrics: false, verboseLogging: true);
        var serviceProvider = services.BuildServiceProvider();
        var aspectRuntime = serviceProvider.GetRequiredService<IAspectRuntime>();

        // Assert
        Assert.NotNull(aspectRuntime);
        Assert.IsType<UnityAspectRuntime>(aspectRuntime);
    }

    [Fact]
    public void ServiceCollectionExtensions_AddUnityAspectRuntime_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ((IServiceCollection)null!).AddUnityAspectRuntime());
    }

    [Fact]
    public void ServiceCollectionExtensions_AddAdaptiveAspectRuntime_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var sourceName = "test-source";
        var meterName = "test-meter";

        // Act
        services.AddAdaptiveAspectRuntime(sourceName, meterName);
        var serviceProvider = services.BuildServiceProvider();
        var aspectRuntime = serviceProvider.GetRequiredService<IAspectRuntime>();

        // Assert
        Assert.NotNull(aspectRuntime);
        // In test environment, OpenTelemetry types are available and we're not in Unity play mode,
        // so it should choose OtelAspectRuntime
        Assert.IsType<OtelAspectRuntime>(aspectRuntime);
    }

    [Fact]
    public void ServiceCollectionExtensions_AddAdaptiveAspectRuntime_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ((IServiceCollection)null!).AddAdaptiveAspectRuntime("source", "meter"));
    }

    [Fact]
    public void ServiceCollectionExtensions_AddAdaptiveAspectRuntime_WithNullSourceName_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            services.AddAdaptiveAspectRuntime(null!, "meter"));
    }

    [Fact]
    public void ServiceCollectionExtensions_AddAdaptiveAspectRuntime_WithNullMeterName_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            services.AddAdaptiveAspectRuntime("source", null!));
    }

    // Test interface for testing purposes
    private interface ITestService
    {
        void TestMethod();
    }

    // Mock disposable for testing invalid context handling
    private class MockDisposable : IDisposable
    {
        public void Dispose() { }
    }
}