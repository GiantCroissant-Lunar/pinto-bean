// Tier-3: Provider lifecycle contract for graceful shutdown and soft-swap safety

using System.Threading;
using System.Threading.Tasks;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Defines a contract for providers that can be gracefully quiesced during soft-swap operations.
/// When implemented by a provider, this allows the runtime to request that the provider stop
/// accepting new work while completing in-flight operations, enabling safe provider transitions.
/// </summary>
public interface IQuiesceable
{
    /// <summary>
    /// Initiates a graceful quiesce operation, asking the provider to stop accepting new work
    /// while allowing existing operations to complete naturally.
    /// </summary>
    /// <param name="cancellationToken">
    /// Cancellation token to abort the quiesce operation if needed. The provider should
    /// respect this token and may abort ongoing operations if cancellation is requested.
    /// </param>
    /// <returns>
    /// A task that completes when the provider has successfully quiesced (stopped accepting
    /// new work and completed or safely aborted existing operations).
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is called by the runtime during soft-swap scenarios where a provider
    /// needs to be replaced without disrupting active operations. The provider should:
    /// </para>
    /// <list type="bullet">
    /// <item>Stop accepting new requests/operations immediately</item>
    /// <item>Allow existing operations to complete naturally (respecting the cancellation token)</item>
    /// <item>Clean up any resources that won't be needed after quiescing</item>
    /// <item>Return only when the provider is in a safe state for replacement</item>
    /// </list>
    /// <para>
    /// Implementations should be thread-safe as this method may be called concurrently
    /// with other provider operations.
    /// </para>
    /// </remarks>
    Task QuiesceAsync(CancellationToken cancellationToken = default);
}