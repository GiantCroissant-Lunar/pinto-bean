// Tier-1: AI text generation service contract for Yokan PintoBean service platform

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Yokan.PintoBean.CodeGen;

namespace Yokan.PintoBean.Abstractions;

/// <summary>
/// Tier-1 AI text generation service contract.
/// Engine-free interface following the 4-tier architecture pattern.
/// Provides both streaming and non-streaming text generation capabilities.
/// Supports text generation with routing strategies (PickOne default).
/// </summary>
[GenerateRegistry(typeof(IAIText))]
public interface IAIText
{
    /// <summary>
    /// Generates text based on the provided prompt and parameters.
    /// Returns a single complete response.
    /// </summary>
    /// <param name="request">The text generation request containing prompt and parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A complete text generation response.</returns>
    Task<AITextResponse> GenerateTextAsync(AITextRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates text with streaming support, returning partial responses as they become available.
    /// Useful for real-time text generation and improved user experience.
    /// </summary>
    /// <param name="request">The text generation request containing prompt and parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>An async enumerable of partial text generation responses.</returns>
    IAsyncEnumerable<AITextResponse> GenerateTextStreamAsync(AITextRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Continues a conversation with the AI using the provided context and new message.
    /// Maintains conversation state and history for multi-turn interactions.
    /// </summary>
    /// <param name="request">The text generation request with conversation history.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A text generation response continuing the conversation.</returns>
    Task<AITextResponse> ContinueConversationAsync(AITextRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Continues a conversation with streaming support.
    /// Combines conversation context with real-time streaming responses.
    /// </summary>
    /// <param name="request">The text generation request with conversation history.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>An async enumerable of partial conversation responses.</returns>
    IAsyncEnumerable<AITextResponse> ContinueConversationStreamAsync(AITextRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes text based on the provided partial input asynchronously.
    /// </summary>
    /// <param name="request">The AI text completion request containing partial text and parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous text completion operation.</returns>
    Task<AITextResponse> CompleteTextAsync(AITextRequest request, CancellationToken cancellationToken = default);
}