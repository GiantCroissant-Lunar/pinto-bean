using System.Threading;
using System.Threading.Tasks;
using Yokan.PintoBean.CodeGen;

namespace Yokan.PintoBean.Abstractions;

/// <summary>
/// Tier-1: Scene flow service contract for deterministic scene transitions.
/// Provides async scene loading capabilities with cancellation support.
/// </summary>
[GenerateRegistry(typeof(ISceneFlow))]
public interface ISceneFlow
{
    /// <summary>
    /// Asynchronously loads the specified scene.
    /// </summary>
    /// <param name="scene">The name or identifier of the scene to load.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous load operation.</returns>
    Task LoadAsync(string scene, CancellationToken cancellationToken = default);
}