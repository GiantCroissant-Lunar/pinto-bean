using System;
using Microsoft.Extensions.DependencyInjection;

namespace Yokan.PintoBean.Runtime.Unity;

/// <summary>
/// Factory interface for creating Unity lifetime scopes that can be integrated with Microsoft.Extensions.DependencyInjection.
/// </summary>
public interface IUnityLifetimeScopeFactory
{
    /// <summary>
    /// Creates a Unity lifetime scope that integrates with the provided IServiceProvider.
    /// </summary>
    /// <param name="serviceProvider">The Microsoft.Extensions.DependencyInjection service provider to integrate with.</param>
    /// <returns>An object representing the configured Unity lifetime scope.</returns>
    object CreateLifetimeScope(IServiceProvider serviceProvider);
}