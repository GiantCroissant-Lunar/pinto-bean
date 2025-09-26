// Tier-3: FanOut result aggregation utilities for handling multiple provider results

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Utility class for aggregating results from multiple providers in FanOut scenarios.
/// Handles error policies and result reduction according to configured options.
/// </summary>
public static class FanOutAggregator
{
    /// <summary>
    /// Aggregates results from multiple synchronous function calls according to the specified options.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by providers.</typeparam>
    /// <param name="providers">The collection of providers to invoke.</param>
    /// <param name="invokeFunc">Function to invoke each provider and get a result.</param>
    /// <param name="options">Aggregation options specifying error handling and reduction behavior.</param>
    /// <returns>The aggregated result according to the specified options.</returns>
    /// <exception cref="ArgumentNullException">Thrown when providers or invokeFunc is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no providers succeed and FailFast policy is used.</exception>
    public static TResult Aggregate<TResult>(
        IEnumerable<object> providers,
        Func<object, TResult> invokeFunc,
        FanOutAggregationOptions<TResult>? options = null)
    {
        if (providers == null) throw new ArgumentNullException(nameof(providers));
        if (invokeFunc == null) throw new ArgumentNullException(nameof(invokeFunc));

        options ??= FanOutAggregationOptions<TResult>.Default();
        
        var results = new List<TResult>();
        var exceptions = new List<Exception>();

        foreach (var provider in providers)
        {
            try
            {
                var result = invokeFunc(provider);
                results.Add(result);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                
                // If FailFast policy, throw immediately on first failure
                if (options.OnError == FanOutErrorPolicy.FailFast)
                {
                    throw new AggregateException(
                        $"FanOut operation failed with FailFast policy on provider: {provider.GetType().Name}",
                        ex);
                }
            }
        }

        // If we have no successful results, handle according to policy
        if (results.Count == 0)
        {
            if (exceptions.Count > 0)
            {
                throw new AggregateException(
                    "All providers failed in FanOut operation",
                    exceptions);
            }
            throw new InvalidOperationException("No providers were available for FanOut operation");
        }

        // Apply reduce function or default to first successful result
        return options.ReduceFunction != null 
            ? options.ReduceFunction(results)
            : results.First();
    }

    /// <summary>
    /// Aggregates results from multiple asynchronous function calls according to the specified options.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by providers.</typeparam>
    /// <param name="providers">The collection of providers to invoke.</param>
    /// <param name="invokeFunc">Async function to invoke each provider and get a result.</param>
    /// <param name="options">Aggregation options specifying error handling and reduction behavior.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task containing the aggregated result according to the specified options.</returns>
    /// <exception cref="ArgumentNullException">Thrown when providers or invokeFunc is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no providers succeed and FailFast policy is used.</exception>
    public static async Task<TResult> AggregateAsync<TResult>(
        IEnumerable<object> providers,
        Func<object, CancellationToken, Task<TResult>> invokeFunc,
        FanOutAggregationOptions<TResult>? options = null,
        CancellationToken cancellationToken = default)
    {
        if (providers == null) throw new ArgumentNullException(nameof(providers));
        if (invokeFunc == null) throw new ArgumentNullException(nameof(invokeFunc));

        options ??= FanOutAggregationOptions<TResult>.Default();
        
        var results = new List<TResult>();
        var exceptions = new List<Exception>();

        foreach (var provider in providers)
        {
            try
            {
                var result = await invokeFunc(provider, cancellationToken).ConfigureAwait(false);
                results.Add(result);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                
                // If FailFast policy, throw immediately on first failure
                if (options.OnError == FanOutErrorPolicy.FailFast)
                {
                    throw new AggregateException(
                        $"FanOut operation failed with FailFast policy on provider: {provider.GetType().Name}",
                        ex);
                }
            }
        }

        // If we have no successful results, handle according to policy
        if (results.Count == 0)
        {
            if (exceptions.Count > 0)
            {
                throw new AggregateException(
                    "All providers failed in FanOut operation",
                    exceptions);
            }
            throw new InvalidOperationException("No providers were available for FanOut operation");
        }

        // Apply reduce function or default to first successful result
        return options.ReduceFunction != null 
            ? options.ReduceFunction(results)
            : results.First();
    }

    /// <summary>
    /// Executes multiple void operations in a FanOut manner with error handling according to specified policy.
    /// </summary>
    /// <param name="providers">The collection of providers to invoke.</param>
    /// <param name="invokeAction">Action to invoke on each provider.</param>
    /// <param name="errorPolicy">Error policy for handling failures. Defaults to Continue.</param>
    /// <exception cref="ArgumentNullException">Thrown when providers or invokeAction is null.</exception>
    /// <exception cref="AggregateException">Thrown when errors occur and error policy requires it.</exception>
    public static void ExecuteAll(
        IEnumerable<object> providers,
        Action<object> invokeAction,
        FanOutErrorPolicy errorPolicy = FanOutErrorPolicy.Continue)
    {
        if (providers == null) throw new ArgumentNullException(nameof(providers));
        if (invokeAction == null) throw new ArgumentNullException(nameof(invokeAction));

        var exceptions = new List<Exception>();

        foreach (var provider in providers)
        {
            try
            {
                invokeAction(provider);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                
                // If FailFast policy, throw immediately on first failure
                if (errorPolicy == FanOutErrorPolicy.FailFast)
                {
                    throw new AggregateException(
                        $"FanOut operation failed with FailFast policy on provider: {provider.GetType().Name}",
                        ex);
                }
            }
        }

        // For Continue policy, throw aggregate exception only if all operations failed
        if (exceptions.Count > 0 && errorPolicy == FanOutErrorPolicy.Continue)
        {
            // For fire-and-forget scenarios with Continue policy, we might want to log exceptions
            // but not throw them. However, for consistency with the async version, we'll throw
            // an aggregate exception containing all failures.
            throw new AggregateException(
                "Some providers failed in FanOut operation (Continue policy)",
                exceptions);
        }
    }

    /// <summary>
    /// Executes multiple async void operations in a FanOut manner with error handling according to specified policy.
    /// </summary>
    /// <param name="providers">The collection of providers to invoke.</param>
    /// <param name="invokeFunc">Async action to invoke on each provider.</param>
    /// <param name="errorPolicy">Error policy for handling failures. Defaults to Continue.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the completion of all operations.</returns>
    /// <exception cref="ArgumentNullException">Thrown when providers or invokeFunc is null.</exception>
    /// <exception cref="AggregateException">Thrown when errors occur and error policy requires it.</exception>
    public static async Task ExecuteAllAsync(
        IEnumerable<object> providers,
        Func<object, CancellationToken, Task> invokeFunc,
        FanOutErrorPolicy errorPolicy = FanOutErrorPolicy.Continue,
        CancellationToken cancellationToken = default)
    {
        if (providers == null) throw new ArgumentNullException(nameof(providers));
        if (invokeFunc == null) throw new ArgumentNullException(nameof(invokeFunc));

        var exceptions = new List<Exception>();

        foreach (var provider in providers)
        {
            try
            {
                await invokeFunc(provider, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                
                // If FailFast policy, throw immediately on first failure
                if (errorPolicy == FanOutErrorPolicy.FailFast)
                {
                    throw new AggregateException(
                        $"FanOut operation failed with FailFast policy on provider: {provider.GetType().Name}",
                        ex);
                }
            }
        }

        // For Continue policy, throw aggregate exception if any operations failed
        if (exceptions.Count > 0 && errorPolicy == FanOutErrorPolicy.Continue)
        {
            throw new AggregateException(
                "Some providers failed in FanOut operation (Continue policy)",
                exceptions);
        }
    }
}