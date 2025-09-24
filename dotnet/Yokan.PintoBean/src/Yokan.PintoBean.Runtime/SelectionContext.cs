// Tier-3: Selection strategy context implementations

using System;
using System.Collections.Generic;
using System.Threading;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Default implementation of ISelectionContext.
/// </summary>
/// <typeparam name="TService">The service contract type.</typeparam>
public sealed class SelectionContext<TService> : ISelectionContext<TService>
    where TService : class
{
    /// <inheritdoc />
    public Type ServiceType { get; }

    /// <inheritdoc />
    public IReadOnlyList<IProviderRegistration> Registrations { get; }

    /// <inheritdoc />
    public IDictionary<string, object>? Metadata { get; }

    /// <inheritdoc />
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Initializes a new instance of the SelectionContext class.
    /// </summary>
    /// <param name="registrations">The available provider registrations.</param>
    /// <param name="metadata">Optional metadata for selection decisions.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    public SelectionContext(
        IReadOnlyList<IProviderRegistration> registrations,
        IDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ServiceType = typeof(TService);
        Registrations = registrations ?? throw new ArgumentNullException(nameof(registrations));
        Metadata = metadata;
        CancellationToken = cancellationToken;
    }
}

/// <summary>
/// Default implementation of ISelectionResult.
/// </summary>
/// <typeparam name="TService">The service contract type.</typeparam>
public sealed class SelectionResult<TService> : ISelectionResult<TService>
    where TService : class
{
    /// <inheritdoc />
    public IReadOnlyList<TService> SelectedProviders { get; }

    /// <inheritdoc />
    public SelectionStrategyType StrategyType { get; }

    /// <inheritdoc />
    public IDictionary<string, object>? SelectionMetadata { get; }

    /// <summary>
    /// Initializes a new instance of the SelectionResult class.
    /// </summary>
    /// <param name="selectedProviders">The selected provider(s).</param>
    /// <param name="strategyType">The strategy type used for selection.</param>
    /// <param name="selectionMetadata">Optional metadata about the selection process.</param>
    public SelectionResult(
        IReadOnlyList<TService> selectedProviders,
        SelectionStrategyType strategyType,
        IDictionary<string, object>? selectionMetadata = null)
    {
        SelectedProviders = selectedProviders ?? throw new ArgumentNullException(nameof(selectedProviders));
        StrategyType = strategyType;
        SelectionMetadata = selectionMetadata;
    }

    /// <summary>
    /// Creates a SelectionResult with a single selected provider.
    /// </summary>
    /// <param name="selectedProvider">The single selected provider.</param>
    /// <param name="strategyType">The strategy type used for selection.</param>
    /// <param name="selectionMetadata">Optional metadata about the selection process.</param>
    /// <returns>A SelectionResult containing the single provider.</returns>
    public static SelectionResult<TService> Single(
        TService selectedProvider,
        SelectionStrategyType strategyType,
        IDictionary<string, object>? selectionMetadata = null)
    {
        if (selectedProvider == null)
            throw new ArgumentNullException(nameof(selectedProvider));

        return new SelectionResult<TService>(
            new[] { selectedProvider },
            strategyType,
            selectionMetadata);
    }
}