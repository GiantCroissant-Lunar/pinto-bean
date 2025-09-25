// Tier-3: Aspect runtime interface for cross-cutting concerns

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Cross-cutting adapter for aspect-oriented concerns including telemetry,
/// logging, metrics collection, and other observability features.
/// Provides hooks for method entry/exit, exception handling, and custom operation tracking.
/// </summary>
public interface IAspectRuntime
{
    /// <summary>
    /// Records the entry into a service method execution.
    /// </summary>
    /// <param name="serviceType">The service contract type.</param>
    /// <param name="methodName">The name of the method being invoked.</param>
    /// <param name="parameters">The parameters passed to the method.</param>
    /// <returns>A correlation context for tracking this method execution.</returns>
    IDisposable EnterMethod(Type serviceType, string methodName, object?[] parameters);

    /// <summary>
    /// Records a successful completion of a service method execution.
    /// </summary>
    /// <param name="context">The correlation context from EnterMethod.</param>
    /// <param name="result">The result returned by the method.</param>
    void ExitMethod(IDisposable context, object? result);

    /// <summary>
    /// Records an exception during service method execution.
    /// </summary>
    /// <param name="context">The correlation context from EnterMethod.</param>
    /// <param name="exception">The exception that occurred.</param>
    void RecordException(IDisposable context, Exception exception);

    /// <summary>
    /// Records custom metrics or telemetry data.
    /// </summary>
    /// <param name="name">The metric or event name.</param>
    /// <param name="value">The metric value.</param>
    /// <param name="tags">Optional tags for categorization.</param>
    void RecordMetric(string name, double value, params (string Key, object Value)[] tags);

    /// <summary>
    /// Starts tracking a custom operation with optional metadata.
    /// </summary>
    /// <param name="operationName">The name of the operation being tracked.</param>
    /// <param name="metadata">Optional metadata for the operation.</param>
    /// <returns>A disposable context for tracking the operation's lifecycle.</returns>
    IDisposable StartOperation(string operationName, IReadOnlyDictionary<string, object>? metadata = null);
}

/// <summary>
/// No-operation implementation of IAspectRuntime that performs no actual telemetry collection.
/// Used for scenarios where telemetry is disabled or not needed.
/// </summary>
public sealed class NoOpAspectRuntime : IAspectRuntime
{
    /// <summary>
    /// Singleton instance of the no-op aspect runtime.
    /// </summary>
    public static readonly NoOpAspectRuntime Instance = new();

    private NoOpAspectRuntime() { }

    /// <inheritdoc />
    public IDisposable EnterMethod(Type serviceType, string methodName, object?[] parameters)
    {
        return NoOpContext.Instance;
    }

    /// <inheritdoc />
    public void ExitMethod(IDisposable context, object? result)
    {
        // No-op
    }

    /// <inheritdoc />
    public void RecordException(IDisposable context, Exception exception)
    {
        // No-op
    }

    /// <inheritdoc />
    public void RecordMetric(string name, double value, params (string Key, object Value)[] tags)
    {
        // No-op
    }

    /// <inheritdoc />
    public IDisposable StartOperation(string operationName, IReadOnlyDictionary<string, object>? metadata = null)
    {
        return NoOpContext.Instance;
    }

    private sealed class NoOpContext : IDisposable
    {
        public static readonly NoOpContext Instance = new();
        private NoOpContext() { }
        public void Dispose() { }
    }
}
