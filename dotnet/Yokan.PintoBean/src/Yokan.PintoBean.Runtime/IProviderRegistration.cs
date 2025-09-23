// Tier-3: Provider registration and change event types

using System;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Represents a provider registration in the service registry.
/// </summary>
public interface IProviderRegistration
{
    /// <summary>
    /// The service contract type this provider implements.
    /// </summary>
    Type ServiceType { get; }

    /// <summary>
    /// The provider instance implementing the service contract.
    /// </summary>
    object Provider { get; }

    /// <summary>
    /// The capabilities and metadata of this provider.
    /// </summary>
    ProviderCapabilities Capabilities { get; }

    /// <summary>
    /// Indicates whether this registration is currently active.
    /// </summary>
    bool IsActive { get; }
}

/// <summary>
/// Event arguments for provider registration changes.
/// </summary>
public sealed class ProviderChangedEventArgs : EventArgs
{
    /// <summary>
    /// The type of change that occurred.
    /// </summary>
    public ProviderChangeType ChangeType { get; }

    /// <summary>
    /// The service contract type affected by the change.
    /// </summary>
    public Type ServiceType { get; }

    /// <summary>
    /// The provider registration that was affected.
    /// </summary>
    public IProviderRegistration Registration { get; }

    /// <summary>
    /// Initializes a new instance of the ProviderChangedEventArgs class.
    /// </summary>
    /// <param name="changeType">The type of change that occurred.</param>
    /// <param name="serviceType">The service contract type affected.</param>
    /// <param name="registration">The provider registration affected.</param>
    public ProviderChangedEventArgs(ProviderChangeType changeType, Type serviceType, IProviderRegistration registration)
    {
        ChangeType = changeType;
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        Registration = registration ?? throw new ArgumentNullException(nameof(registration));
    }
}

/// <summary>
/// Defines the types of provider registration changes.
/// </summary>
public enum ProviderChangeType
{
    /// <summary>
    /// A new provider was registered.
    /// </summary>
    Added,

    /// <summary>
    /// An existing provider was removed.
    /// </summary>
    Removed,

    /// <summary>
    /// An existing provider's capabilities were updated.
    /// </summary>
    Updated
}

/// <summary>
/// Event handler delegate for provider registration changes.
/// </summary>
/// <param name="sender">The service registry that raised the event.</param>
/// <param name="e">The event arguments containing change details.</param>
public delegate void ProviderChangedEventHandler(object? sender, ProviderChangedEventArgs e);