using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Yokan.PintoBean.Runtime.Unity;

/// <summary>
/// Core bridge class that integrates Microsoft.Extensions.DependencyInjection with Unity environments.
/// This provides a Unity-agnostic way to access MS.DI services from Unity MonoBehaviours.
/// </summary>
public sealed class UnityServiceProviderBridge
{
    private readonly IServiceProvider _serviceProvider;
    private static UnityServiceProviderBridge? _instance;

    /// <summary>
    /// Gets the current global instance of the Unity service provider bridge.
    /// </summary>
    public static UnityServiceProviderBridge Current => _instance ?? throw new InvalidOperationException("Unity bridge has not been initialized. Call Initialize() first.");

    /// <summary>
    /// Gets a value indicating whether the bridge has been initialized.
    /// </summary>
    public static bool IsInitialized => _instance != null;

    /// <summary>
    /// Gets the Unity scheduler if available in the service provider.
    /// </summary>
    public IUnityScheduler? Scheduler => _serviceProvider.GetService<IUnityScheduler>();

    /// <summary>
    /// Initializes a new instance of the <see cref="UnityServiceProviderBridge"/> class.
    /// </summary>
    /// <param name="serviceProvider">The Microsoft.Extensions.DependencyInjection service provider.</param>
    private UnityServiceProviderBridge(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Initializes the global Unity bridge instance with the provided service provider.
    /// </summary>
    /// <param name="serviceProvider">The Microsoft.Extensions.DependencyInjection service provider to use.</param>
    /// <returns>The initialized bridge instance.</returns>
    public static UnityServiceProviderBridge Initialize(IServiceProvider serviceProvider)
    {
        if (_instance != null)
        {
            throw new InvalidOperationException("Unity bridge has already been initialized.");
        }

        _instance = new UnityServiceProviderBridge(serviceProvider);
        return _instance;
    }

    /// <summary>
    /// Resets the global bridge instance. This is primarily for testing scenarios.
    /// </summary>
    public static void Reset()
    {
        _instance = null;
    }

    /// <summary>
    /// Resolves a service of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of service to resolve.</typeparam>
    /// <returns>The resolved service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service cannot be resolved.</exception>
    public T GetService<T>() where T : notnull
    {
        var service = _serviceProvider.GetService<T>();
        if (service == null)
        {
            throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
        }
        return service;
    }

    /// <summary>
    /// Resolves a service of the specified type, returning null if not found.
    /// </summary>
    /// <typeparam name="T">The type of service to resolve.</typeparam>
    /// <returns>The resolved service instance, or null if not found.</returns>
    public T? GetServiceOrNull<T>() where T : class
    {
        return _serviceProvider.GetService<T>();
    }

    /// <summary>
    /// Resolves a required service of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of service to resolve.</typeparam>
    /// <returns>The resolved service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service cannot be resolved.</exception>
    public T GetRequiredService<T>() where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Resolves a service of the specified type.
    /// </summary>
    /// <param name="serviceType">The type of service to resolve.</param>
    /// <returns>The resolved service instance, or null if not found.</returns>
    public object? GetService(Type serviceType)
    {
        return _serviceProvider.GetService(serviceType);
    }

    /// <summary>
    /// Resolves a required service of the specified type.
    /// </summary>
    /// <param name="serviceType">The type of service to resolve.</param>
    /// <returns>The resolved service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service cannot be resolved.</exception>
    public object GetRequiredService(Type serviceType)
    {
        return _serviceProvider.GetRequiredService(serviceType);
    }

    /// <summary>
    /// Posts an action to run on the Unity main thread if a scheduler is available.
    /// If no scheduler is available, executes the action immediately.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    public void PostToMainThread(Action action)
    {
        var scheduler = Scheduler;
        if (scheduler != null)
        {
            scheduler.Post(action);
        }
        else
        {
            action();
        }
    }

    /// <summary>
    /// Posts an async function to run on the Unity main thread if a scheduler is available.
    /// If no scheduler is available, executes the function immediately.
    /// </summary>
    /// <param name="func">The async function to execute.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task PostToMainThreadAsync(Func<Task> func)
    {
        var scheduler = Scheduler;
        if (scheduler != null)
        {
            return scheduler.PostAsync(func);
        }
        else
        {
            return func();
        }
    }
}