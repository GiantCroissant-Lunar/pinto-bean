using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for the OpenTelemetry-backed IAspectRuntime implementation.
/// </summary>
public class OtelAspectRuntimeTests : IDisposable
{
    private readonly OtelAspectRuntime _runtime;
    private readonly ActivityListener _activityListener;
    private readonly List<Activity> _recordedActivities;
    private readonly MeterListener _meterListener;
    private readonly List<(string InstrumentName, double Value, KeyValuePair<string, object?>[] Tags)> _recordedMetrics;

    public OtelAspectRuntimeTests()
    {
        _runtime = new OtelAspectRuntime("test-source", "test-meter");
        _recordedActivities = new List<Activity>();
        _recordedMetrics = new List<(string, double, KeyValuePair<string, object?>[])>();

        // Set up activity listener to capture activities
        _activityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => _recordedActivities.Add(activity),
            ActivityStopped = _ => { }
        };
        ActivitySource.AddActivityListener(_activityListener);

        // Set up meter listener to capture metrics
        _meterListener = new MeterListener();
        _meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "test-meter")
            {
                listener.EnableMeasurementEvents(instrument, null);
            }
        };
        _meterListener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            _recordedMetrics.Add((instrument.Name, measurement, tags.ToArray()));
        });
        _meterListener.Start();
    }

    [Fact]
    public void Constructor_WithValidParameters_InitializesCorrectly()
    {
        // Arrange & Act
        var runtime = new OtelAspectRuntime("source-name", "meter-name");

        // Assert
        Assert.NotNull(runtime);
    }

    [Fact]
    public void Constructor_WithNullSourceName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OtelAspectRuntime(null!, "meter-name"));
    }

    [Fact]
    public void Constructor_WithNullMeterName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OtelAspectRuntime("source-name", null!));
    }

    [Fact]
    public void EnterMethod_CreatesActivityWithCorrectTags()
    {
        // Arrange
        var serviceType = typeof(ITestService);
        var methodName = "TestMethod";
        var parameters = new object?[] { "param1", 42 };

        // Act
        using var context = _runtime.EnterMethod(serviceType, methodName, parameters);

        // Assert
        Assert.Single(_recordedActivities);
        var activity = _recordedActivities[0];
        Assert.Equal($"{serviceType.Name}.{methodName}", activity.OperationName);
        Assert.Equal(serviceType.FullName, activity.GetTagItem("service.type"));
        Assert.Equal(methodName, activity.GetTagItem("method.name"));
        Assert.Equal(2, activity.GetTagItem("parameter.count"));
    }

    [Fact]
    public void ExitMethod_WithValidContext_SetsResultTagAndOkStatus()
    {
        // Arrange
        var serviceType = typeof(ITestService);
        var methodName = "TestMethod";
        var parameters = new object?[] { };
        var result = "test-result";

        // Act
        using var context = _runtime.EnterMethod(serviceType, methodName, parameters);
        _runtime.ExitMethod(context, result);

        // Assert
        Assert.Single(_recordedActivities);
        var activity = _recordedActivities[0];
        Assert.Equal(result.GetType().Name, activity.GetTagItem("method.result.type"));
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
    }

    [Fact]
    public void ExitMethod_WithNullResult_SetsNullResultType()
    {
        // Arrange
        var serviceType = typeof(ITestService);
        var methodName = "TestMethod";
        var parameters = new object?[] { };

        // Act
        using var context = _runtime.EnterMethod(serviceType, methodName, parameters);
        _runtime.ExitMethod(context, null);

        // Assert
        Assert.Single(_recordedActivities);
        var activity = _recordedActivities[0];
        Assert.Equal("null", activity.GetTagItem("method.result.type"));
    }

    [Fact]
    public void RecordException_WithValidContext_SetsErrorStatusAndTags()
    {
        // Arrange
        var serviceType = typeof(ITestService);
        var methodName = "TestMethod";
        var parameters = new object?[] { };
        var exception = new InvalidOperationException("Test exception");

        // Act
        using var context = _runtime.EnterMethod(serviceType, methodName, parameters);
        _runtime.RecordException(context, exception);

        // Assert
        Assert.Single(_recordedActivities);
        var activity = _recordedActivities[0];
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal(exception.Message, activity.StatusDescription);
        Assert.Equal(exception.GetType().Name, activity.GetTagItem("exception.type"));
        Assert.Equal(exception.Message, activity.GetTagItem("exception.message"));
        
        // Verify exception event was added
        var events = activity.Events.ToList();
        Assert.Single(events);
        var exceptionEvent = events[0];
        Assert.Equal("exception", exceptionEvent.Name);
    }

    [Fact]
    public void RecordMetric_WithValidParameters_RecordsHistogramMeasurement()
    {
        // Arrange
        var metricName = "test.metric";
        var value = 42.5;
        var tags = new[] { ("tag1", (object)"value1"), ("tag2", (object)123) };

        // Act
        _runtime.RecordMetric(metricName, value, tags);

        // Assert
        Assert.Single(_recordedMetrics);
        var (instrumentName, recordedValue, recordedTags) = _recordedMetrics[0];
        Assert.Equal("pinto_bean_metrics", instrumentName);
        Assert.Equal(value, recordedValue);
        
        // Verify tags are present
        var tagDict = new Dictionary<string, object?>();
        foreach (var tag in recordedTags)
        {
            tagDict[tag.Key] = tag.Value;
        }
        Assert.Equal(metricName, tagDict["metric.name"]);
        Assert.Equal("value1", tagDict["tag1"]);
        Assert.Equal("123", tagDict["tag2"]);
    }

    [Fact]
    public void RecordMetric_WithNullOrEmptyName_DoesNotRecord()
    {
        // Act
        _runtime.RecordMetric(null!, 42.0);
        _runtime.RecordMetric("", 42.0);

        // Assert
        Assert.Empty(_recordedMetrics);
    }

    [Fact]
    public void StartOperation_WithValidParameters_CreatesActivityWithMetadata()
    {
        // Arrange
        var operationName = "custom-operation";
        var metadata = new Dictionary<string, object>
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };

        // Act
        using var context = _runtime.StartOperation(operationName, metadata);

        // Assert
        Assert.Single(_recordedActivities);
        var activity = _recordedActivities[0];
        Assert.Equal(operationName, activity.OperationName);
        Assert.Equal("value1", activity.GetTagItem("key1"));
        Assert.Equal("42", activity.GetTagItem("key2"));
    }

    [Fact]
    public void StartOperation_WithNullMetadata_CreatesActivityWithoutMetadata()
    {
        // Arrange
        var operationName = "custom-operation";

        // Act
        using var context = _runtime.StartOperation(operationName, null);

        // Assert
        Assert.Single(_recordedActivities);
        var activity = _recordedActivities[0];
        Assert.Equal(operationName, activity.OperationName);
    }

    [Fact]
    public void ServiceCollectionExtensions_AddOpenTelemetryAspectRuntime_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var sourceName = "test-source";
        var meterName = "test-meter";

        // Act
        services.AddOpenTelemetryAspectRuntime(sourceName, meterName);
        var serviceProvider = services.BuildServiceProvider();
        var aspectRuntime = serviceProvider.GetRequiredService<IAspectRuntime>();

        // Assert
        Assert.NotNull(aspectRuntime);
        Assert.IsType<OtelAspectRuntime>(aspectRuntime);
    }

    [Fact]
    public void ServiceCollectionExtensions_AddOpenTelemetryAspectRuntime_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ((IServiceCollection)null!).AddOpenTelemetryAspectRuntime("source", "meter"));
    }

    [Fact]
    public void ServiceCollectionExtensions_AddOpenTelemetryAspectRuntime_WithNullSourceName_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            services.AddOpenTelemetryAspectRuntime(null!, "meter"));
    }

    [Fact]
    public void ServiceCollectionExtensions_AddOpenTelemetryAspectRuntime_WithNullMeterName_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            services.AddOpenTelemetryAspectRuntime("source", null!));
    }

    [Fact]
    public void ServiceCollectionExtensions_AddNoOpAspectRuntime_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNoOpAspectRuntime();
        var serviceProvider = services.BuildServiceProvider();
        var aspectRuntime = serviceProvider.GetRequiredService<IAspectRuntime>();

        // Assert
        Assert.NotNull(aspectRuntime);
        Assert.IsType<NoOpAspectRuntime>(aspectRuntime);
        Assert.Same(NoOpAspectRuntime.Instance, aspectRuntime);
    }

    [Fact]
    public void ServiceCollectionExtensions_AddNoOpAspectRuntime_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ((IServiceCollection)null!).AddNoOpAspectRuntime());
    }

    public void Dispose()
    {
        _runtime?.Dispose();
        _activityListener?.Dispose();
        _meterListener?.Dispose();
    }

    // Test interface for testing purposes
    private interface ITestService
    {
        void TestMethod();
    }
}