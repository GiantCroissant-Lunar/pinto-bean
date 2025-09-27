// Tier-1: AI Text service contract for Yokan PintoBean service platform

using System.Threading;
using System.Threading.Tasks;
using Yokan.PintoBean.CodeGen;

namespace Yokan.PintoBean.Abstractions;

/// <summary>
/// Tier-1 contract for AI text generation functionality.
/// Engine-free interface following the 4-tier architecture pattern.
/// Supports text generation with routing strategies (PickOne default).
/// </summary>
[GenerateRegistry(typeof(IAIText))]
public interface IAIText
{
    /// <summary>
    /// Generates text based on the provided prompt asynchronously.
    /// </summary>
    /// <param name="request">The AI text generation request containing prompt and parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous text generation operation.</returns>
    Task<AITextResponse> GenerateTextAsync(AITextRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes text based on the provided partial input asynchronously.
    /// </summary>
    /// <param name="request">The AI text completion request containing partial text and parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous text completion operation.</returns>
    Task<AITextResponse> CompleteTextAsync(AITextRequest request, CancellationToken cancellationToken = default);
}