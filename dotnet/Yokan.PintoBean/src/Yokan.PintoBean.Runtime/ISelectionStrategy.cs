// Tier-3: Selection strategy abstractions for provider selection and routing

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Defines the types of selection strategies available for provider selection.
/// </summary>
public enum SelectionStrategyType
{
    /// <summary>
    /// Select one provider per call using capability filter, platform filter, priority, and tie-break logic.
    /// This is the default strategy.
    /// </summary>
    PickOne,

    /// <summary>
    /// Invoke all matched providers and aggregate results/failures.
    /// Used for fire-and-forget operations or result aggregation.
    /// </summary>
    FanOut,

    /// <summary>
    /// Route by key using a key extraction function.
    /// Commonly used for analytics with event name prefix routing.
    /// </summary>
    Sharded
}

/// <summary>
/// Provides context information for selection strategy execution.
/// </summary>
/// <typeparam name="TService">The service contract type.</typeparam>
public interface ISelectionContext<TService>
    where TService : class
{
    /// <summary>
    /// Gets the service contract type.
    /// </summary>
    Type ServiceType { get; }

    /// <summary>
    /// Gets all available provider registrations for the service contract.
    /// </summary>
    IReadOnlyList<IProviderRegistration> Registrations { get; }

    /// <summary>
    /// Gets optional metadata or criteria for selection decisions.
    /// </summary>
    IDictionary<string, object>? Metadata { get; }

    /// <summary>
    /// Gets the cancellation token for the operation.
    /// </summary>
    CancellationToken CancellationToken { get; }
}

/// <summary>
/// Represents the result of a selection strategy execution.
/// </summary>
/// <typeparam name="TService">The service contract type.</typeparam>
public interface ISelectionResult<TService>
    where TService : class
{
    /// <summary>
    /// Gets the selected provider(s) for invocation.
    /// </summary>
    IReadOnlyList<TService> SelectedProviders { get; }

    /// <summary>
    /// Gets the strategy type that was used for selection.
    /// </summary>
    SelectionStrategyType StrategyType { get; }

    /// <summary>
    /// Gets optional metadata about the selection process.
    /// </summary>
    IDictionary<string, object>? SelectionMetadata { get; }
}

/// <summary>
/// Defines the contract for provider selection strategies.
/// Implementations provide different algorithms for selecting providers based on the strategy type.
/// </summary>
/// <typeparam name="TService">The service contract type.</typeparam>
public interface ISelectionStrategy<TService>
    where TService : class
{
    /// <summary>
    /// Gets the strategy type implemented by this strategy.
    /// </summary>
    SelectionStrategyType StrategyType { get; }

    /// <summary>
    /// Selects provider(s) based on the strategy's algorithm and the provided context.
    /// </summary>
    /// <param name="context">The selection context containing available providers and metadata.</param>
    /// <returns>The selection result containing the chosen provider(s).</returns>
    ISelectionResult<TService> SelectProviders(ISelectionContext<TService> context);

    /// <summary>
    /// Determines if this strategy can handle the given selection context.
    /// </summary>
    /// <param name="context">The selection context to evaluate.</param>
    /// <returns>True if the strategy can handle the context, false otherwise.</returns>
    bool CanHandle(ISelectionContext<TService> context);
}

/// <summary>
/// Non-generic selection strategy interface for strategy registration and management.
/// </summary>
public interface ISelectionStrategy
{
    /// <summary>
    /// Gets the strategy type implemented by this strategy.
    /// </summary>
    SelectionStrategyType StrategyType { get; }

    /// <summary>
    /// Gets the service contract type this strategy is designed for.
    /// </summary>
    Type ServiceType { get; }
}