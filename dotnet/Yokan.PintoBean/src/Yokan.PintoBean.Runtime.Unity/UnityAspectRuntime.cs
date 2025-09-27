// Tier-4: Unity-specific implementation of IAspectRuntime

using System;
using System.Collections.Generic;
using System.Diagnostics;

#if UNITY_2018_1_OR_NEWER
using UnityEngine;
#endif

namespace Yokan.PintoBean.Runtime.Unity;

/// <summary>
/// Unity-specific implementation of IAspectRuntime that logs to UnityEngine.Debug.* with low-cardinality tags.
/// Designed for Unity play mode with minimal performance overhead and Unity-friendly log output.
/// </summary>
public sealed class UnityAspectRuntime : IAspectRuntime
{
    private readonly bool _enableMetrics;
    private readonly bool _verboseLogging;

    /// <summary>
    /// Initializes a new instance of the UnityAspectRuntime class.
    /// </summary>
    /// <param name="enableMetrics">Whether to log metric recordings. Default is true.</param>
    /// <param name="verboseLogging">Whether to enable verbose logging for all operations. Default is false.</param>
    public UnityAspectRuntime(bool enableMetrics = true, bool verboseLogging = false)
    {
        _enableMetrics = enableMetrics;
        _verboseLogging = verboseLogging;
    }

    /// <inheritdoc />
    public IDisposable EnterMethod(Type serviceType, string methodName, object?[] parameters)
    {
        var context = new UnityMethodContext(serviceType, methodName, parameters?.Length ?? 0);
        
        if (_verboseLogging)
        {
            Log($"[PintoBean] → {serviceType.Name}.{methodName} (params: {parameters?.Length ?? 0})");
        }

        return context;
    }

    /// <inheritdoc />
    public void ExitMethod(IDisposable context, object? result)
    {
        if (context is UnityMethodContext methodContext)
        {
            var duration = methodContext.GetDuration();
            var resultType = result?.GetType().Name ?? "void";
            
            Log($"[PintoBean] ✓ {methodContext.ServiceType.Name}.{methodContext.MethodName} completed in {duration:F2}ms → {resultType}");
            
            methodContext.Dispose();
        }
    }

    /// <inheritdoc />
    public void RecordException(IDisposable context, Exception exception)
    {
        if (context is UnityMethodContext methodContext)
        {
            var duration = methodContext.GetDuration();
            LogError($"[PintoBean] ✗ {methodContext.ServiceType.Name}.{methodContext.MethodName} failed in {duration:F2}ms: {exception.GetType().Name} - {exception.Message}");
        }
        else
        {
            LogError($"[PintoBean] ✗ Exception: {exception.GetType().Name} - {exception.Message}");
        }
    }

    /// <inheritdoc />
    public void RecordMetric(string name, double value, params (string Key, object Value)[] tags)
    {
        if (!_enableMetrics || string.IsNullOrEmpty(name)) return;

        var tagString = "";
        if (tags != null && tags.Length > 0)
        {
            var tagPairs = new string[Math.Min(tags.Length, 3)]; // Limit to 3 tags for low cardinality
            for (int i = 0; i < tagPairs.Length; i++)
            {
                if (!string.IsNullOrEmpty(tags[i].Key) && tags[i].Value != null)
                {
                    tagPairs[i] = $"{tags[i].Key}={tags[i].Value}";
                }
            }
            tagString = $" [{string.Join(", ", tagPairs)}]";
        }

        Log($"[PintoBean] 📊 {name}: {value:F2}{tagString}");
    }

    /// <inheritdoc />
    public IDisposable StartOperation(string operationName, IReadOnlyDictionary<string, object>? metadata = null)
    {
        var context = new UnityOperationContext(operationName);
        
        if (_verboseLogging)
        {
            var metadataString = "";
            if (metadata != null && metadata.Count > 0)
            {
                var metadataPairs = new List<string>();
                foreach (var (key, value) in metadata)
                {
                    if (!string.IsNullOrEmpty(key) && value != null && metadataPairs.Count < 3) // Limit metadata for readability
                    {
                        metadataPairs.Add($"{key}={value}");
                    }
                }
                metadataString = metadataPairs.Count > 0 ? $" [{string.Join(", ", metadataPairs)}]" : "";
            }
            Log($"[PintoBean] ⏰ Started: {operationName}{metadataString}");
        }

        return context;
    }

    private void Log(string message)
    {
#if UNITY_2018_1_OR_NEWER
        Debug.Log(message);
#else
        Console.WriteLine(message);
#endif
    }

    private void LogError(string message)
    {
#if UNITY_2018_1_OR_NEWER
        Debug.LogError(message);
#else
        Console.WriteLine($"ERROR: {message}");
#endif
    }

    /// <summary>
    /// Context for tracking Unity method execution with timing.
    /// </summary>
    private sealed class UnityMethodContext : IDisposable
    {
        public Type ServiceType { get; }
        public string MethodName { get; }
        public int ParameterCount { get; }
        
        private readonly Stopwatch _stopwatch;

        public UnityMethodContext(Type serviceType, string methodName, int parameterCount)
        {
            ServiceType = serviceType;
            MethodName = methodName;
            ParameterCount = parameterCount;
            _stopwatch = Stopwatch.StartNew();
        }

        public double GetDuration()
        {
            return _stopwatch.Elapsed.TotalMilliseconds;
        }

        public void Dispose()
        {
            _stopwatch?.Stop();
        }
    }

    /// <summary>
    /// Context for tracking Unity custom operations with timing.
    /// </summary>
    private sealed class UnityOperationContext : IDisposable
    {
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;

        public UnityOperationContext(string operationName)
        {
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            var duration = _stopwatch.Elapsed.TotalMilliseconds;
#if UNITY_2018_1_OR_NEWER
            Debug.Log($"[PintoBean] ⏰ Completed: {_operationName} in {duration:F2}ms");
#else
            Console.WriteLine($"[PintoBean] ⏰ Completed: {_operationName} in {duration:F2}ms");
#endif
            _stopwatch?.Stop();
        }
    }
}