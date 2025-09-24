// Tier-3: Selection strategy factory for creating strategies based on configuration

using System;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Factory interface for creating selection strategies based on configuration and service types.
/// </summary>
public interface ISelectionStrategyFactory
{
    /// <summary>
    /// Creates a selection strategy for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <returns>A selection strategy instance configured for the service type.</returns>
    ISelectionStrategy<TService> CreateStrategy<TService>() where TService : class;

    /// <summary>
    /// Creates a selection strategy for the specified service type.
    /// </summary>
    /// <param name="serviceType">The service contract type.</param>
    /// <returns>A selection strategy instance configured for the service type.</returns>
    ISelectionStrategy CreateStrategy(Type serviceType);
}
