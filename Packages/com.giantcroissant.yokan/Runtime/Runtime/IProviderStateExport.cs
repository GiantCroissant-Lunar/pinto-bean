// Tier-3: Provider lifecycle contract for state export during transitions

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Defines a contract for providers that can export their internal state during soft-swap operations.
/// This enables state handover from an outgoing provider to an incoming replacement provider,
/// allowing for seamless transitions without data loss.
/// </summary>
public interface IProviderStateExport
{
    /// <summary>
    /// Exports the current internal state of the provider for handover to a replacement provider.
    /// </summary>
    /// <param name="cancellationToken">
    /// Cancellation token to abort the export operation if needed.
    /// </param>
    /// <returns>
    /// A task that resolves to a <see cref="ValueTask{ProviderState}"/> containing the exported state data.
    /// The state should be serializable and contain all necessary information for a replacement
    /// provider to continue operations seamlessly.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is typically called after <see cref="IQuiesceable.QuiesceAsync"/> (if implemented)
    /// to ensure the provider is in a stable state before exporting. Providers should:
    /// </para>
    /// <list type="bullet">
    /// <item>Capture all relevant state needed for continuity</item>
    /// <item>Ensure the state is consistent and complete</item>
    /// <item>Return state in a format that can be consumed by <see cref="IProviderStateImport"/></item>
    /// <item>Avoid including transient or temporary data</item>
    /// </list>
    /// <para>
    /// The returned <see cref="ProviderState"/> should be thread-safe and immutable to prevent
    /// corruption during the handover process.
    /// </para>
    /// </remarks>
    ValueTask<ProviderState> ExportStateAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the exported state from a provider during soft-swap operations.
/// This encapsulates all the data needed to restore provider functionality in a replacement instance.
/// </summary>
public sealed class ProviderState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderState"/> class.
    /// </summary>
    /// <param name="data">The state data payload.</param>
    /// <param name="version">The state format version for compatibility checking.</param>
    /// <param name="timestamp">When the state was captured.</param>
    /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
    public ProviderState(object data, string version, DateTimeOffset timestamp)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Timestamp = timestamp;
    }

    /// <summary>
    /// Gets the actual state data payload. The format and content of this data is provider-specific.
    /// </summary>
    public object Data { get; }

    /// <summary>
    /// Gets the version identifier for the state format, used for compatibility checking
    /// between exporting and importing providers.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the timestamp when this state was captured from the exporting provider.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}