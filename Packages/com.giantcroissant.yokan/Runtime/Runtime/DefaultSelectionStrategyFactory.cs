// Tier-3: Default implementation of selection strategy factory

using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Default implementation of <see cref="ISelectionStrategyFactory"/> that creates strategies
/// based on configured options and category defaults.
/// </summary>
public sealed class DefaultSelectionStrategyFactory : ISelectionStrategyFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SelectionStrategyOptions _options;
    private readonly ConcurrentDictionary<Type, ISelectionStrategy> _strategyCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultSelectionStrategyFactory"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency resolution.</param>
    /// <param name="options">The selection strategy configuration options.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public DefaultSelectionStrategyFactory(IServiceProvider serviceProvider, SelectionStrategyOptions options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _strategyCache = new ConcurrentDictionary<Type, ISelectionStrategy>();
    }

    /// <inheritdoc />
    public ISelectionStrategy<TService> CreateStrategy<TService>() where TService : class
    {
        var serviceType = typeof(TService);

        // Check for custom strategy factory first
        var customFactory = _options.GetCustomStrategyFactory(serviceType);
        if (customFactory != null)
        {
            var customStrategy = customFactory(_serviceProvider);
            if (customStrategy is ISelectionStrategy<TService> typedCustomStrategy)
            {
                return typedCustomStrategy;
            }
            throw new InvalidOperationException($"Custom strategy factory for {serviceType.Name} must return ISelectionStrategy<{serviceType.Name}>");
        }

        // Check for service type override
        var strategyOverride = _options.GetStrategyOverride(serviceType);
        var strategyType = strategyOverride ?? GetDefaultStrategyForService(serviceType);

        // Get registry and resilience executor from DI if available
        var registry = _serviceProvider.GetService<IServiceRegistry>();
        var resilienceExecutor = _serviceProvider.GetService<IResilienceExecutor>();

        return strategyType switch
        {
            SelectionStrategyType.PickOne => DefaultSelectionStrategies.CreatePickOne<TService>(registry),
            SelectionStrategyType.FanOut => DefaultSelectionStrategies.CreateFanOut<TService>(registry, resilienceExecutor),
            SelectionStrategyType.Sharded => DefaultSelectionStrategies.CreateAnalyticsSharded<TService>(registry),
            _ => throw new NotSupportedException($"Strategy type {strategyType} is not supported")
        };
    }

    /// <inheritdoc />
    public ISelectionStrategy CreateStrategy(Type serviceType)
    {
        if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

        // Check for custom strategy factory first - don't cache custom strategies
        var customFactory = _options.GetCustomStrategyFactory(serviceType);
        if (customFactory != null)
        {
            return customFactory(_serviceProvider);
        }

        // For standard strategies, use caching to avoid repeated creation
        return _strategyCache.GetOrAdd(serviceType, type => new TypeErasedSelectionStrategy(type, this));
    }

    /// <summary>
    /// Determines the default strategy type for a service based on category inference.
    /// </summary>
    /// <param name="serviceType">The service contract type.</param>
    /// <returns>The default strategy type for the service.</returns>
    private SelectionStrategyType GetDefaultStrategyForService(Type serviceType)
    {
        // Try to infer category from service type name or attributes
        var category = InferServiceCategory(serviceType);
        return _options.GetDefaultForCategory(category);
    }

    /// <summary>
    /// Infers the service category from the service type.
    /// </summary>
    /// <param name="serviceType">The service contract type.</param>
    /// <returns>The inferred service category.</returns>
    private static ServiceCategory InferServiceCategory(Type serviceType)
    {
        var typeName = serviceType.Name.ToLowerInvariant();
        var namespaceName = serviceType.Namespace?.ToLowerInvariant() ?? string.Empty;

        // Check for analytics keywords
        if (typeName.Contains("analytics") || typeName.Contains("telemetry") ||
            typeName.Contains("tracking") || typeName.Contains("metrics") ||
            namespaceName.Contains("analytics") || namespaceName.Contains("telemetry"))
        {
            return ServiceCategory.Analytics;
        }

        // Check for AI keywords
        if (typeName.Contains("ai") || typeName.Contains("intelligence") ||
            typeName.Contains("ml") || typeName.Contains("machinelearning") ||
            namespaceName.Contains("ai") || namespaceName.Contains("intelligence"))
        {
            return ServiceCategory.AI;
        }

        // Check for scene flow keywords
        if (typeName.Contains("scene") || typeName.Contains("flow") ||
            typeName.Contains("narrative") || typeName.Contains("story") ||
            namespaceName.Contains("scene") || namespaceName.Contains("flow"))
        {
            return ServiceCategory.SceneFlow;
        }

        // Check for resource keywords
        if (typeName.Contains("resource") || typeName.Contains("data") ||
            typeName.Contains("repository") || typeName.Contains("storage") ||
            namespaceName.Contains("resource") || namespaceName.Contains("data"))
        {
            return ServiceCategory.Resources;
        }

        // Default to Resources category (PickOne strategy)
        return ServiceCategory.Resources;
    }

    /// <summary>
    /// A type-erased wrapper for selection strategies that implements the non-generic interface.
    /// </summary>
    private sealed class TypeErasedSelectionStrategy : ISelectionStrategy
    {
        public SelectionStrategyType StrategyType { get; }
        public Type ServiceType { get; }

        public TypeErasedSelectionStrategy(Type serviceType, DefaultSelectionStrategyFactory factory)
        {
            ServiceType = serviceType;

            // Determine strategy type based on configuration
            var strategyOverride = factory._options.GetStrategyOverride(serviceType);
            StrategyType = strategyOverride ?? factory.GetDefaultStrategyForService(serviceType);
        }
    }
}
