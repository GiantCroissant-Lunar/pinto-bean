// Tier-3: Configuration options for Polly-based resilience executor

using System;
using System.Collections.Generic;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Configuration options for the Polly-based resilience executor.
/// Provides settings for timeout, retry, and circuit breaker policies.
/// </summary>
public sealed class PollyResilienceExecutorOptions
{
    /// <summary>
    /// Gets or sets the default timeout for operations in seconds.
    /// </summary>
    public double DefaultTimeoutSeconds { get; set; } = 30.0;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for transient exceptions.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay for retry attempts in milliseconds.
    /// Jitter will be applied to this base value.
    /// </summary>
    public double BaseRetryDelayMilliseconds { get; set; } = 1000.0;

    /// <summary>
    /// Gets or sets whether circuit breaker functionality is enabled.
    /// </summary>
    public bool EnableCircuitBreaker { get; set; } = false;

    /// <summary>
    /// Gets or sets the number of consecutive failures required to open the circuit breaker.
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the duration to keep the circuit breaker open in seconds.
    /// </summary>
    public double CircuitBreakerDurationOfBreakSeconds { get; set; } = 30.0;

    /// <summary>
    /// Gets or sets the sampling duration for circuit breaker metrics in seconds.
    /// </summary>
    public double CircuitBreakerSamplingDurationSeconds { get; set; } = 10.0;

    /// <summary>
    /// Gets or sets category-specific timeout overrides.
    /// Key is the category name, value is the timeout in seconds.
    /// </summary>
    public Dictionary<string, double> CategoryTimeouts { get; set; } = new();

    /// <summary>
    /// Gets or sets operation-specific timeout overrides.
    /// Key is the operation name, value is the timeout in seconds.
    /// </summary>
    public Dictionary<string, double> OperationTimeouts { get; set; } = new();

    /// <summary>
    /// Gets the timeout for a specific operation or category.
    /// Checks operation timeouts first, then category timeouts, then falls back to default.
    /// </summary>
    /// <param name="operationName">The operation name to check.</param>
    /// <param name="categoryName">The category name to check if operation is not found.</param>
    /// <returns>The timeout in seconds.</returns>
    public double GetTimeoutSeconds(string? operationName, string? categoryName)
    {
        if (!string.IsNullOrEmpty(operationName) && OperationTimeouts.TryGetValue(operationName, out var operationTimeout))
        {
            return operationTimeout;
        }

        if (!string.IsNullOrEmpty(categoryName) && CategoryTimeouts.TryGetValue(categoryName, out var categoryTimeout))
        {
            return categoryTimeout;
        }

        return DefaultTimeoutSeconds;
    }

    /// <summary>
    /// Sets a category-specific timeout.
    /// </summary>
    /// <param name="category">The service category.</param>
    /// <param name="timeoutSeconds">The timeout in seconds.</param>
    /// <returns>This options instance for method chaining.</returns>
    public PollyResilienceExecutorOptions SetCategoryTimeout(ServiceCategory category, double timeoutSeconds)
    {
        CategoryTimeouts[category.ToString()] = timeoutSeconds;
        return this;
    }

    /// <summary>
    /// Sets an operation-specific timeout.
    /// </summary>
    /// <param name="operationName">The operation name.</param>
    /// <param name="timeoutSeconds">The timeout in seconds.</param>
    /// <returns>This options instance for method chaining.</returns>
    public PollyResilienceExecutorOptions SetOperationTimeout(string operationName, double timeoutSeconds)
    {
        if (string.IsNullOrEmpty(operationName)) throw new ArgumentException("Operation name cannot be null or empty.", nameof(operationName));
        
        OperationTimeouts[operationName] = timeoutSeconds;
        return this;
    }
}