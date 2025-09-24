// Tier-3: Configuration options for selection strategy defaults

using System;
using System.Collections.Generic;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Represents a service category for selection strategy defaults.
/// </summary>
public enum ServiceCategory
{
    /// <summary>
    /// Analytics services (telemetry, metrics, tracking).
    /// Default: FanOut (send to multiple analytics backends).
    /// </summary>
    Analytics,

    /// <summary>
    /// Resource services (data access, storage, APIs).
    /// Default: PickOne (primary with fallback resilience).
    /// </summary>
    Resources,

    /// <summary>
    /// Scene flow services (game state, narrative progression).
    /// Default: PickOne (deterministic flow control).
    /// </summary>
    SceneFlow,

    /// <summary>
    /// AI services (machine learning, decision making).
    /// Default: PickOne (router decides backend).
    /// </summary>
    AI
}

/// <summary>
/// Configuration options for selection strategy defaults by service category.
/// </summary>
public sealed class SelectionStrategyOptions
{
    private readonly Dictionary<ServiceCategory, SelectionStrategyType> _categoryDefaults;
    private readonly Dictionary<Type, SelectionStrategyType> _serviceTypeOverrides;
    private readonly Dictionary<Type, Func<IServiceProvider, ISelectionStrategy>> _customStrategyFactories;

    /// <summary>
    /// Initializes a new instance of the <see cref="SelectionStrategyOptions"/> class
    /// with RFC-0003 category defaults.
    /// </summary>
    public SelectionStrategyOptions()
    {
        _categoryDefaults = new Dictionary<ServiceCategory, SelectionStrategyType>
        {
            { ServiceCategory.Analytics, SelectionStrategyType.FanOut },
            { ServiceCategory.Resources, SelectionStrategyType.PickOne },
            { ServiceCategory.SceneFlow, SelectionStrategyType.PickOne },
            { ServiceCategory.AI, SelectionStrategyType.PickOne }
        };

        _serviceTypeOverrides = new Dictionary<Type, SelectionStrategyType>();
        _customStrategyFactories = new Dictionary<Type, Func<IServiceProvider, ISelectionStrategy>>();
    }

    /// <summary>
    /// Gets the default selection strategy for the specified service category.
    /// </summary>
    /// <param name="category">The service category.</param>
    /// <returns>The default selection strategy type for the category.</returns>
    public SelectionStrategyType GetDefaultForCategory(ServiceCategory category)
    {
        return _categoryDefaults.TryGetValue(category, out var strategy)
            ? strategy
            : SelectionStrategyType.PickOne;
    }

    /// <summary>
    /// Sets the default selection strategy for the specified service category.
    /// </summary>
    /// <param name="category">The service category.</param>
    /// <param name="strategyType">The selection strategy type to use as default.</param>
    /// <returns>This options instance for method chaining.</returns>
    public SelectionStrategyOptions SetCategoryDefault(ServiceCategory category, SelectionStrategyType strategyType)
    {
        _categoryDefaults[category] = strategyType;
        return this;
    }

    /// <summary>
    /// Sets the selection strategy override for a specific service type.
    /// </summary>
    /// <param name="serviceType">The service contract type.</param>
    /// <param name="strategyType">The selection strategy type to use.</param>
    /// <returns>This options instance for method chaining.</returns>
    public SelectionStrategyOptions UseStrategyFor(Type serviceType, SelectionStrategyType strategyType)
    {
        if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

        _serviceTypeOverrides[serviceType] = strategyType;
        return this;
    }

    /// <summary>
    /// Sets the selection strategy override for a specific service type.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <param name="strategyType">The selection strategy type to use.</param>
    /// <returns>This options instance for method chaining.</returns>
    public SelectionStrategyOptions UseStrategyFor<TService>(SelectionStrategyType strategyType)
        where TService : class
    {
        return UseStrategyFor(typeof(TService), strategyType);
    }

    /// <summary>
    /// Sets a custom strategy factory for a specific service type.
    /// </summary>
    /// <param name="serviceType">The service contract type.</param>
    /// <param name="strategyFactory">Factory function to create the custom strategy.</param>
    /// <returns>This options instance for method chaining.</returns>
    public SelectionStrategyOptions UseCustomStrategyFor(Type serviceType, Func<IServiceProvider, ISelectionStrategy> strategyFactory)
    {
        if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
        if (strategyFactory == null) throw new ArgumentNullException(nameof(strategyFactory));

        _customStrategyFactories[serviceType] = strategyFactory;
        return this;
    }

    /// <summary>
    /// Sets a custom strategy factory for a specific service type.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <param name="strategyFactory">Factory function to create the custom strategy.</param>
    /// <returns>This options instance for method chaining.</returns>
    public SelectionStrategyOptions UseCustomStrategyFor<TService>(Func<IServiceProvider, ISelectionStrategy<TService>> strategyFactory)
        where TService : class
    {
        if (strategyFactory == null) throw new ArgumentNullException(nameof(strategyFactory));

        return UseCustomStrategyFor(typeof(TService), serviceProvider =>
        {
            var typedStrategy = strategyFactory(serviceProvider);
            return (ISelectionStrategy)typedStrategy;
        });
    }

    /// <summary>
    /// Gets the strategy type override for the specified service type, if any.
    /// </summary>
    /// <param name="serviceType">The service contract type.</param>
    /// <returns>The strategy type override, or null if no override is set.</returns>
    public SelectionStrategyType? GetStrategyOverride(Type serviceType)
    {
        return _serviceTypeOverrides.TryGetValue(serviceType, out var strategy) ? strategy : null;
    }

    /// <summary>
    /// Gets the custom strategy factory for the specified service type, if any.
    /// </summary>
    /// <param name="serviceType">The service contract type.</param>
    /// <returns>The custom strategy factory, or null if no custom factory is set.</returns>
    public Func<IServiceProvider, ISelectionStrategy>? GetCustomStrategyFactory(Type serviceType)
    {
        return _customStrategyFactories.TryGetValue(serviceType, out var factory) ? factory : null;
    }

    /// <summary>
    /// Gets all configured service type overrides.
    /// </summary>
    /// <returns>A dictionary of service type to strategy type overrides.</returns>
    public IReadOnlyDictionary<Type, SelectionStrategyType> GetAllStrategyOverrides()
    {
        return _serviceTypeOverrides;
    }

    /// <summary>
    /// Gets all configured custom strategy factories.
    /// </summary>
    /// <returns>A dictionary of service type to custom strategy factories.</returns>
    public IReadOnlyDictionary<Type, Func<IServiceProvider, ISelectionStrategy>> GetAllCustomStrategyFactories()
    {
        return _customStrategyFactories;
    }
}
