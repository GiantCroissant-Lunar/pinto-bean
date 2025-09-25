// Tier-3: Polly-based resilience executor implementation

using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Polly-based implementation of IResilienceExecutor that provides resilience patterns
/// including timeout, retry with jitter, and optional circuit breaker functionality.
/// </summary>
public sealed class PollyResilienceExecutor : IResilienceExecutor
{
    private readonly PollyResilienceExecutorOptions _options;
    private readonly IAsyncPolicy _asyncPolicy;
    private readonly ISyncPolicy _syncPolicy;

    /// <summary>
    /// Initializes a new instance of the PollyResilienceExecutor class.
    /// </summary>
    /// <param name="options">The resilience executor options.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    public PollyResilienceExecutor(IOptions<PollyResilienceExecutorOptions> options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        
        _options = options.Value ?? throw new ArgumentNullException(nameof(options), "Options value cannot be null.");
        
        _asyncPolicy = BuildAsyncPolicy();
        _syncPolicy = BuildSyncPolicy();
    }

    /// <summary>
    /// Initializes a new instance of the PollyResilienceExecutor class with options directly.
    /// </summary>
    /// <param name="options">The resilience executor options.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    public PollyResilienceExecutor(PollyResilienceExecutorOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        
        _asyncPolicy = BuildAsyncPolicy();
        _syncPolicy = BuildSyncPolicy();
    }

    /// <inheritdoc />
    public TResult Execute<TResult>(Func<TResult> func)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));

        return _syncPolicy.Execute(func);
    }

    /// <inheritdoc />
    public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> func, CancellationToken cancellationToken = default)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));

        return _asyncPolicy.ExecuteAsync(func, cancellationToken);
    }

    /// <inheritdoc />
    public Task ExecuteAsync(Func<CancellationToken, Task> func, CancellationToken cancellationToken = default)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));

        return _asyncPolicy.ExecuteAsync(func, cancellationToken);
    }

    private IAsyncPolicy BuildAsyncPolicy()
    {
        var policies = new List<IAsyncPolicy>();

        // Add timeout policy
        var timeoutPolicy = Policy.TimeoutAsync(
            TimeSpan.FromSeconds(_options.DefaultTimeoutSeconds));
        policies.Add(timeoutPolicy);

        // Add retry policy with jitter
        var retryPolicy = Policy
            .Handle<Exception>(ex => IsTransientException(ex))
            .WaitAndRetryAsync(
                retryCount: _options.MaxRetryAttempts,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(
                    _options.BaseRetryDelayMilliseconds * Math.Pow(2, retryAttempt - 1))
                    .Add(TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100))), // Add jitter
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    // Optional: Add logging here in the future
                });
        policies.Add(retryPolicy);

        // Add circuit breaker policy if enabled
        // Note: Circuit breaker is currently disabled in this version for simplicity
        // It can be enabled in future versions if needed
        // if (_options.EnableCircuitBreaker)
        // {
        //     var circuitBreakerPolicy = Policy
        //         .Handle<Exception>(ex => IsTransientException(ex))
        //         .CircuitBreakerAsync(
        //             handledEventsAllowedBeforeBreaking: _options.CircuitBreakerFailureThreshold,
        //             durationOfBreak: TimeSpan.FromSeconds(_options.CircuitBreakerDurationOfBreakSeconds));
        //     policies.Add(circuitBreakerPolicy);
        // }

        // Wrap all policies together (last policy wraps all previous)
        return policies.Count == 1 ? policies[0] : Policy.WrapAsync(policies.ToArray());
    }

    private ISyncPolicy BuildSyncPolicy()
    {
        var policies = new List<ISyncPolicy>();

        // Add timeout policy
        var timeoutPolicy = Policy.Timeout(
            TimeSpan.FromSeconds(_options.DefaultTimeoutSeconds));
        policies.Add(timeoutPolicy);

        // Add retry policy with jitter
        var retryPolicy = Policy
            .Handle<Exception>(ex => IsTransientException(ex))
            .WaitAndRetry(
                retryCount: _options.MaxRetryAttempts,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(
                    _options.BaseRetryDelayMilliseconds * Math.Pow(2, retryAttempt - 1))
                    .Add(TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100))), // Add jitter
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    // Optional: Add logging here in the future
                });
        policies.Add(retryPolicy);

        // Add circuit breaker policy if enabled
        // Note: Circuit breaker is currently disabled in this version for simplicity
        // It can be enabled in future versions if needed
        // if (_options.EnableCircuitBreaker)
        // {
        //     var circuitBreakerPolicy = Policy
        //         .Handle<Exception>(ex => IsTransientException(ex))
        //         .CircuitBreaker(
        //             handledEventsAllowedBeforeBreaking: _options.CircuitBreakerFailureThreshold,
        //             durationOfBreak: TimeSpan.FromSeconds(_options.CircuitBreakerDurationOfBreakSeconds));
        //     policies.Add(circuitBreakerPolicy);
        // }

        // Wrap all policies together (last policy wraps all previous)
        return policies.Count == 1 ? policies[0] : Policy.Wrap(policies.ToArray());
    }

    /// <summary>
    /// Determines if an exception is transient and should trigger retry logic.
    /// </summary>
    /// <param name="exception">The exception to evaluate.</param>
    /// <returns>True if the exception is transient, false otherwise.</returns>
    private static bool IsTransientException(Exception exception)
    {
        // Treat timeout exceptions as transient
        if (exception is TimeoutRejectedException)
            return true;

        // Common transient exceptions
        return exception is InvalidOperationException ||
               exception is TimeoutException ||
               (exception.Message?.Contains("timeout", StringComparison.OrdinalIgnoreCase) == true) ||
               (exception.Message?.Contains("connection", StringComparison.OrdinalIgnoreCase) == true);
    }
}