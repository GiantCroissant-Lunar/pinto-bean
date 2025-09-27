// Tests for IntelligentRouter provider QoS-based selection

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime;
using Yokan.PintoBean.Runtime.AI;

namespace Yokan.PintoBean.Runtime.Tests.AI;

/// <summary>
/// Tests for IntelligentRouter provider functionality as specified in P7-03.
/// Validates QoS-based selection (cost/latency) and constraint handling.
/// </summary>
public class IntelligentRouterTests
{
    /// <summary>
    /// Mock service registry for testing.
    /// </summary>
    private class MockServiceRegistry : IServiceRegistry
    {
        private readonly List<IProviderRegistration> _registrations = new();

        public event ProviderChangedEventHandler? ProviderChanged
        {
            add { /* Not used in tests */ }
            remove { /* Not used in tests */ }
        }

        public IProviderRegistration Register(Type serviceType, object provider, ProviderCapabilities capabilities)
        {
            var registration = new MockProviderRegistration(serviceType, provider, capabilities);
            _registrations.Add(registration);
            return registration;
        }

        public IProviderRegistration Register<TService>(TService provider, ProviderCapabilities capabilities) where TService : class
        {
            return Register(typeof(TService), provider, capabilities);
        }

        public bool Unregister(IProviderRegistration registration)
        {
            return _registrations.Remove(registration);
        }

        public IEnumerable<IProviderRegistration> GetRegistrations(Type serviceType)
        {
            return _registrations.Where(r => r.ServiceType == serviceType);
        }

        public IEnumerable<IProviderRegistration> GetRegistrations<TService>()
        {
            return GetRegistrations(typeof(TService));
        }

        public bool HasRegistrations(Type serviceType) => GetRegistrations(serviceType).Any();
        public bool HasRegistrations<TService>() => HasRegistrations(typeof(TService));
        public int ClearRegistrations(Type serviceType)
        {
            var toRemove = GetRegistrations(serviceType).ToList();
            foreach (var reg in toRemove)
                _registrations.Remove(reg);
            return toRemove.Count;
        }
        public int ClearRegistrations<TService>() => ClearRegistrations(typeof(TService));
        public IServiceRegistry<TService> For<TService>() where TService : class => throw new NotImplementedException();
    }

    /// <summary>
    /// Mock provider registration for testing.
    /// </summary>
    private class MockProviderRegistration : IProviderRegistration
    {
        public Type ServiceType { get; }
        public object Provider { get; }
        public ProviderCapabilities Capabilities { get; }
        public bool IsActive { get; set; } = true;

        public MockProviderRegistration(Type serviceType, object provider, ProviderCapabilities capabilities)
        {
            ServiceType = serviceType;
            Provider = provider;
            Capabilities = capabilities;
        }
    }

    /// <summary>
    /// Mock AI text provider for testing with controllable responses.
    /// </summary>
    private class MockAITextProvider : IAIText
    {
        public string ProviderId { get; }
        public int CallCount { get; private set; }
        public AITextRequest? LastRequest { get; private set; }

        public MockAITextProvider(string providerId)
        {
            ProviderId = providerId;
        }

        public Task<AITextResponse> GenerateTextAsync(AITextRequest request, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastRequest = request;
            return Task.FromResult(new AITextResponse
            {
                Content = $"Response from {ProviderId}: {request.Prompt}",
                ModelInfo = ProviderId
            });
        }

        public async IAsyncEnumerable<AITextResponse> GenerateTextStreamAsync(AITextRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastRequest = request;
            yield return new AITextResponse
            {
                Content = $"Stream from {ProviderId}: {request.Prompt}",
                ModelInfo = ProviderId,
                IsComplete = true
            };
            await Task.Yield(); // Make it actually async
        }

        public Task<AITextResponse> ContinueConversationAsync(AITextRequest request, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastRequest = request;
            return Task.FromResult(new AITextResponse
            {
                Content = $"Conversation from {ProviderId}: {request.Prompt}",
                ModelInfo = ProviderId
            });
        }

        public async IAsyncEnumerable<AITextResponse> ContinueConversationStreamAsync(AITextRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastRequest = request;
            yield return new AITextResponse
            {
                Content = $"ConversationStream from {ProviderId}: {request.Prompt}",
                ModelInfo = ProviderId,
                IsComplete = true
            };
            await Task.Yield(); // Make it actually async
        }

        public Task<AITextResponse> CompleteTextAsync(AITextRequest request, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastRequest = request;
            return Task.FromResult(new AITextResponse
            {
                Content = $"Complete from {ProviderId}: {request.Prompt}",
                ModelInfo = ProviderId
            });
        }
    }

    [Fact]
    public async Task IntelligentRouter_ShouldChooseCheapestProvider_WhenBudgetSpecified()
    {
        // Arrange
        var registry = new MockServiceRegistry();
        var expensiveProvider = new MockAITextProvider("ExpensiveProvider");
        var cheapProvider = new MockAITextProvider("CheapProvider");
        var midPriceProvider = new MockAITextProvider("MidPriceProvider");

        // Register providers with different costs
        registry.Register<IAIText>(expensiveProvider, ProviderCapabilities.Create("expensive")
            .AddMetadata("costPer1KTokens", 0.50m));
        registry.Register<IAIText>(cheapProvider, ProviderCapabilities.Create("cheap")
            .AddMetadata("costPer1KTokens", 0.10m));
        registry.Register<IAIText>(midPriceProvider, ProviderCapabilities.Create("mid")
            .AddMetadata("costPer1KTokens", 0.25m));

        var options = RouterOptions.ForCostOptimization(0.60m);
        var router = new IntelligentRouter(registry, options);

        var request = new AITextRequest { Prompt = "Test prompt for cost optimization" };

        // Act
        var response = await router.GenerateTextAsync(request);

        // Assert
        Assert.Equal("CheapProvider", response.ModelInfo);
        Assert.Equal(1, cheapProvider.CallCount);
        Assert.Equal(0, expensiveProvider.CallCount);
        Assert.Equal(0, midPriceProvider.CallCount);
    }

    [Fact]
    public async Task IntelligentRouter_ShouldChooseLowestLatencyProvider_WhenLatencyTargetSpecified()
    {
        // Arrange
        var registry = new MockServiceRegistry();
        var slowProvider = new MockAITextProvider("SlowProvider");
        var fastProvider = new MockAITextProvider("FastProvider");
        var mediumProvider = new MockAITextProvider("MediumProvider");

        // Register providers with different latencies
        registry.Register<IAIText>(slowProvider, ProviderCapabilities.Create("slow")
            .AddMetadata("avgLatencyMs", 500));
        registry.Register<IAIText>(fastProvider, ProviderCapabilities.Create("fast")
            .AddMetadata("avgLatencyMs", 100));
        registry.Register<IAIText>(mediumProvider, ProviderCapabilities.Create("medium")
            .AddMetadata("avgLatencyMs", 250));

        var options = RouterOptions.ForLowLatency(300);
        var router = new IntelligentRouter(registry, options);

        var request = new AITextRequest { Prompt = "Test prompt for latency optimization" };

        // Act
        var response = await router.GenerateTextAsync(request);

        // Assert
        Assert.Equal("FastProvider", response.ModelInfo);
        Assert.Equal(1, fastProvider.CallCount);
        Assert.Equal(0, slowProvider.CallCount);
        Assert.Equal(0, mediumProvider.CallCount);
    }

    [Fact]
    public async Task IntelligentRouter_ShouldHonorAllowExternalFalse_ExcludingExternalProviders()
    {
        // Arrange
        var registry = new MockServiceRegistry();
        var internalProvider = new MockAITextProvider("InternalProvider");
        var externalProvider = new MockAITextProvider("ExternalProvider");
        var thirdPartyProvider = new MockAITextProvider("ThirdPartyProvider");

        // Register providers with different external status
        registry.Register<IAIText>(internalProvider, ProviderCapabilities.Create("internal")
            .AddTags("internal", "local"));
        registry.Register<IAIText>(externalProvider, ProviderCapabilities.Create("external")
            .AddTags("external", "cloud"));
        registry.Register<IAIText>(thirdPartyProvider, ProviderCapabilities.Create("third-party")
            .AddTags("third-party", "api"));

        var options = RouterOptions.InternalOnly();
        var router = new IntelligentRouter(registry, options);

        var request = new AITextRequest { Prompt = "Test prompt for internal-only routing" };

        // Act
        var response = await router.GenerateTextAsync(request);

        // Assert
        Assert.Equal("InternalProvider", response.ModelInfo);
        Assert.Equal(1, internalProvider.CallCount);
        Assert.Equal(0, externalProvider.CallCount);
        Assert.Equal(0, thirdPartyProvider.CallCount);
    }

    [Fact]
    public async Task IntelligentRouter_ShouldAllowExternalProviders_WhenAllowExternalTrue()
    {
        // Arrange
        var registry = new MockServiceRegistry();
        var externalProvider = new MockAITextProvider("ExternalProvider");

        registry.Register<IAIText>(externalProvider, ProviderCapabilities.Create("external")
            .AddTags("external"));

        var options = RouterOptions.Default.WithExternalPolicy(true);
        var router = new IntelligentRouter(registry, options);

        var request = new AITextRequest { Prompt = "Test prompt for external allowed" };

        // Act
        var response = await router.GenerateTextAsync(request);

        // Assert
        Assert.Equal("ExternalProvider", response.ModelInfo);
        Assert.Equal(1, externalProvider.CallCount);
    }

    [Fact]
    public async Task IntelligentRouter_ShouldRespectRegionPreference()
    {
        // Arrange
        var registry = new MockServiceRegistry();
        var usProvider = new MockAITextProvider("USProvider");
        var euProvider = new MockAITextProvider("EUProvider");

        registry.Register<IAIText>(usProvider, ProviderCapabilities.Create("us")
            .AddMetadata("region", "us-east-1"));
        registry.Register<IAIText>(euProvider, ProviderCapabilities.Create("eu")
            .AddMetadata("region", "eu-west-1"));

        var options = RouterOptions.Default.WithRegion("eu-west-1");
        var router = new IntelligentRouter(registry, options);

        var request = new AITextRequest { Prompt = "Test prompt for region preference" };

        // Act
        var response = await router.GenerateTextAsync(request);

        // Assert
        Assert.Equal("EUProvider", response.ModelInfo);
        Assert.Equal(1, euProvider.CallCount);
        Assert.Equal(0, usProvider.CallCount);
    }

    [Fact]
    public async Task IntelligentRouter_ShouldThrowException_WhenNoProvidersRegistered()
    {
        // Arrange
        var registry = new MockServiceRegistry();
        var router = new IntelligentRouter(registry);

        var request = new AITextRequest { Prompt = "Test prompt" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => router.GenerateTextAsync(request));
        
        Assert.Contains("No AI text providers are registered", exception.Message);
    }

    [Fact]
    public async Task IntelligentRouter_ShouldThrowException_WhenNoProvidersMatchConstraints()
    {
        // Arrange
        var registry = new MockServiceRegistry();
        var expensiveProvider = new MockAITextProvider("ExpensiveProvider");

        registry.Register<IAIText>(expensiveProvider, ProviderCapabilities.Create("expensive")
            .AddMetadata("costPer1KTokens", 1.00m));

        var options = RouterOptions.ForCostOptimization(0.50m);
        var router = new IntelligentRouter(registry, options);

        var request = new AITextRequest { Prompt = "Test prompt" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => router.GenerateTextAsync(request));
        
        Assert.Contains("No AI text providers match the specified router constraints", exception.Message);
    }

    [Fact]
    public async Task IntelligentRouter_ShouldFallbackToPriority_WhenNoQoSMetadata()
    {
        // Arrange
        var registry = new MockServiceRegistry();
        var lowPriorityProvider = new MockAITextProvider("LowPriorityProvider");
        var highPriorityProvider = new MockAITextProvider("HighPriorityProvider");

        registry.Register<IAIText>(lowPriorityProvider, ProviderCapabilities.Create("low")
            .WithPriority(Priority.Low));
        registry.Register<IAIText>(highPriorityProvider, ProviderCapabilities.Create("high")
            .WithPriority(Priority.High));

        var router = new IntelligentRouter(registry);

        var request = new AITextRequest { Prompt = "Test prompt for priority fallback" };

        // Act
        var response = await router.GenerateTextAsync(request);

        // Assert
        Assert.Equal("HighPriorityProvider", response.ModelInfo);
        Assert.Equal(1, highPriorityProvider.CallCount);
        Assert.Equal(0, lowPriorityProvider.CallCount);
    }

    [Fact]
    public async Task IntelligentRouter_ShouldWorkWithAllMethods()
    {
        // Arrange
        var registry = new MockServiceRegistry();
        var provider = new MockAITextProvider("TestProvider");

        registry.Register<IAIText>(provider, ProviderCapabilities.Create("test"));

        var router = new IntelligentRouter(registry);
        var request = new AITextRequest { Prompt = "Test prompt" };

        // Act & Assert for GenerateTextAsync
        var response1 = await router.GenerateTextAsync(request);
        Assert.Equal("TestProvider", response1.ModelInfo);

        // Act & Assert for GenerateTextStreamAsync
        var streamResults = new List<AITextResponse>();
        await foreach (var item in router.GenerateTextStreamAsync(request))
        {
            streamResults.Add(item);
        }
        Assert.Single(streamResults);
        Assert.Equal("TestProvider", streamResults[0].ModelInfo);

        // Act & Assert for ContinueConversationAsync
        var response3 = await router.ContinueConversationAsync(request);
        Assert.Equal("TestProvider", response3.ModelInfo);

        // Act & Assert for ContinueConversationStreamAsync
        var conversationStreamResults = new List<AITextResponse>();
        await foreach (var item in router.ContinueConversationStreamAsync(request))
        {
            conversationStreamResults.Add(item);
        }
        Assert.Single(conversationStreamResults);
        Assert.Equal("TestProvider", conversationStreamResults[0].ModelInfo);

        // Act & Assert for CompleteTextAsync
        var response5 = await router.CompleteTextAsync(request);
        Assert.Equal("TestProvider", response5.ModelInfo);

        // Verify all methods were called
        Assert.Equal(5, provider.CallCount);
    }

    [Fact]
    public async Task IntelligentRouter_ShouldPrioritizeCostOverLatency_WhenBudgetSpecified()
    {
        // Arrange
        var registry = new MockServiceRegistry();
        var fastExpensiveProvider = new MockAITextProvider("FastExpensiveProvider");
        var slowCheapProvider = new MockAITextProvider("SlowCheapProvider");

        registry.Register<IAIText>(fastExpensiveProvider, ProviderCapabilities.Create("fast-expensive")
            .AddMetadata("costPer1KTokens", 0.50m)
            .AddMetadata("avgLatencyMs", 100));
        registry.Register<IAIText>(slowCheapProvider, ProviderCapabilities.Create("slow-cheap")
            .AddMetadata("costPer1KTokens", 0.10m)
            .AddMetadata("avgLatencyMs", 500));

        // Set both budget and latency preferences - budget should win
        var options = RouterOptions.ForCostOptimization(0.60m).WithLatencyTarget(200);
        var router = new IntelligentRouter(registry, options);

        var request = new AITextRequest { Prompt = "Test prompt for cost priority" };

        // Act
        var response = await router.GenerateTextAsync(request);

        // Assert - Should choose cheap provider despite being slower
        Assert.Equal("SlowCheapProvider", response.ModelInfo);
        Assert.Equal(1, slowCheapProvider.CallCount);
        Assert.Equal(0, fastExpensiveProvider.CallCount);
    }
}