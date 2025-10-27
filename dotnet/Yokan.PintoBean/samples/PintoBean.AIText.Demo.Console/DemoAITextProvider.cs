using System.Runtime.CompilerServices;
using Yokan.PintoBean.Abstractions;

namespace PintoBean.AIText.Demo.Console;

/// <summary>
/// Demo AI text provider that simulates streaming text generation with proper cancellation support.
/// </summary>
public class DemoAITextProvider : IAIText
{
    private readonly string _providerName;
    private readonly int _streamDelayMs;

    public DemoAITextProvider(string providerName, int streamDelayMs = 200)
    {
        _providerName = providerName;
        _streamDelayMs = streamDelayMs;
    }

    public Task<AITextResponse> GenerateTextAsync(AITextRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AITextResponse
        {
            Content = $"[{_providerName}] Generated: {request.Prompt}",
            ModelInfo = _providerName,
            IsComplete = true
        });
    }

    public async IAsyncEnumerable<AITextResponse> GenerateTextStreamAsync(AITextRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Simulate streaming text generation with proper cancellation support
        var tokens = new[]
        {
            $"[{_providerName}]",
            "Starting",
            "to",
            "generate",
            "text",
            "for",
            "prompt:",
            $"'{request.Prompt}'",
            "...",
            "Generation",
            "complete!"
        };

        for (int i = 0; i < tokens.Length; i++)
        {
            // Check for cancellation before each token
            cancellationToken.ThrowIfCancellationRequested();

            yield return new AITextResponse
            {
                Content = tokens[i],
                ModelInfo = _providerName,
                IsComplete = i == tokens.Length - 1
            };

            // Add delay between tokens to simulate real streaming, but respect cancellation
            if (i < tokens.Length - 1)
            {
                try
                {
                    await Task.Delay(_streamDelayMs, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    System.Console.WriteLine($"\n[{_providerName}] Stream cancelled gracefully");
                    throw;
                }
            }
        }
    }

    public Task<AITextResponse> ContinueConversationAsync(AITextRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AITextResponse
        {
            Content = $"[{_providerName}] Continuing conversation: {request.Prompt}",
            ModelInfo = _providerName,
            IsComplete = true
        });
    }

    public async IAsyncEnumerable<AITextResponse> ContinueConversationStreamAsync(AITextRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Simulate conversation streaming
        var responses = new[]
        {
            $"[{_providerName}] Let me continue our conversation...",
            $"You mentioned: '{request.Prompt}'",
            "That's an interesting point to discuss further.",
            "What would you like to explore next?"
        };

        for (int i = 0; i < responses.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new AITextResponse
            {
                Content = responses[i],
                ModelInfo = _providerName,
                IsComplete = i == responses.Length - 1
            };

            if (i < responses.Length - 1)
            {
                try
                {
                    await Task.Delay(_streamDelayMs, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    System.Console.WriteLine($"\n[{_providerName}] Conversation stream cancelled gracefully");
                    throw;
                }
            }
        }
    }

    public Task<AITextResponse> CompleteTextAsync(AITextRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AITextResponse
        {
            Content = $"[{_providerName}] Completed: {request.Prompt}",
            ModelInfo = _providerName,
            IsComplete = true
        });
    }
}