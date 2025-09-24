// Tier-3: Default selection strategy implementations

using System;
using System.Linq;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Default PickOne selection strategy that selects a single provider based on priority and registration time.
/// This implements the same logic as the original hardcoded SelectProvider method.
/// </summary>
/// <typeparam name="TService">The service contract type.</typeparam>
public sealed class PickOneSelectionStrategy<TService> : ISelectionStrategy<TService>, ISelectionStrategy
    where TService : class
{
    /// <inheritdoc />
    public SelectionStrategyType StrategyType => SelectionStrategyType.PickOne;

    /// <inheritdoc />
    public Type ServiceType => typeof(TService);

    /// <inheritdoc />
    public ISelectionResult<TService> SelectProviders(ISelectionContext<TService> context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (context.Registrations.Count == 0)
        {
            throw new InvalidOperationException(
                $"No providers registered for service contract '{typeof(TService).Name}'.");
        }

        // Select highest priority, then by registration time (stable sort)
        // This maintains the exact same logic as the original hardcoded implementation
        var selected = context.Registrations
            .OrderByDescending(r => (int)r.Capabilities.Priority)
            .ThenBy(r => r.Capabilities.RegisteredAt)
            .First();

        var selectedProvider = (TService)selected.Provider;

        return SelectionResult<TService>.Single(
            selectedProvider,
            SelectionStrategyType.PickOne,
            new System.Collections.Generic.Dictionary<string, object>
            {
                ["Priority"] = selected.Capabilities.Priority,
                ["RegisteredAt"] = selected.Capabilities.RegisteredAt
            });
    }

    /// <inheritdoc />
    public bool CanHandle(ISelectionContext<TService> context)
    {
        // PickOne can handle any context with at least one registration
        return context?.Registrations?.Count > 0;
    }
}

/// <summary>
/// Registry for default selection strategies provided by the runtime.
/// </summary>
public static class DefaultSelectionStrategies
{
    /// <summary>
    /// Creates a default PickOne strategy for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <returns>A PickOne selection strategy instance.</returns>
    public static ISelectionStrategy<TService> CreatePickOne<TService>()
        where TService : class
    {
        return new PickOneSelectionStrategy<TService>();
    }
}