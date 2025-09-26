// Tier-3: Provider lifecycle contract for state import during transitions

using System.Threading;
using System.Threading.Tasks;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Defines a contract for providers that can import state from a previous provider instance
/// during soft-swap operations. This enables seamless transitions by allowing a new provider
/// to restore the operational state from its predecessor.
/// </summary>
public interface IProviderStateImport
{
    /// <summary>
    /// Imports state from a previous provider instance, allowing this provider to continue
    /// operations seamlessly from where the previous provider left off.
    /// </summary>
    /// <param name="state">
    /// The state data exported from the previous provider instance via <see cref="IProviderStateExport.ExportStateAsync"/>.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token to abort the import operation if needed.
    /// </param>
    /// <returns>
    /// A task that completes when the state has been successfully imported and the provider
    /// is ready to handle requests based on the restored state.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is typically called during provider initialization, before the provider
    /// starts accepting requests. Implementations should:
    /// </para>
    /// <list type="bullet">
    /// <item>Validate the state version for compatibility</item>
    /// <item>Restore all necessary internal state from the provided data</item>
    /// <item>Initialize any resources needed based on the imported state</item>
    /// <item>Ensure the provider is fully operational after import completion</item>
    /// </list>
    /// <para>
    /// If the state cannot be imported (due to version incompatibility, corruption, etc.),
    /// implementations should throw an appropriate exception with descriptive error information.
    /// </para>
    /// <para>
    /// This method should be thread-safe and idempotent - calling it multiple times with the
    /// same state should produce the same result without side effects.
    /// </para>
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">Thrown when state is null.</exception>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when the provider is not in a valid state for importing or when import fails.
    /// </exception>
    /// <exception cref="System.NotSupportedException">
    /// Thrown when the state version is incompatible with this provider implementation.
    /// </exception>
    Task ImportStateAsync(ProviderState state, CancellationToken cancellationToken = default);
}