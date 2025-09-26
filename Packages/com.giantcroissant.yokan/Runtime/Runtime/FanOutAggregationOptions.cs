// Tier-3: FanOut aggregation configuration and result handling

using System;
using System.Collections.Generic;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Configuration options for FanOut result aggregation behavior.
/// Defines how multiple provider results should be combined and how errors should be handled.
/// </summary>
/// <typeparam name="TResult">The type of result returned by providers.</typeparam>
public sealed class FanOutAggregationOptions<TResult>
{
    /// <summary>
    /// Gets or sets the error handling policy for the FanOut operation.
    /// </summary>
    public FanOutErrorPolicy OnError { get; set; } = FanOutErrorPolicy.Continue;

    /// <summary>
    /// Gets or sets the function used to reduce multiple results into a single result.
    /// If null, defaults to returning the first successful result.
    /// </summary>
    public Func<IEnumerable<TResult>, TResult>? ReduceFunction { get; set; }

    /// <summary>
    /// Creates default aggregation options with Continue error policy and first-success reduction.
    /// </summary>
    /// <returns>Default aggregation options.</returns>
    public static FanOutAggregationOptions<TResult> Default() => new();

    /// <summary>
    /// Creates aggregation options with the specified error policy and default first-success reduction.
    /// </summary>
    /// <param name="errorPolicy">The error handling policy to use.</param>
    /// <returns>Aggregation options with the specified error policy.</returns>
    public static FanOutAggregationOptions<TResult> WithErrorPolicy(FanOutErrorPolicy errorPolicy) =>
        new() { OnError = errorPolicy };

    /// <summary>
    /// Creates aggregation options with the specified reduce function and default Continue error policy.
    /// </summary>
    /// <param name="reduceFunction">The function to use for reducing multiple results.</param>
    /// <returns>Aggregation options with the specified reduce function.</returns>
    public static FanOutAggregationOptions<TResult> WithReduceFunction(Func<IEnumerable<TResult>, TResult> reduceFunction) =>
        new() { ReduceFunction = reduceFunction };

    /// <summary>
    /// Creates aggregation options with both error policy and reduce function specified.
    /// </summary>
    /// <param name="errorPolicy">The error handling policy to use.</param>
    /// <param name="reduceFunction">The function to use for reducing multiple results.</param>
    /// <returns>Aggregation options with the specified configuration.</returns>
    public static FanOutAggregationOptions<TResult> Create(
        FanOutErrorPolicy errorPolicy, 
        Func<IEnumerable<TResult>, TResult>? reduceFunction = null) =>
        new() { OnError = errorPolicy, ReduceFunction = reduceFunction };
}