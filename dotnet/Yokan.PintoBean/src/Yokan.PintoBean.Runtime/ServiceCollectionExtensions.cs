// Tier-3: Dependency injection extensions for service registry

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

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
}