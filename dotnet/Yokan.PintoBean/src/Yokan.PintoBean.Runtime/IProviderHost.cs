// Tier-3: Provider host interface for plugin-registry integration

using System;
using System.Collections.Generic;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Defines a contract for plugin instances that can provide service implementations
/// to be registered in the service registry. This interface enables the plugin host
/// to discover and register providers when plugins are loaded/activated.
/// </summary>
public interface IProviderHost
{
    /// <summary>
    /// Gets all service providers that this plugin instance exposes.
    /// Each provider will be automatically registered with the service registry
    /// when the plugin is activated and unregistered when deactivated.
    /// </summary>
    /// <returns>An enumerable of provider descriptors containing service types and implementations.</returns>
    IEnumerable<ProviderDescriptor> GetProviders();
}

/// <summary>
/// Describes a service provider that a plugin exposes.
/// </summary>
public sealed class ProviderDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderDescriptor"/> class.
    /// </summary>
    /// <param name="serviceType">The service contract type.</param>
    /// <param name="provider">The provider implementation.</param>
    /// <param name="capabilities">The provider capabilities and metadata.</param>
    public ProviderDescriptor(Type serviceType, object provider, ProviderCapabilities capabilities)
    {
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
    }

    /// <summary>
    /// Gets the service contract type this provider implements.
    /// </summary>
    public Type ServiceType { get; }

    /// <summary>
    /// Gets the provider implementation instance.
    /// </summary>
    public object Provider { get; }

    /// <summary>
    /// Gets the provider capabilities and metadata.
    /// </summary>
    public ProviderCapabilities Capabilities { get; }
}