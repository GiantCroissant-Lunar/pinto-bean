using System;

namespace Yokan.PintoBean.Runtime.Unity;

/// <summary>
/// Base class for Unity MonoBehaviours that need access to Microsoft.Extensions.DependencyInjection services.
/// This class provides convenient methods to resolve services from the Unity service provider bridge.
/// 
/// Note: This class is designed to compile without Unity references for testing,
/// but should inherit from MonoBehaviour when used in Unity.
/// </summary>
public abstract class ServiceAwareMonoBehaviour // In Unity: : MonoBehaviour
{
    /// <summary>
    /// Gets a service of the specified type from the Unity service provider bridge.
    /// </summary>
    /// <typeparam name="T">The type of service to resolve.</typeparam>
    /// <returns>The resolved service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service cannot be resolved or the bridge is not initialized.</exception>
    protected T GetService<T>() where T : notnull
    {
        if (!UnityServiceProviderBridge.IsInitialized)
        {
            throw new InvalidOperationException("Unity service provider bridge is not initialized. Ensure the bridge is set up before using this MonoBehaviour.");
        }

        return UnityServiceProviderBridge.Current.GetService<T>();
    }

    /// <summary>
    /// Gets a service of the specified type from the Unity service provider bridge, returning null if not found.
    /// </summary>
    /// <typeparam name="T">The type of service to resolve.</typeparam>
    /// <returns>The resolved service instance, or null if not found.</returns>
    protected T? GetServiceOrNull<T>() where T : class
    {
        if (!UnityServiceProviderBridge.IsInitialized)
        {
            return null;
        }

        return UnityServiceProviderBridge.Current.GetServiceOrNull<T>();
    }

    /// <summary>
    /// Gets a required service of the specified type from the Unity service provider bridge.
    /// </summary>
    /// <typeparam name="T">The type of service to resolve.</typeparam>
    /// <returns>The resolved service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service cannot be resolved or the bridge is not initialized.</exception>
    protected T GetRequiredService<T>() where T : notnull
    {
        if (!UnityServiceProviderBridge.IsInitialized)
        {
            throw new InvalidOperationException("Unity service provider bridge is not initialized. Ensure the bridge is set up before using this MonoBehaviour.");
        }

        return UnityServiceProviderBridge.Current.GetRequiredService<T>();
    }

    /// <summary>
    /// Gets a service of the specified type from the Unity service provider bridge.
    /// </summary>
    /// <param name="serviceType">The type of service to resolve.</param>
    /// <returns>The resolved service instance, or null if not found.</returns>
    protected object? GetService(Type serviceType)
    {
        if (!UnityServiceProviderBridge.IsInitialized)
        {
            return null;
        }

        return UnityServiceProviderBridge.Current.GetService(serviceType);
    }

    /// <summary>
    /// Gets a required service of the specified type from the Unity service provider bridge.
    /// </summary>
    /// <param name="serviceType">The type of service to resolve.</param>
    /// <returns>The resolved service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service cannot be resolved or the bridge is not initialized.</exception>
    protected object GetRequiredService(Type serviceType)
    {
        if (!UnityServiceProviderBridge.IsInitialized)
        {
            throw new InvalidOperationException("Unity service provider bridge is not initialized. Ensure the bridge is set up before using this MonoBehaviour.");
        }

        return UnityServiceProviderBridge.Current.GetRequiredService(serviceType);
    }
}