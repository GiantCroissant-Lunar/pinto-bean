using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Providers.AI.Ollama;

namespace Yokan.PintoBean.Providers.AI.Ollama.Tests;

/// <summary>
/// Unit tests for OllamaTextProvider stub implementation.
/// </summary>
public class OllamaTextProviderTests
{
    private readonly OllamaTextProvider _provider;

    public OllamaTextProviderTests()
    {
        _provider = new OllamaTextProvider();
    }

    [Fact]
    public void Provider_ShouldHaveCorrectConstants()
    {
        // Assert
        Assert.Equal("1.0.0", OllamaTextProvider.Version);
        Assert.Equal("ollama-text-v1", OllamaTextProvider.ProviderId);
        Assert.Equal("http://localhost:11434", OllamaTextProvider.DefaultEndpoint);
    }

    [Fact]
    public void Provider_ShouldUseCustomEndpoint()
    {
        // Arrange
        var customEndpoint = "http://localhost:8080";
        
        // Act
        var provider = new OllamaTextProvider(customEndpoint);
        var request = new AITextRequest { Prompt = "Test" };
        var response = provider.GenerateTextAsync(request).Result;

        // Assert
        Assert.Contains(customEndpoint, response.Content);
    }

    [Fact]
    public async Task GenerateTextAsync_ShouldReturnOllamaResponse()
    {
        // Arrange
        var request = new AITextRequest { Prompt = "Test prompt" };

        // Act
        var response = await _provider.GenerateTextAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Contains("[Ollama@", response.Content);
        Assert.Contains("Test prompt", response.Content);
        Assert.Contains("llama3.2:3b", response.ModelInfo);
        Assert.Contains("local", response.ModelInfo);
        Assert.NotNull(response.TokenUsage);
        Assert.True(response.TokenUsage.TotalTokens > 0);
        Assert.True(response.IsComplete);
    }

    [Fact]
    public async Task GenerateTextStreamAsync_ShouldReturnStreamingResponse()
    {
        // Arrange
        var request = new AITextRequest { Prompt = "Stream test" };

        // Act
        var responses = new List<AITextResponse>();
        await foreach (var response in _provider.GenerateTextStreamAsync(request))
        {
            responses.Add(response);
        }

        // Assert
        Assert.NotEmpty(responses);
        Assert.True(responses.Count > 1);
        
        // Check that all but the last response are incomplete
        var incompleteResponses = responses.Take(responses.Count - 1);
        var lastResponse = responses.Last();
        
        Assert.All(incompleteResponses, r => Assert.False(r.IsComplete));
        Assert.True(lastResponse.IsComplete);
        Assert.NotNull(lastResponse.TokenUsage);
        Assert.Contains("local streaming", lastResponse.ModelInfo);
    }

    [Fact]
    public async Task ContinueConversationAsync_ShouldIncludeHistoryCount()
    {
        // Arrange
        var request = new AITextRequest 
        { 
            Prompt = "Continue this",
            ConversationHistory = new[]
            {
                new AIMessage { Role = "user", Content = "Hello" },
                new AIMessage { Role = "assistant", Content = "Hi there" },
                new AIMessage { Role = "user", Content = "How are you?" }
            }.ToList()
        };

        // Act
        var response = await _provider.ContinueConversationAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Contains("[Ollama@", response.Content);
        Assert.Contains("3 history messages", response.Content);
        Assert.Contains("local conversation", response.ModelInfo);
        Assert.NotNull(response.TokenUsage);
        Assert.True(response.TokenUsage.PromptTokens > 0); // Should include history tokens
    }

    [Fact]
    public async Task ContinueConversationStreamAsync_ShouldStreamConversationResponse()
    {
        // Arrange
        var request = new AITextRequest 
        { 
            Prompt = "Continue conversation",
            ConversationHistory = new[]
            {
                new AIMessage { Role = "user", Content = "Hello" }
            }.ToList()
        };

        // Act
        var responses = new List<AITextResponse>();
        await foreach (var response in _provider.ContinueConversationStreamAsync(request))
        {
            responses.Add(response);
        }

        // Assert
        Assert.NotEmpty(responses);
        var lastResponse = responses.Last();
        Assert.True(lastResponse.IsComplete);
        Assert.Contains("local conversation streaming", lastResponse.ModelInfo);
    }

    [Fact]
    public async Task CompleteTextAsync_ShouldReturnCompletionResponse()
    {
        // Arrange
        var request = new AITextRequest { Prompt = "Complete this text" };

        // Act
        var response = await _provider.CompleteTextAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Contains("[Ollama@", response.Content);
        Assert.Contains("Text completion", response.Content);
        Assert.Contains("local completion", response.ModelInfo);
        Assert.NotNull(response.TokenUsage);
        Assert.True(response.IsComplete);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Short")]
    [InlineData("This is a longer prompt with more content to test token estimation for Ollama models")]
    public async Task TokenEstimation_ShouldBeReasonable(string prompt)
    {
        // Arrange
        var request = new AITextRequest { Prompt = prompt };

        // Act
        var response = await _provider.GenerateTextAsync(request);

        // Assert
        Assert.NotNull(response.TokenUsage);
        
        if (string.IsNullOrEmpty(prompt))
        {
            Assert.Equal(0, response.TokenUsage.PromptTokens);
        }
        else
        {
            Assert.True(response.TokenUsage.PromptTokens > 0);
            // Rough validation: token count should be reasonable relative to text length
            Assert.True(response.TokenUsage.PromptTokens <= prompt.Length);
        }
        
        Assert.True(response.TokenUsage.CompletionTokens > 0);
    }

    [Fact]
    public async Task StreamingDelay_ShouldBeSlowerThanOpenAI()
    {
        // Arrange
        var request = new AITextRequest { Prompt = "Test streaming timing" };
        var startTime = DateTime.UtcNow;

        // Act
        var responses = new List<AITextResponse>();
        await foreach (var response in _provider.GenerateTextStreamAsync(request))
        {
            responses.Add(response);
        }
        var endTime = DateTime.UtcNow;

        // Assert
        var duration = endTime - startTime;
        
        // Should take some time due to the 150ms delays (vs 100ms for OpenAI)
        Assert.True(duration.TotalMilliseconds > 100);
        Assert.NotEmpty(responses);
    }
}