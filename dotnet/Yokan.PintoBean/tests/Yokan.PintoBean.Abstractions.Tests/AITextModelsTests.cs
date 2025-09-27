// Tests for AI text models
using System;
using System.Collections.Generic;
using Xunit;
using Yokan.PintoBean.Abstractions;

namespace Yokan.PintoBean.Abstractions.Tests;

/// <summary>
/// Tests for AI text request and response models.
/// </summary>
public class AITextModelsTests
{
    [Fact]
    public void AITextRequest_WithRequiredPrompt_ShouldCreateSuccessfully()
    {
        // Arrange & Act
        var request = new AITextRequest
        {
            Prompt = "Generate a creative story about a robot"
        };

        // Assert
        Assert.Equal("Generate a creative story about a robot", request.Prompt);
        Assert.Null(request.MaxTokens);
        Assert.Null(request.Temperature);
        Assert.Null(request.ModelFamily);
        Assert.Null(request.ContextWindow);
        Assert.Null(request.Parameters);
        Assert.Null(request.UserId);
        Assert.Null(request.SessionId);
    }

    [Fact]
    public void AITextRequest_WithAllProperties_ShouldCreateSuccessfully()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { { "top_p", 0.9 } };

        // Act
        var request = new AITextRequest
        {
            Prompt = "Complete this code",
            MaxTokens = 1000,
            Temperature = 0.7,
            ModelFamily = "gpt",
            ContextWindow = 4096,
            Parameters = parameters,
            UserId = "user123",
            SessionId = "session456"
        };

        // Assert
        Assert.Equal("Complete this code", request.Prompt);
        Assert.Equal(1000, request.MaxTokens);
        Assert.Equal(0.7, request.Temperature);
        Assert.Equal("gpt", request.ModelFamily);
        Assert.Equal(4096, request.ContextWindow);
        Assert.Equal(parameters, request.Parameters);
        Assert.Equal("user123", request.UserId);
        Assert.Equal("session456", request.SessionId);
    }

    [Fact]
    public void AITextResponse_WithRequiredText_ShouldCreateSuccessfully()
    {
        // Arrange & Act
        var response = new AITextResponse
        {
            Text = "Once upon a time, there was a friendly robot..."
        };

        // Assert
        Assert.Equal("Once upon a time, there was a friendly robot...", response.Text);
        Assert.True(response.Timestamp <= DateTime.UtcNow);
        Assert.True(response.Timestamp > DateTime.UtcNow.AddSeconds(-1)); // Should be very recent
        Assert.Null(response.ModelUsed);
        Assert.Null(response.TokensGenerated);
        Assert.Null(response.TokensConsumed);
        Assert.Null(response.ServiceInfo);
        Assert.Null(response.Metrics);
    }

    [Fact]
    public void AITextResponse_WithAllProperties_ShouldCreateSuccessfully()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var metrics = new Dictionary<string, object>
        {
            { "confidence", 0.95 },
            { "safety_score", 0.98 }
        };

        // Act
        var response = new AITextResponse
        {
            Text = "Generated text content",
            Timestamp = timestamp,
            ModelUsed = "gpt-4",
            TokensGenerated = 150,
            TokensConsumed = 50,
            ServiceInfo = "OpenAI Provider v1.0",
            Metrics = metrics
        };

        // Assert
        Assert.Equal("Generated text content", response.Text);
        Assert.Equal(timestamp, response.Timestamp);
        Assert.Equal("gpt-4", response.ModelUsed);
        Assert.Equal(150, response.TokensGenerated);
        Assert.Equal(50, response.TokensConsumed);
        Assert.Equal("OpenAI Provider v1.0", response.ServiceInfo);
        Assert.Equal(metrics, response.Metrics);
    }

    [Fact]
    public void AITextRequest_ShouldSupportRecordEquality()
    {
        // Arrange
        var request1 = new AITextRequest { Prompt = "Test prompt" };
        var request2 = new AITextRequest { Prompt = "Test prompt" };
        var request3 = new AITextRequest { Prompt = "Different prompt" };

        // Act & Assert
        Assert.Equal(request1, request2);
        Assert.NotEqual(request1, request3);
    }

    [Fact]
    public void AITextResponse_ShouldSupportRecordEquality()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var response1 = new AITextResponse { Text = "Test response", Timestamp = timestamp };
        var response2 = new AITextResponse { Text = "Test response", Timestamp = timestamp };
        var response3 = new AITextResponse { Text = "Different response", Timestamp = timestamp };

        // Act & Assert
        Assert.Equal(response1, response2);
        Assert.NotEqual(response1, response3);
    }
}