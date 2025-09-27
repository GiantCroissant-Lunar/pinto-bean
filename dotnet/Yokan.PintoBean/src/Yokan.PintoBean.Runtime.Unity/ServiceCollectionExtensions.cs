using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Yokan.PintoBean.Runtime;

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

    /// <summary>
    /// Adds the Unity-specific aspect runtime to the service collection that logs to UnityEngine.Debug.*.
    /// Designed for Unity play mode with minimal performance overhead and Unity-friendly log output.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="enableMetrics">Whether to log metric recordings. Default is true.</param>
    /// <param name="verboseLogging">Whether to enable verbose logging for all operations. Default is false.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    public static IServiceCollection AddUnityAspectRuntime(this IServiceCollection services, bool enableMetrics = true, bool verboseLogging = false)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        services.TryAddSingleton<IAspectRuntime>(serviceProvider => 
            new UnityAspectRuntime(enableMetrics, verboseLogging));

        return services;
    }

    /// <summary>
    /// Adds an adaptive aspect runtime that detects Unity context and switches between Unity Debug logging 
    /// and OpenTelemetry based on runtime environment and package availability.
    /// In Unity play mode, uses UnityAspectRuntime. In Editor with OpenTelemetry packages, can use OtelAspectRuntime.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="sourceName">The name of the ActivitySource for OpenTelemetry tracing (used when OTel is available).</param>
    /// <param name="meterName">The name of the Meter for OpenTelemetry metrics (used when OTel is available).</param>
    /// <param name="enableMetrics">Whether to log metric recordings in Unity mode. Default is true.</param>
    /// <param name="verboseLogging">Whether to enable verbose logging in Unity mode. Default is false.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services, sourceName, or meterName is null.</exception>
    public static IServiceCollection AddAdaptiveAspectRuntime(this IServiceCollection services, string sourceName, string meterName, bool enableMetrics = true, bool verboseLogging = false)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (sourceName == null) throw new ArgumentNullException(nameof(sourceName));
        if (meterName == null) throw new ArgumentNullException(nameof(meterName));

        services.TryAddSingleton<IAspectRuntime>(serviceProvider => 
        {
            // Try to detect if we're in Unity play mode vs Editor mode
            bool isUnityPlayMode = DetectUnityPlayMode();
            bool isOpenTelemetryAvailable = DetectOpenTelemetryAvailability();

            if (isUnityPlayMode || !isOpenTelemetryAvailable)
            {
                // Use Unity logging in play mode or when OpenTelemetry is not available
                return new UnityAspectRuntime(enableMetrics, verboseLogging);
            }
            else
            {
                // Use OpenTelemetry in Editor mode when available
                return new OtelAspectRuntime(sourceName, meterName);
            }
        });

        return services;
    }

    private static bool DetectUnityPlayMode()
    {
#if UNITY_2018_1_OR_NEWER
        return UnityEngine.Application.isPlaying;
#else
        return false;
#endif
    }

    private static bool DetectOpenTelemetryAvailability()
    {
        try
        {
            // Try to load the OpenTelemetry types to see if they're available
            var activitySourceType = Type.GetType("System.Diagnostics.ActivitySource, System.Diagnostics.DiagnosticSource");
            var meterType = Type.GetType("System.Diagnostics.Metrics.Meter, System.Diagnostics.DiagnosticSource");
            return activitySourceType != null && meterType != null;
        }
        catch
        {
            return false;
        }
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