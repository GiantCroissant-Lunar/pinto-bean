// Tier-3: Router policy configuration for AI provider selection

using System;
using System.Collections.Generic;

namespace Yokan.PintoBean.Runtime.AI;

/// <summary>
/// Policy configuration for the IntelligentRouter provider that controls
/// backend selection based on QoS (Quality of Service) criteria.
/// </summary>
public sealed record RouterOptions
{
    /// <summary>
    /// Maximum budget per 1K tokens in USD cents.
    /// Used to filter providers that exceed cost constraints.
    /// Null means no budget constraint.
    /// </summary>
    public decimal? BudgetPer1KTokens { get; init; }

    /// <summary>
    /// Target latency threshold in milliseconds.
    /// Router prefers providers with latency at or below this value.
    /// Null means no latency preference.
    /// </summary>
    public int? TargetLatencyMs { get; init; }

    /// <summary>
    /// Preferred geographic region for the provider.
    /// Used to select providers in specific regions for data locality.
    /// Null means no region preference.
    /// </summary>
    public string? Region { get; init; }

    /// <summary>
    /// Whether to allow external/third-party providers.
    /// When false, only internal/local providers are considered.
    /// Default is true.
    /// </summary>
    public bool AllowExternal { get; init; } = true;

    /// <summary>
    /// Optional metadata for additional routing criteria.
    /// Can be used for custom filtering logic.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Creates a default RouterOptions instance with no constraints.
    /// </summary>
    /// <returns>A new RouterOptions instance with default values.</returns>
    public static RouterOptions Default => new();

    /// <summary>
    /// Creates a RouterOptions instance optimized for cost efficiency.
    /// </summary>
    /// <param name="budgetPer1KTokens">Maximum budget per 1K tokens.</param>
    /// <returns>A new RouterOptions instance optimized for cost.</returns>
    public static RouterOptions ForCostOptimization(decimal budgetPer1KTokens)
        => new() { BudgetPer1KTokens = budgetPer1KTokens };

    /// <summary>
    /// Creates a RouterOptions instance optimized for low latency.
    /// </summary>
    /// <param name="targetLatencyMs">Target latency threshold in milliseconds.</param>
    /// <returns>A new RouterOptions instance optimized for latency.</returns>
    public static RouterOptions ForLowLatency(int targetLatencyMs)
        => new() { TargetLatencyMs = targetLatencyMs };

    /// <summary>
    /// Creates a RouterOptions instance restricted to internal providers only.
    /// </summary>
    /// <returns>A new RouterOptions instance that disallows external providers.</returns>
    public static RouterOptions InternalOnly()
        => new() { AllowExternal = false };

    /// <summary>
    /// Creates a copy of this instance with the specified budget constraint.
    /// </summary>
    /// <param name="budgetPer1KTokens">Maximum budget per 1K tokens.</param>
    /// <returns>A new RouterOptions instance with the updated budget.</returns>
    public RouterOptions WithBudget(decimal budgetPer1KTokens)
        => this with { BudgetPer1KTokens = budgetPer1KTokens };

    /// <summary>
    /// Creates a copy of this instance with the specified latency target.
    /// </summary>
    /// <param name="targetLatencyMs">Target latency threshold in milliseconds.</param>
    /// <returns>A new RouterOptions instance with the updated latency target.</returns>
    public RouterOptions WithLatencyTarget(int targetLatencyMs)
        => this with { TargetLatencyMs = targetLatencyMs };

    /// <summary>
    /// Creates a copy of this instance with the specified region preference.
    /// </summary>
    /// <param name="region">Preferred geographic region.</param>
    /// <returns>A new RouterOptions instance with the updated region preference.</returns>
    public RouterOptions WithRegion(string region)
        => this with { Region = region };

    /// <summary>
    /// Creates a copy of this instance with the specified external provider policy.
    /// </summary>
    /// <param name="allowExternal">Whether to allow external providers.</param>
    /// <returns>A new RouterOptions instance with the updated external policy.</returns>
    public RouterOptions WithExternalPolicy(bool allowExternal)
        => this with { AllowExternal = allowExternal };

    /// <summary>
    /// Creates a copy of this instance with additional metadata.
    /// </summary>
    /// <param name="metadata">Additional routing metadata.</param>
    /// <returns>A new RouterOptions instance with the updated metadata.</returns>
    public RouterOptions WithMetadata(IReadOnlyDictionary<string, object> metadata)
        => this with { Metadata = metadata };
}