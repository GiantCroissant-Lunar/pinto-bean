// Tier-1: DTOs for AI service contracts

using System;
using System.Collections.Generic;

namespace Yokan.PintoBean.Abstractions;

#region AI Text Generation Models

/// <summary>
/// Request DTO for AI text generation operations.
/// Contains the prompt and optional parameters for text generation.
/// </summary>
public sealed record AITextRequest
{
    /// <summary>
    /// The input prompt or text to process. Required.
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// Optional maximum number of tokens to generate.
    /// If not specified, uses the service default.
    /// </summary>
    public int? MaxTokens { get; init; }

    /// <summary>
    /// Optional temperature for randomness control (0.0 to 1.0).
    /// Lower values produce more deterministic outputs.
    /// </summary>
    public double? Temperature { get; init; }

    /// <summary>
    /// Optional system message or context for the AI model.
    /// Provides instructions or context for the generation.
    /// </summary>
    public string? SystemMessage { get; init; }

    /// <summary>
    /// Optional conversation history for multi-turn interactions.
    /// Contains previous messages in the conversation.
    /// </summary>
    public List<AIMessage>? ConversationHistory { get; init; }

    /// <summary>
    /// Optional metadata for the request (e.g., user ID, session ID).
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Response DTO for AI text generation operations.
/// Contains the generated text and metadata.
/// </summary>
public sealed record AITextResponse
{
    /// <summary>
    /// The generated text content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// The timestamp when the response was generated.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Optional model information that generated the response.
    /// </summary>
    public string? ModelInfo { get; init; }

    /// <summary>
    /// Optional token usage information.
    /// </summary>
    public AITokenUsage? TokenUsage { get; init; }

    /// <summary>
    /// Indicates if this is the final response in a streaming context.
    /// True for non-streaming responses or the last chunk in streaming.
    /// </summary>
    public bool IsComplete { get; init; } = true;
}

/// <summary>
/// Represents a message in a conversation for AI text generation.
/// </summary>
public sealed record AIMessage
{
    /// <summary>
    /// The role of the message sender (e.g., "user", "assistant", "system").
    /// </summary>
    public required string Role { get; init; }

    /// <summary>
    /// The content of the message.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Optional timestamp when the message was created.
    /// </summary>
    public DateTime? Timestamp { get; init; }
}

/// <summary>
/// Token usage information for AI operations.
/// </summary>
public sealed record AITokenUsage
{
    /// <summary>
    /// Number of tokens in the input prompt.
    /// </summary>
    public int PromptTokens { get; init; }

    /// <summary>
    /// Number of tokens in the generated completion.
    /// </summary>
    public int CompletionTokens { get; init; }

    /// <summary>
    /// Total number of tokens used (prompt + completion).
    /// </summary>
    public int TotalTokens => PromptTokens + CompletionTokens;
}

#endregion

#region Vector Operations Models

/// <summary>
/// Request DTO for vector operations (embedding generation, similarity search).
/// </summary>
public sealed record VectorRequest
{
    /// <summary>
    /// The input text to convert to vector embedding. Required.
    /// </summary>
    public required string Input { get; init; }

    /// <summary>
    /// Optional model to use for embedding generation.
    /// If not specified, uses the service default.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Optional dimension size for the resulting vector.
    /// If not specified, uses the model default.
    /// </summary>
    public int? Dimensions { get; init; }

    /// <summary>
    /// Optional metadata for the request.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Response DTO for vector operations.
/// Contains the generated vector embedding and metadata.
/// </summary>
public sealed record VectorResponse
{
    /// <summary>
    /// The generated vector embedding as an array of floats.
    /// </summary>
    public required float[] Embedding { get; init; }

    /// <summary>
    /// The dimension size of the embedding vector.
    /// </summary>
    public int Dimensions => Embedding.Length;

    /// <summary>
    /// The timestamp when the response was generated.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Optional model information that generated the embedding.
    /// </summary>
    public string? ModelInfo { get; init; }

    /// <summary>
    /// Optional token usage information.
    /// </summary>
    public AITokenUsage? TokenUsage { get; init; }
}

/// <summary>
/// Request DTO for vector similarity search operations.
/// </summary>
public sealed record VectorSearchRequest
{
    /// <summary>
    /// The query vector to search for similar vectors. Required.
    /// </summary>
    public required float[] QueryVector { get; init; }

    /// <summary>
    /// Optional maximum number of similar vectors to return.
    /// Defaults to 10 if not specified.
    /// </summary>
    public int? TopK { get; init; }

    /// <summary>
    /// Optional minimum similarity threshold (0.0 to 1.0).
    /// Only vectors with similarity above this threshold are returned.
    /// </summary>
    public double? MinSimilarity { get; init; }

    /// <summary>
    /// Optional metadata filters for the search.
    /// </summary>
    public Dictionary<string, object>? Filters { get; init; }
}

/// <summary>
/// Represents a vector search result with similarity score.
/// </summary>
public sealed record VectorSearchResult
{
    /// <summary>
    /// The vector embedding.
    /// </summary>
    public required float[] Vector { get; init; }

    /// <summary>
    /// The similarity score (0.0 to 1.0, higher is more similar).
    /// </summary>
    public required double SimilarityScore { get; init; }

    /// <summary>
    /// Optional identifier for the vector.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Optional metadata associated with the vector.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}

#endregion

#region Tool Call Models

/// <summary>
/// Request DTO for AI tool calling operations.
/// Contains the available tools and context for tool selection and execution.
/// </summary>
public sealed record ToolCallRequest
{
    /// <summary>
    /// The user message or query that may require tool usage. Required.
    /// </summary>
    public required string Query { get; init; }

    /// <summary>
    /// Available tools that the AI can choose from. Required.
    /// </summary>
    public required List<ToolDefinition> AvailableTools { get; init; }

    /// <summary>
    /// Optional conversation history for context.
    /// </summary>
    public List<AIMessage>? ConversationHistory { get; init; }

    /// <summary>
    /// Optional system message providing context about tool usage.
    /// </summary>
    public string? SystemMessage { get; init; }

    /// <summary>
    /// Optional metadata for the request.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Response DTO for AI tool calling operations.
/// Contains the AI's response and any tool calls it decided to make.
/// </summary>
public sealed record ToolCallResponse
{
    /// <summary>
    /// The AI's text response to the query.
    /// </summary>
    public required string Response { get; init; }

    /// <summary>
    /// Tool calls that the AI decided to make, if any.
    /// </summary>
    public List<ToolCall>? ToolCalls { get; init; }

    /// <summary>
    /// The timestamp when the response was generated.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Optional model information that generated the response.
    /// </summary>
    public string? ModelInfo { get; init; }

    /// <summary>
    /// Optional token usage information.
    /// </summary>
    public AITokenUsage? TokenUsage { get; init; }

    /// <summary>
    /// Indicates if the AI requires tool execution results to continue.
    /// </summary>
    public bool RequiresToolExecution => ToolCalls?.Count > 0;
}

/// <summary>
/// Defines an available tool that the AI can call.
/// </summary>
public sealed record ToolDefinition
{
    /// <summary>
    /// The name/identifier of the tool. Required.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Description of what the tool does. Required.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The parameters schema for the tool (typically JSON schema).
    /// Defines what parameters the tool accepts and their types.
    /// </summary>
    public required Dictionary<string, object> ParametersSchema { get; init; }

    /// <summary>
    /// Optional examples of how to use the tool.
    /// </summary>
    public List<ToolExample>? Examples { get; init; }
}

/// <summary>
/// Represents an AI's decision to call a specific tool.
/// </summary>
public sealed record ToolCall
{
    /// <summary>
    /// Unique identifier for this tool call. Required.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The name of the tool to call. Required.
    /// </summary>
    public required string ToolName { get; init; }

    /// <summary>
    /// The arguments/parameters to pass to the tool.
    /// </summary>
    public required Dictionary<string, object> Arguments { get; init; }

    /// <summary>
    /// Optional reasoning from the AI about why it chose to call this tool.
    /// </summary>
    public string? Reasoning { get; init; }
}

/// <summary>
/// Example usage of a tool for AI training/context.
/// </summary>
public sealed record ToolExample
{
    /// <summary>
    /// Example input query that would trigger this tool.
    /// </summary>
    public required string Query { get; init; }

    /// <summary>
    /// Example parameters for the tool call.
    /// </summary>
    public required Dictionary<string, object> Parameters { get; init; }

    /// <summary>
    /// Optional description of the example.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Result of executing a tool call.
/// </summary>
public sealed record ToolCallResult
{
    /// <summary>
    /// The ID of the tool call this result corresponds to. Required.
    /// </summary>
    public required string ToolCallId { get; init; }

    /// <summary>
    /// Whether the tool execution was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// The result data from the tool execution.
    /// For successful calls, contains the tool's output.
    /// For failed calls, may contain error information.
    /// </summary>
    public object? Result { get; init; }

    /// <summary>
    /// Optional error message if the tool execution failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The timestamp when the tool execution completed.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

#endregion