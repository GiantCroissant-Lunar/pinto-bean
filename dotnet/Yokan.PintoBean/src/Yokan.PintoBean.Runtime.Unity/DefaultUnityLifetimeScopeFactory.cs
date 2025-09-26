using System;
using Microsoft.Extensions.DependencyInjection;

namespace Yokan.PintoBean.Runtime.Unity;

/// <summary>
/// Default implementation of IUnityLifetimeScopeFactory that creates Unity-compatible service scope objects.
/// </summary>
public class DefaultUnityLifetimeScopeFactory : IUnityLifetimeScopeFactory
{
    /// <summary>
    /// Creates a Unity lifetime scope object that integrates with the provided service provider.
    /// </summary>
    /// <param name="serviceProvider">The Microsoft.Extensions.DependencyInjection service provider to integrate with.</param>
    /// <returns>A Unity-compatible lifetime scope object.</returns>
    public object CreateLifetimeScope(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

        // Initialize the Unity bridge with the service provider
        UnityServiceProviderBridge.Initialize(serviceProvider);

        // Return a simple scope object that represents the Unity integration
        return new UnityServiceScope(serviceProvider);
    }
}

/// <summary>
/// Represents a Unity service scope that maintains a reference to the service provider.
/// </summary>
public sealed class UnityServiceScope : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnityServiceScope"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for this scope.</param>
    public UnityServiceScope(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Gets the service provider for this scope.
    /// </summary>
    public IServiceProvider ServiceProvider => _serviceProvider;

    /// <summary>
    /// Disposes the service scope and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        if (_serviceProvider is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }

        _disposed = true;
    }
}