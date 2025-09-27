// Tests for AIText façade functionality
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for AI text façade routing through registry.
/// These tests simulate the expected behavior of the generated façade.
/// </summary>
public class AITextFacadeTests
{
    /// <summary>
    /// Test AI text provider implementation for testing façade routing.
    /// </summary>
    public class TestAITextProvider : IAIText
    {
        public int GenerateTextCallCount { get; private set; }
        public int CompleteTextCallCount { get; private set; }
        public AITextRequest? LastRequest { get; private set; }

        public Task<AITextResponse> GenerateTextAsync(AITextRequest request, CancellationToken cancellationToken = default)
        {
            GenerateTextCallCount++;
            LastRequest = request;
            return Task.FromResult(new AITextResponse
            {
                Text = $"Generated: {request.Prompt}",
                ServiceInfo = "TestAITextProvider"
            });
        }

        public Task<AITextResponse> CompleteTextAsync(AITextRequest request, CancellationToken cancellationToken = default)
        {
            CompleteTextCallCount++;
            LastRequest = request;
            return Task.FromResult(new AITextResponse
            {
                Text = $"Completed: {request.Prompt}",
                ServiceInfo = "TestAITextProvider"
            });
        }
    }

    /// <summary>
    /// Test AI text façade that simulates the generated façade pattern.
    /// This demonstrates how the real generated façade should work.
    /// </summary>
    public class TestAITextFacade : IAIText
    {
        private readonly IServiceRegistry _registry;

        public TestAITextFacade(IServiceRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public async Task<AITextResponse> GenerateTextAsync(AITextRequest request, CancellationToken cancellationToken = default)
        {
            // Simulate generated façade pattern: delegate to typed registry
            var typedRegistry = _registry.For<IAIText>();
            return await typedRegistry.InvokeAsync((service, ct) => service.GenerateTextAsync(request, ct), cancellationToken);
        }

        public async Task<AITextResponse> CompleteTextAsync(AITextRequest request, CancellationToken cancellationToken = default)
        {
            // Simulate generated façade pattern: delegate to typed registry
            var typedRegistry = _registry.For<IAIText>();
            return await typedRegistry.InvokeAsync((service, ct) => service.CompleteTextAsync(request, ct), cancellationToken);  
        }
    }

    [Fact]
    public async Task AITextFacade_GenerateTextAsync_ShouldRouteViaRegistry()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAIRegistry();
        services.AddNoOpAspectRuntime();
        services.AddResilienceExecutor();

        var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IServiceRegistry>();
        
        var testProvider = new TestAITextProvider();
        registry.Register<IAIText>(testProvider, ProviderCapabilities.Create("test-provider"));

        var facade = new TestAITextFacade(registry);
        var request = new AITextRequest { Prompt = "Generate a story" };

        // Act
        var response = await facade.GenerateTextAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("Generated: Generate a story", response.Text);
        Assert.Equal("TestAITextProvider", response.ServiceInfo);
        Assert.Equal(1, testProvider.GenerateTextCallCount);
        Assert.Equal(request, testProvider.LastRequest);
    }

    [Fact]
    public async Task AITextFacade_CompleteTextAsync_ShouldRouteViaRegistry()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAIRegistry();
        services.AddNoOpAspectRuntime();
        services.AddResilienceExecutor();

        var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IServiceRegistry>();
        
        var testProvider = new TestAITextProvider();
        registry.Register<IAIText>(testProvider, ProviderCapabilities.Create("test-provider"));

        var facade = new TestAITextFacade(registry);
        var request = new AITextRequest { Prompt = "Complete this text" };

        // Act
        var response = await facade.CompleteTextAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("Completed: Complete this text", response.Text);
        Assert.Equal("TestAITextProvider", response.ServiceInfo);
        Assert.Equal(1, testProvider.CompleteTextCallCount);
        Assert.Equal(request, testProvider.LastRequest);
    }

    [Fact]
    public void AITextFacade_WithCapabilityTags_ShouldSupportModelFamilyAndContextWindow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAIRegistry();

        var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IServiceRegistry>();
        
        var testProvider = new TestAITextProvider();
        var capabilities = ProviderCapabilities.Create("gpt-provider")
            .WithTags("model-family:gpt", "context-window:4096");

        // Act
        registry.Register<IAIText>(testProvider, capabilities);

        // Assert
        var registrations = registry.GetRegistrations<IAIText>();
        var registration = registrations.First();
        Assert.True(registration.Capabilities.HasAnyTag("model-family:gpt"));
        Assert.True(registration.Capabilities.HasAnyTag("context-window:4096"));
    }

    [Fact]
    public async Task AITextFacade_WithMultipleProviders_ShouldUsePickOneStrategy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAIRegistry();
        services.AddNoOpAspectRuntime();
        services.AddResilienceExecutor();

        var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IServiceRegistry>();
        
        var provider1 = new TestAITextProvider();
        var provider2 = new TestAITextProvider();
        
        registry.Register<IAIText>(provider1, ProviderCapabilities.Create("provider1"));
        registry.Register<IAIText>(provider2, ProviderCapabilities.Create("provider2"));

        var facade = new TestAITextFacade(registry);
        var request = new AITextRequest { Prompt = "Test prompt" };

        // Act
        var response = await facade.GenerateTextAsync(request);

        // Assert - PickOne should select exactly one provider
        Assert.NotNull(response);
        int totalCalls = provider1.GenerateTextCallCount + provider2.GenerateTextCallCount;
        Assert.Equal(1, totalCalls); // Only one provider should be called with PickOne strategy
    }
}