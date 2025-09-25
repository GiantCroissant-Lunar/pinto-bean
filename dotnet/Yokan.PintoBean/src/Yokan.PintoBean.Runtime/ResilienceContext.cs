// Tier-3: Resilience context for cross-cutting resilience operations

using System;
using System.Collections.Generic;
using System.Threading;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Provides context information for resilience operations.
/// Contains metadata and configuration needed for resilience execution.
/// </summary>
public sealed class ResilienceContext
{
    /// <summary>
    /// Gets the optional operation name for telemetry and logging.
    /// </summary>
    public string? OperationName { get; }

    /// <summary>
    /// Gets optional metadata for resilience policies and telemetry.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; }

    /// <summary>
    /// Gets the cancellation token for the operation.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Initializes a new instance of the ResilienceContext class.
    /// </summary>
    /// <param name="operationName">Optional operation name for telemetry and logging.</param>
    /// <param name="metadata">Optional metadata for resilience policies and telemetry.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    public ResilienceContext(
        string? operationName = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        OperationName = operationName;
        Metadata = metadata;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Creates a new ResilienceContext with a different cancellation token.
    /// </summary>
    /// <param name="cancellationToken">The new cancellation token.</param>
    /// <returns>A new ResilienceContext with the specified cancellation token.</returns>
    public ResilienceContext WithCancellationToken(CancellationToken cancellationToken)
    {
        return new ResilienceContext(OperationName, Metadata, cancellationToken);
    }

    /// <summary>
    /// Creates a new ResilienceContext with additional metadata.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>A new ResilienceContext with the additional metadata.</returns>
    public ResilienceContext WithMetadata(string key, object value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        var newMetadata = new Dictionary<string, object>();
        if (Metadata != null)
        {
            foreach (var kvp in Metadata)
            {
                newMetadata[kvp.Key] = kvp.Value;
            }
        }
        newMetadata[key] = value;

        return new ResilienceContext(OperationName, newMetadata, CancellationToken);
    }
}