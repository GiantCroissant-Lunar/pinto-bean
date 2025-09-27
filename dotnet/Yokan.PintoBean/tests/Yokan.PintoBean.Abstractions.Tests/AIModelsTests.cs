using System;
using System.Collections.Generic;
using Yokan.PintoBean.Abstractions;

namespace Yokan.PintoBean.Abstractions.Tests;

/// <summary>
/// Tests for AI model DTOs.
/// </summary>
public class AIModelsTests
{
    #region AITextRequest Tests

    [Fact]
    public void AITextRequest_WithRequiredPrompt_ShouldCreate()
    {
        // Arrange & Act
        var request = new AITextRequest { Prompt = "Hello, world!" };

        // Assert
        Assert.Equal("Hello, world!", request.Prompt);
        Assert.Null(request.MaxTokens);
        Assert.Null(request.Temperature);
        Assert.Null(request.SystemMessage);
        Assert.Null(request.ConversationHistory);
        Assert.Null(request.Metadata);
    }

    [Fact]
    public void AITextRequest_WithAllProperties_ShouldCreate()
    {
        // Arrange
        var history = new List<AIMessage>
        {
            new() { Role = "user", Content = "Hi there!" }
        };
        var metadata = new Dictionary<string, object> { { "userId", "123" } };

        // Act
        var request = new AITextRequest
        {
            Prompt = "Continue the conversation",
            MaxTokens = 100,
            Temperature = 0.7,
            SystemMessage = "You are a helpful assistant",
            ConversationHistory = history,
            Metadata = metadata
        };

        // Assert
        Assert.Equal("Continue the conversation", request.Prompt);
        Assert.Equal(100, request.MaxTokens);
        Assert.Equal(0.7, request.Temperature);
        Assert.Equal("You are a helpful assistant", request.SystemMessage);
        Assert.Same(history, request.ConversationHistory);
        Assert.Same(metadata, request.Metadata);
    }

    [Fact]
    public void AITextRequest_RecordEquality_ShouldWork()
    {
        // Arrange
        var request1 = new AITextRequest { Prompt = "Test", MaxTokens = 50 };
        var request2 = new AITextRequest { Prompt = "Test", MaxTokens = 50 };
        var request3 = new AITextRequest { Prompt = "Different", MaxTokens = 50 };

        // Act & Assert
        Assert.Equal(request1, request2);
        Assert.NotEqual(request1, request3);
    }

    #endregion

    #region AITextResponse Tests

    [Fact]
    public void AITextResponse_WithRequiredContent_ShouldCreate()
    {
        // Arrange & Act
        var response = new AITextResponse { Content = "Generated text" };

        // Assert
        Assert.Equal("Generated text", response.Content);
        Assert.True(response.Timestamp <= DateTime.UtcNow);
        Assert.True(response.Timestamp > DateTime.UtcNow.AddMinutes(-1));
        Assert.Null(response.ModelInfo);
        Assert.Null(response.TokenUsage);
        Assert.True(response.IsComplete);
    }

    [Fact]
    public void AITextResponse_WithAllProperties_ShouldCreate()
    {
        // Arrange
        var tokenUsage = new AITokenUsage { PromptTokens = 10, CompletionTokens = 20 };
        var timestamp = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var response = new AITextResponse
        {
            Content = "Generated response",
            Timestamp = timestamp,
            ModelInfo = "gpt-4",
            TokenUsage = tokenUsage,
            IsComplete = false
        };

        // Assert
        Assert.Equal("Generated response", response.Content);
        Assert.Equal(timestamp, response.Timestamp);
        Assert.Equal("gpt-4", response.ModelInfo);
        Assert.Same(tokenUsage, response.TokenUsage);
        Assert.False(response.IsComplete);
    }

    #endregion

    #region AIMessage Tests

    [Fact]
    public void AIMessage_WithRequiredProperties_ShouldCreate()
    {
        // Arrange & Act
        var message = new AIMessage { Role = "user", Content = "Hello!" };

        // Assert
        Assert.Equal("user", message.Role);
        Assert.Equal("Hello!", message.Content);
        Assert.Null(message.Timestamp);
    }

    [Fact]
    public void AIMessage_WithAllProperties_ShouldCreate()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var message = new AIMessage
        {
            Role = "assistant",
            Content = "Hi there!",
            Timestamp = timestamp
        };

        // Assert
        Assert.Equal("assistant", message.Role);
        Assert.Equal("Hi there!", message.Content);
        Assert.Equal(timestamp, message.Timestamp);
    }

    #endregion

    #region AITokenUsage Tests

    [Fact]
    public void AITokenUsage_TotalTokens_ShouldCalculateCorrectly()
    {
        // Arrange & Act
        var usage = new AITokenUsage { PromptTokens = 15, CompletionTokens = 25 };

        // Assert
        Assert.Equal(15, usage.PromptTokens);
        Assert.Equal(25, usage.CompletionTokens);
        Assert.Equal(40, usage.TotalTokens);
    }

    #endregion

    #region VectorRequest Tests

    [Fact]
    public void VectorRequest_WithRequiredInput_ShouldCreate()
    {
        // Arrange & Act
        var request = new VectorRequest { Input = "Text to embed" };

        // Assert
        Assert.Equal("Text to embed", request.Input);
        Assert.Null(request.Model);
        Assert.Null(request.Dimensions);
        Assert.Null(request.Metadata);
    }

    [Fact]
    public void VectorRequest_WithAllProperties_ShouldCreate()
    {
        // Arrange
        var metadata = new Dictionary<string, object> { { "source", "test" } };

        // Act
        var request = new VectorRequest
        {
            Input = "Text to vectorize",
            Model = "text-embedding-ada-002",
            Dimensions = 1536,
            Metadata = metadata
        };

        // Assert
        Assert.Equal("Text to vectorize", request.Input);
        Assert.Equal("text-embedding-ada-002", request.Model);
        Assert.Equal(1536, request.Dimensions);
        Assert.Same(metadata, request.Metadata);
    }

    #endregion

    #region VectorResponse Tests

    [Fact]
    public void VectorResponse_WithRequiredEmbedding_ShouldCreate()
    {
        // Arrange
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };

        // Act
        var response = new VectorResponse { Embedding = embedding };

        // Assert
        Assert.Same(embedding, response.Embedding);
        Assert.Equal(3, response.Dimensions);
        Assert.True(response.Timestamp <= DateTime.UtcNow);
        Assert.Null(response.ModelInfo);
        Assert.Null(response.TokenUsage);
    }

    [Fact]
    public void VectorResponse_Dimensions_ShouldReturnEmbeddingLength()
    {
        // Arrange
        var embedding = new float[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f };

        // Act
        var response = new VectorResponse { Embedding = embedding };

        // Assert
        Assert.Equal(5, response.Dimensions);
    }

    #endregion

    #region VectorSearchRequest Tests

    [Fact]
    public void VectorSearchRequest_WithRequiredQueryVector_ShouldCreate()
    {
        // Arrange
        var queryVector = new float[] { 0.1f, 0.2f, 0.3f };

        // Act
        var request = new VectorSearchRequest { QueryVector = queryVector };

        // Assert
        Assert.Same(queryVector, request.QueryVector);
        Assert.Null(request.TopK);
        Assert.Null(request.MinSimilarity);
        Assert.Null(request.Filters);
    }

    #endregion

    #region VectorSearchResult Tests

    [Fact]
    public void VectorSearchResult_WithRequiredProperties_ShouldCreate()
    {
        // Arrange
        var vector = new float[] { 0.1f, 0.2f };

        // Act
        var result = new VectorSearchResult { Vector = vector, SimilarityScore = 0.85 };

        // Assert
        Assert.Same(vector, result.Vector);
        Assert.Equal(0.85, result.SimilarityScore);
        Assert.Null(result.Id);
        Assert.Null(result.Metadata);
    }

    #endregion

    #region ToolCallRequest Tests

    [Fact]
    public void ToolCallRequest_WithRequiredProperties_ShouldCreate()
    {
        // Arrange
        var tools = new List<ToolDefinition>
        {
            new()
            {
                Name = "test_tool",
                Description = "A test tool",
                ParametersSchema = new Dictionary<string, object>()
            }
        };

        // Act
        var request = new ToolCallRequest { Query = "Test query", AvailableTools = tools };

        // Assert
        Assert.Equal("Test query", request.Query);
        Assert.Same(tools, request.AvailableTools);
        Assert.Null(request.ConversationHistory);
        Assert.Null(request.SystemMessage);
        Assert.Null(request.Metadata);
    }

    #endregion

    #region ToolCallResponse Tests

    [Fact]
    public void ToolCallResponse_RequiresToolExecution_ShouldWork()
    {
        // Arrange
        var toolCalls = new List<ToolCall>
        {
            new()
            {
                Id = "call_1",
                ToolName = "test_tool",
                Arguments = new Dictionary<string, object>()
            }
        };

        // Act
        var response1 = new ToolCallResponse { Response = "Test", ToolCalls = toolCalls };
        var response2 = new ToolCallResponse { Response = "Test", ToolCalls = null };

        // Assert
        Assert.True(response1.RequiresToolExecution);
        Assert.False(response2.RequiresToolExecution);
    }

    #endregion

    #region ToolDefinition Tests

    [Fact]
    public void ToolDefinition_WithRequiredProperties_ShouldCreate()
    {
        // Arrange
        var schema = new Dictionary<string, object> { { "type", "object" } };

        // Act
        var tool = new ToolDefinition
        {
            Name = "calculator",
            Description = "Performs calculations",
            ParametersSchema = schema
        };

        // Assert
        Assert.Equal("calculator", tool.Name);
        Assert.Equal("Performs calculations", tool.Description);
        Assert.Same(schema, tool.ParametersSchema);
        Assert.Null(tool.Examples);
    }

    #endregion

    #region ToolCall Tests

    [Fact]
    public void ToolCall_WithRequiredProperties_ShouldCreate()
    {
        // Arrange
        var arguments = new Dictionary<string, object> { { "x", 5 }, { "y", 3 } };

        // Act
        var toolCall = new ToolCall
        {
            Id = "call_123",
            ToolName = "add",
            Arguments = arguments
        };

        // Assert
        Assert.Equal("call_123", toolCall.Id);
        Assert.Equal("add", toolCall.ToolName);
        Assert.Same(arguments, toolCall.Arguments);
        Assert.Null(toolCall.Reasoning);
    }

    #endregion

    #region ToolCallResult Tests

    [Fact]
    public void ToolCallResult_WithRequiredProperties_ShouldCreate()
    {
        // Arrange & Act
        var result = new ToolCallResult
        {
            ToolCallId = "call_123",
            Success = true
        };

        // Assert
        Assert.Equal("call_123", result.ToolCallId);
        Assert.True(result.Success);
        Assert.Null(result.Result);
        Assert.Null(result.ErrorMessage);
        Assert.True(result.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void ToolCallResult_WithError_ShouldCreate()
    {
        // Arrange & Act
        var result = new ToolCallResult
        {
            ToolCallId = "call_456",
            Success = false,
            ErrorMessage = "Tool execution failed"
        };

        // Assert
        Assert.Equal("call_456", result.ToolCallId);
        Assert.False(result.Success);
        Assert.Equal("Tool execution failed", result.ErrorMessage);
    }

    #endregion
}