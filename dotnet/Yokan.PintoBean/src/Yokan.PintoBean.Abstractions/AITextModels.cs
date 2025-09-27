// Tier-1: DTOs for AI text service contract

using System;
using System.Collections.Generic;

namespace Yokan.PintoBean.Abstractions;

/// <summary>
/// Request DTO for AI text generation operations.
/// Contains the prompt and parameters for text generation operations.
/// </summary>
public sealed record AITextRequest
{
    /// <summary>
    /// The input prompt or text to process. Required.
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// Maximum number of tokens to generate. Optional.
    /// Controls the length of the generated text.
    /// </summary>
    public int? MaxTokens { get; init; }

    /// <summary>
    /// Temperature parameter for controlling randomness (0.0 to 2.0).
    /// Higher values produce more creative but less focused output.
    /// </summary>
    public double? Temperature { get; init; }

    /// <summary>
    /// Optional model family hint (e.g., "gpt", "claude", "llama").
    /// Allows providers to use specific model families when available.
    /// </summary>
    public string? ModelFamily { get; init; }

    /// <summary>
    /// Optional context window requirement in tokens.
    /// Ensures the provider can handle the required context size.
    /// </summary>
    public int? ContextWindow { get; init; }

    /// <summary>
    /// Optional additional parameters as key-value pairs.
    /// Contains provider-specific configuration options.
    /// </summary>
    public Dictionary<string, object>? Parameters { get; init; }

    /// <summary>
    /// Optional user identifier for tracking and personalization.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Optional session identifier for maintaining conversation context.
    /// </summary>
    public string? SessionId { get; init; }
}

/// <summary>
/// Response DTO for AI text generation operations.
/// Contains the generated text and metadata about the operation.
/// </summary>
public sealed record AITextResponse
{
    /// <summary>
    /// The generated text content.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// The timestamp when the response was generated.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// The model family used for generation (e.g., "gpt-4", "claude-3").
    /// </summary>
    public string? ModelUsed { get; init; }

    /// <summary>
    /// Number of tokens in the generated response.
    /// </summary>
    public int? TokensGenerated { get; init; }

    /// <summary>
    /// Number of tokens in the input prompt.
    /// </summary>
    public int? TokensConsumed { get; init; }

    /// <summary>
    /// Optional metadata about the service that generated the response.
    /// Contains provider-specific information.
    /// </summary>
    public string? ServiceInfo { get; init; }

    /// <summary>
    /// Optional quality metrics for the generated text.
    /// Contains confidence scores, safety ratings, etc.
    /// </summary>
    public Dictionary<string, object>? Metrics { get; init; }
}