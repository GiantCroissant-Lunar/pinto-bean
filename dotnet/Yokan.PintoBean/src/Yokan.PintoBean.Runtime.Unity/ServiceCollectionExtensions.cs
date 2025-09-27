using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Yokan.PintoBean.Runtime.Unity;

/// <summary>
/// Extension methods for integrating Microsoft.Extensions.DependencyInjection with Unity and VContainer.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Unity bridge support to the service collection, enabling integration between
    /// Microsoft.Extensions.DependencyInjection and Unity/VContainer.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="lifetimeScopeFactory">Factory for creating Unity lifetime scopes.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or lifetimeScopeFactory is null.</exception>
    public static IServiceCollection AddUnityBridge(this IServiceCollection services, IUnityLifetimeScopeFactory lifetimeScopeFactory)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (lifetimeScopeFactory == null) throw new ArgumentNullException(nameof(lifetimeScopeFactory));

        // Register the lifetime scope factory
        services.TryAddSingleton(lifetimeScopeFactory);

        // Add Unity bridge marker service
        services.TryAddSingleton<IUnityBridgeMarker, UnityBridgeMarker>();

        return services;
    }

    /// <summary>
    /// Adds Unity bridge support to the service collection with a factory delegate.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="lifetimeScopeFactory">Factory delegate for creating Unity lifetime scopes.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or lifetimeScopeFactory is null.</exception>
    public static IServiceCollection AddUnityBridge(this IServiceCollection services, Func<IServiceProvider, IUnityLifetimeScopeFactory> lifetimeScopeFactory)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (lifetimeScopeFactory == null) throw new ArgumentNullException(nameof(lifetimeScopeFactory));

        // Register the lifetime scope factory using the provided delegate
        services.TryAddSingleton(lifetimeScopeFactory);

        // Add Unity bridge marker service
        services.TryAddSingleton<IUnityBridgeMarker, UnityBridgeMarker>();

        return services;
    }

    /// <summary>
    /// Adds Unity scheduler support to the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="mainThreadId">The ID of the Unity main thread. If not provided, uses the current thread.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    public static IServiceCollection AddUnityScheduler(this IServiceCollection services, int? mainThreadId = null)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        // Register the Unity scheduler
        services.TryAddSingleton<IUnityScheduler>(serviceProvider => new DefaultUnityScheduler(mainThreadId));

        return services;
    }

    /// <summary>
    /// Adds Unity scheduler support to the service collection with a custom implementation.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="scheduler">The scheduler instance to register.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or scheduler is null.</exception>
    public static IServiceCollection AddUnityScheduler(this IServiceCollection services, IUnityScheduler scheduler)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));

        // Register the provided scheduler instance
        services.TryAddSingleton(scheduler);

        return services;
    }

    /// <summary>
    /// Adds Unity scheduler support to the service collection with a factory delegate.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="schedulerFactory">Factory delegate for creating the scheduler.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or schedulerFactory is null.</exception>
    public static IServiceCollection AddUnityScheduler(this IServiceCollection services, Func<IServiceProvider, IUnityScheduler> schedulerFactory)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (schedulerFactory == null) throw new ArgumentNullException(nameof(schedulerFactory));

        // Register the scheduler using the provided factory
        services.TryAddSingleton(schedulerFactory);

        return services;
    }
}

/// <summary>
/// Marker interface to indicate that Unity bridge services have been registered.
/// </summary>
public interface IUnityBridgeMarker
{
}

/// <summary>
/// Implementation of the Unity bridge marker.
/// </summary>
internal sealed class UnityBridgeMarker : IUnityBridgeMarker
{
}