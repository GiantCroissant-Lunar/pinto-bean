// Tier-3: OpenTelemetry implementation of IAspectRuntime

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// OpenTelemetry-backed implementation of IAspectRuntime for tracing and metrics collection.
/// Maps aspect runtime operations to OpenTelemetry ActivitySource and Meter primitives.
/// </summary>
public sealed class OtelAspectRuntime : IAspectRuntime
{
    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;
    private readonly Histogram<double> _metricsHistogram;

    /// <summary>
    /// Initializes a new instance of the OtelAspectRuntime class.
    /// </summary>
    /// <param name="sourceName">The name of the ActivitySource for tracing.</param>
    /// <param name="meterName">The name of the Meter for metrics.</param>
    /// <exception cref="ArgumentNullException">Thrown when sourceName or meterName is null.</exception>
    public OtelAspectRuntime(string sourceName, string meterName)
    {
        if (sourceName == null) throw new ArgumentNullException(nameof(sourceName));
        if (meterName == null) throw new ArgumentNullException(nameof(meterName));

        _activitySource = new ActivitySource(sourceName);
        _meter = new Meter(meterName);
        _metricsHistogram = _meter.CreateHistogram<double>("pinto_bean_metrics", description: "PintoBean custom metrics");
    }

    /// <inheritdoc />
    public IDisposable EnterMethod(Type serviceType, string methodName, object?[] parameters)
    {
        var operationName = $"{serviceType.Name}.{methodName}";
        var activity = _activitySource.StartActivity(operationName);
        
        if (activity != null)
        {
            activity.SetTag("service.type", serviceType.FullName);
            activity.SetTag("method.name", methodName);
            activity.SetTag("parameter.count", parameters?.Length ?? 0);
        }

        return new OtelMethodContext(activity);
    }

    /// <inheritdoc />
    public void ExitMethod(IDisposable context, object? result)
    {
        if (context is OtelMethodContext methodContext)
        {
            var activity = methodContext.Activity;
            if (activity != null)
            {
                activity.SetTag("method.result.type", result?.GetType().Name ?? "null");
                activity.SetStatus(ActivityStatusCode.Ok);
            }
            methodContext.Dispose();
        }
    }

    /// <inheritdoc />
    public void RecordException(IDisposable context, Exception exception)
    {
        if (context is OtelMethodContext methodContext)
        {
            var activity = methodContext.Activity;
            if (activity != null)
            {
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                activity.SetTag("exception.type", exception.GetType().Name);
                activity.SetTag("exception.message", exception.Message);
                
                // Record as an event for more detailed tracking
                activity.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow, new ActivityTagsCollection
                {
                    ["exception.type"] = exception.GetType().FullName,
                    ["exception.message"] = exception.Message,
                    ["exception.stacktrace"] = exception.StackTrace
                }));
            }
        }
    }

    /// <inheritdoc />
    public void RecordMetric(string name, double value, params (string Key, object Value)[] tags)
    {
        if (string.IsNullOrEmpty(name)) return;

        var tagCollection = new TagList();
        tagCollection.Add("metric.name", name);
        
        if (tags != null)
        {
            foreach (var (key, tagValue) in tags)
            {
                if (!string.IsNullOrEmpty(key) && tagValue != null)
                {
                    tagCollection.Add(key, tagValue.ToString());
                }
            }
        }

        _metricsHistogram.Record(value, tagCollection);
    }

    /// <inheritdoc />
    public IDisposable StartOperation(string operationName, IReadOnlyDictionary<string, object>? metadata = null)
    {
        var activity = _activitySource.StartActivity(operationName);
        
        if (activity != null && metadata != null)
        {
            foreach (var (key, value) in metadata)
            {
                if (!string.IsNullOrEmpty(key) && value != null)
                {
                    activity.SetTag(key, value.ToString());
                }
            }
        }

        return new OtelOperationContext(activity);
    }

    /// <summary>
    /// Releases all resources used by the OtelAspectRuntime.
    /// </summary>
    public void Dispose()
    {
        _activitySource?.Dispose();
        _meter?.Dispose();
    }

    /// <summary>
    /// Context for tracking OpenTelemetry method execution.
    /// </summary>
    private sealed class OtelMethodContext : IDisposable
    {
        public Activity? Activity { get; }

        public OtelMethodContext(Activity? activity)
        {
            Activity = activity;
        }

        public void Dispose()
        {
            Activity?.Dispose();
        }
    }

    /// <summary>
    /// Context for tracking OpenTelemetry custom operations.
    /// </summary>
    private sealed class OtelOperationContext : IDisposable
    {
        private readonly Activity? _activity;

        public OtelOperationContext(Activity? activity)
        {
            _activity = activity;
        }

        public void Dispose()
        {
            _activity?.Dispose();
        }
    }
}