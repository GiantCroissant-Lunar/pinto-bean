// Tier-3: Service registry interfaces for provider selection and routing

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Non-generic service registry interface for provider registration and lifecycle management.
/// </summary>
public interface IServiceRegistry
{
    /// <summary>
    /// Event raised when provider registrations change (cache invalidation used by strategies).
    /// </summary>
    event ProviderChangedEventHandler? ProviderChanged;

    /// <summary>
    /// Registers a provider for the specified service contract type.
    /// </summary>
    /// <param name="serviceType">The service contract type.</param>
    /// <param name="provider">The provider instance.</param>
    /// <param name="capabilities">The provider capabilities and metadata.</param>
    /// <returns>A registration token that can be used to unregister the provider.</returns>
    IProviderRegistration Register(Type serviceType, object provider, ProviderCapabilities capabilities);

    /// <summary>
    /// Registers a provider for the specified service contract type.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <param name="provider">The provider instance.</param>
    /// <param name="capabilities">The provider capabilities and metadata.</param>
    /// <returns>A registration token that can be used to unregister the provider.</returns>
    IProviderRegistration Register<TService>(TService provider, ProviderCapabilities capabilities)
        where TService : class;

    /// <summary>
    /// Unregisters a provider using its registration token.
    /// </summary>
    /// <param name="registration">The provider registration to remove.</param>
    /// <returns>True if the provider was successfully unregistered, false if not found.</returns>
    bool Unregister(IProviderRegistration registration);

    /// <summary>
    /// Gets all registered providers for the specified service contract type.
    /// </summary>
    /// <param name="serviceType">The service contract type.</param>
    /// <returns>A collection of provider registrations.</returns>
    IEnumerable<IProviderRegistration> GetRegistrations(Type serviceType);

    /// <summary>
    /// Gets all registered providers for the specified service contract type.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <returns>A collection of provider registrations.</returns>
    IEnumerable<IProviderRegistration> GetRegistrations<TService>();

    /// <summary>
    /// Checks if any providers are registered for the specified service contract type.
    /// </summary>
    /// <param name="serviceType">The service contract type.</param>
    /// <returns>True if providers are registered, false otherwise.</returns>
    bool HasRegistrations(Type serviceType);

    /// <summary>
    /// Checks if any providers are registered for the specified service contract type.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <returns>True if providers are registered, false otherwise.</returns>
    bool HasRegistrations<TService>();

    /// <summary>
    /// Clears all provider registrations for the specified service contract type.
    /// </summary>
    /// <param name="serviceType">The service contract type.</param>
    /// <returns>The number of registrations that were removed.</returns>
    int ClearRegistrations(Type serviceType);

    /// <summary>
    /// Clears all provider registrations for the specified service contract type.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <returns>The number of registrations that were removed.</returns>
    int ClearRegistrations<TService>();

    /// <summary>
    /// Gets a typed registry for the specified service contract type.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <returns>A typed service registry for the specified contract.</returns>
    IServiceRegistry<TService> For<TService>()
        where TService : class;
}

/// <summary>
/// Typed service registry interface for provider selection and invocation.
/// </summary>
/// <typeparam name="TService">The service contract type.</typeparam>
public interface IServiceRegistry<TService>
    where TService : class
{
    /// <summary>
    /// The underlying service registry instance.
    /// </summary>
    IServiceRegistry Registry { get; }

    /// <summary>
    /// Gets all registered providers for this service contract.
    /// </summary>
    /// <returns>A collection of provider registrations.</returns>
    IEnumerable<IProviderRegistration> GetRegistrations();

    /// <summary>
    /// Invokes an action on the selected provider(s) based on the default strategy.
    /// </summary>
    /// <param name="action">The action to invoke on the provider(s).</param>
    void Invoke(Action<TService> action);

    /// <summary>
    /// Invokes a function on the selected provider using the default strategy.
    /// </summary>
    /// <typeparam name="TResult">The return type of the function.</typeparam>
    /// <param name="func">The function to invoke on the provider.</param>
    /// <returns>The result from the selected provider.</returns>
    TResult Invoke<TResult>(Func<TService, TResult> func);

    /// <summary>
    /// Invokes an async function on the selected provider using the default strategy.
    /// </summary>
    /// <typeparam name="TResult">The return type of the async function.</typeparam>
    /// <param name="func">The async function to invoke on the provider.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the result from the selected provider.</returns>
    Task<TResult> InvokeAsync<TResult>(Func<TService, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes an async action on the selected provider using the default strategy.
    /// </summary>
    /// <param name="func">The async action to invoke on the provider.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the operation.</returns>
    Task InvokeAsync(Func<TService, CancellationToken, Task> func, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any providers are registered for this service contract.
    /// </summary>
    /// <returns>True if providers are registered, false otherwise.</returns>
    bool HasRegistrations();
}
