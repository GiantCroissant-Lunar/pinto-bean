// Tier-3: Service registry implementation for provider selection and routing

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Internal implementation of IProviderRegistration.
/// </summary>
internal sealed record ProviderRegistration : IProviderRegistration
{
    public Type ServiceType { get; init; } = null!;
    public object Provider { get; init; } = null!;
    public ProviderCapabilities Capabilities { get; init; } = null!;
    public bool IsActive { get; init; } = true;
    public Guid Id { get; init; } = Guid.NewGuid();
}

/// <summary>
/// Default implementation of the service registry for provider management and selection.
/// </summary>
public sealed class ServiceRegistry : IServiceRegistry
{
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<Guid, ProviderRegistration>> _providers = new();
    private readonly object _eventLock = new();

    /// <inheritdoc />
    public event ProviderChangedEventHandler? ProviderChanged;

    /// <inheritdoc />
    public IProviderRegistration Register(Type serviceType, object provider, ProviderCapabilities capabilities)
    {
        if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
        if (provider == null) throw new ArgumentNullException(nameof(provider));
        if (capabilities == null) throw new ArgumentNullException(nameof(capabilities));

        // Validate provider implements service type
        if (!serviceType.IsAssignableFrom(provider.GetType()))
        {
            throw new ArgumentException(
                $"Provider of type '{provider.GetType().Name}' does not implement service contract '{serviceType.Name}'.",
                nameof(provider));
        }

        var registration = new ProviderRegistration
        {
            ServiceType = serviceType,
            Provider = provider,
            Capabilities = capabilities
        };

        var serviceProviders = _providers.GetOrAdd(serviceType, _ => new ConcurrentDictionary<Guid, ProviderRegistration>());
        serviceProviders[registration.Id] = registration;

        RaiseProviderChanged(ProviderChangeType.Added, serviceType, registration);

        return registration;
    }

    /// <inheritdoc />
    public IProviderRegistration Register<TService>(TService provider, ProviderCapabilities capabilities)
        where TService : class
    {
        return Register(typeof(TService), provider, capabilities);
    }

    /// <inheritdoc />
    public bool Unregister(IProviderRegistration registration)
    {
        if (registration == null) throw new ArgumentNullException(nameof(registration));

        if (registration is not ProviderRegistration internalReg)
            return false;

        if (!_providers.TryGetValue(registration.ServiceType, out var serviceProviders))
            return false;

        if (!serviceProviders.TryRemove(internalReg.Id, out var removed))
            return false;

        // Clean up empty service type entries
        if (serviceProviders.IsEmpty)
        {
            _providers.TryRemove(registration.ServiceType, out _);
        }

        RaiseProviderChanged(ProviderChangeType.Removed, registration.ServiceType, removed);

        return true;
    }

    /// <inheritdoc />
    public IEnumerable<IProviderRegistration> GetRegistrations(Type serviceType)
    {
        if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

        if (!_providers.TryGetValue(serviceType, out var serviceProviders))
            return Enumerable.Empty<IProviderRegistration>();

        return serviceProviders.Values.Where(r => r.IsActive).Cast<IProviderRegistration>().ToList();
    }

    /// <inheritdoc />
    public IEnumerable<IProviderRegistration> GetRegistrations<TService>()
    {
        return GetRegistrations(typeof(TService));
    }

    /// <inheritdoc />
    public bool HasRegistrations(Type serviceType)
    {
        if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

        return _providers.TryGetValue(serviceType, out var serviceProviders) &&
               serviceProviders.Values.Any(r => r.IsActive);
    }

    /// <inheritdoc />
    public bool HasRegistrations<TService>()
    {
        return HasRegistrations(typeof(TService));
    }

    /// <inheritdoc />
    public int ClearRegistrations(Type serviceType)
    {
        if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

        if (!_providers.TryRemove(serviceType, out var serviceProviders))
            return 0;

        var count = serviceProviders.Count;

        // Raise events for each removed provider
        foreach (var registration in serviceProviders.Values)
        {
            RaiseProviderChanged(ProviderChangeType.Removed, serviceType, registration);
        }

        return count;
    }

    /// <inheritdoc />
    public int ClearRegistrations<TService>()
    {
        return ClearRegistrations(typeof(TService));
    }

    /// <inheritdoc />
    public IServiceRegistry<TService> For<TService>()
        where TService : class
    {
        return new TypedServiceRegistry<TService>(this);
    }

    private void RaiseProviderChanged(ProviderChangeType changeType, Type serviceType, ProviderRegistration registration)
    {
        try
        {
            lock (_eventLock)
            {
                ProviderChanged?.Invoke(this, new ProviderChangedEventArgs(changeType, serviceType, registration));
            }
        }
        catch
        {
            // Prevent event handler exceptions from affecting registry operations
        }
    }
}

/// <summary>
/// Typed service registry implementation for provider selection and invocation.
/// </summary>
/// <typeparam name="TService">The service contract type.</typeparam>
internal sealed class TypedServiceRegistry<TService> : IServiceRegistry<TService>
    where TService : class
{
    /// <inheritdoc />
    public IServiceRegistry Registry { get; }

    private readonly ISelectionStrategy<TService> _selectionStrategy;

    internal TypedServiceRegistry(IServiceRegistry registry)
    {
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _selectionStrategy = DefaultSelectionStrategies.CreatePickOne<TService>();
    }

    /// <inheritdoc />
    public IEnumerable<IProviderRegistration> GetRegistrations()
    {
        return Registry.GetRegistrations<TService>();
    }

    /// <inheritdoc />
    public void Invoke(Action<TService> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        var provider = SelectProvider();
        action(provider);
    }

    /// <inheritdoc />
    public TResult Invoke<TResult>(Func<TService, TResult> func)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));

        var provider = SelectProvider();
        return func(provider);
    }

    /// <inheritdoc />
    public async Task<TResult> InvokeAsync<TResult>(Func<TService, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken = default)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));

        var provider = SelectProvider();
        return await func(provider, cancellationToken);
    }

    /// <inheritdoc />
    public async Task InvokeAsync(Func<TService, CancellationToken, Task> func, CancellationToken cancellationToken = default)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));

        var provider = SelectProvider();
        await func(provider, cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<TResult> InvokeStreamAsync<TResult>(Func<TService, CancellationToken, IAsyncEnumerable<TResult>> func, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));

        var provider = SelectProvider();
        await foreach (var result in func(provider, cancellationToken))
        {
            yield return result;
        }
    }

    /// <inheritdoc />
    public bool HasRegistrations()
    {
        return Registry.HasRegistrations<TService>();
    }

    private TService SelectProvider()
    {
        var registrations = GetRegistrations().ToList();
        var context = new SelectionContext<TService>(registrations);
        var result = _selectionStrategy.SelectProviders(context);

        // For backward compatibility, return the first selected provider
        // This maintains the existing behavior for PickOne strategy
        return result.SelectedProviders.First();
    }
}
