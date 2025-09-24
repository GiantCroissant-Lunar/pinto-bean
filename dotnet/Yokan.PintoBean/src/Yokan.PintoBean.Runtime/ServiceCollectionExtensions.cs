// Tier-3: Dependency injection extensions for service registry

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Linq;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Extension methods for registering the service registry with dependency injection containers.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the PintoBean service registry to the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddServiceRegistry(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        // Register as singleton to maintain provider registrations across the application lifetime
        services.TryAddSingleton<IServiceRegistry, ServiceRegistry>();

        return services;
    }

    /// <summary>
    /// Adds the PintoBean service registry to the service collection with configuration.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">A delegate to configure the service registry.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configure"/> is null.</exception>
    public static IServiceCollection AddServiceRegistry(this IServiceCollection services, Action<IServiceRegistry> configure)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        // Register the service registry with configuration
        services.TryAddSingleton<IServiceRegistry>(serviceProvider =>
        {
            var registry = new ServiceRegistry();
            configure(registry);
            return registry;
        });

        return services;
    }

    /// <summary>
    /// Adds selection strategies to the service collection with category defaults per RFC-0003.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional delegate to configure selection strategy options.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddSelectionStrategies(this IServiceCollection services, Action<SelectionStrategyOptions>? configure = null)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        // Ensure service registry is registered
        services.AddServiceRegistry();

        // Check if options are already registered
        var existingDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(SelectionStrategyOptions));
        if (existingDescriptor != null)
        {
            // Options already registered, just apply configuration if provided
            if (configure != null)
            {
                services.Remove(existingDescriptor);

                // If it was a singleton with an instance, get the instance and configure it
                if (existingDescriptor.ImplementationInstance is SelectionStrategyOptions existingOptions)
                {
                    configure(existingOptions);
                    services.AddSingleton(existingOptions);
                }
                else
                {
                    // It was registered with a factory, create new options and apply both configurations
                    var options = new SelectionStrategyOptions();
                    configure(options);
                    services.AddSingleton(options);
                }
            }
        }
        else
        {
            // Configure and register selection strategy options for the first time
            var options = new SelectionStrategyOptions();
            configure?.Invoke(options);
            services.TryAddSingleton(options);
        }

        // Register default strategy factory
        services.TryAddSingleton<ISelectionStrategyFactory, DefaultSelectionStrategyFactory>();

        return services;
    }

    /// <summary>
    /// Configures the selection strategies to use PickOne for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection UsePickOneFor<TService>(this IServiceCollection services)
        where TService : class
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        return services.AddSelectionStrategies(options =>
            options.UseStrategyFor<TService>(SelectionStrategyType.PickOne));
    }

    /// <summary>
    /// Configures the selection strategies to use FanOut for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection UseFanOutFor<TService>(this IServiceCollection services)
        where TService : class
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        return services.AddSelectionStrategies(options =>
            options.UseStrategyFor<TService>(SelectionStrategyType.FanOut));
    }

    /// <summary>
    /// Configures the selection strategies to use Sharded for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection UseShardedFor<TService>(this IServiceCollection services)
        where TService : class
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        return services.AddSelectionStrategies(options =>
            options.UseStrategyFor<TService>(SelectionStrategyType.Sharded));
    }

    /// <summary>
    /// Configures the selection strategies to use PickOne as the default for the specified service category.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="category">The service category to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection UsePickOneForCategory(this IServiceCollection services, ServiceCategory category)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        return services.AddSelectionStrategies(options =>
            options.SetCategoryDefault(category, SelectionStrategyType.PickOne));
    }

    /// <summary>
    /// Configures the selection strategies to use FanOut as the default for the specified service category.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="category">The service category to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection UseFanOutForCategory(this IServiceCollection services, ServiceCategory category)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        return services.AddSelectionStrategies(options =>
            options.SetCategoryDefault(category, SelectionStrategyType.FanOut));
    }

    /// <summary>
    /// Configures the selection strategies to use Sharded as the default for the specified service category.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="category">The service category to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection UseShardedForCategory(this IServiceCollection services, ServiceCategory category)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        return services.AddSelectionStrategies(options =>
            options.SetCategoryDefault(category, SelectionStrategyType.Sharded));
    }
}
