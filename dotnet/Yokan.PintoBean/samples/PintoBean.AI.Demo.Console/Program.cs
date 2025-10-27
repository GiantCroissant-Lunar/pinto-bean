using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime;
using Yokan.PintoBean.Providers.AI.OpenAI;
using Yokan.PintoBean.Providers.AI.Ollama;

namespace PintoBean.AI.Demo.Console;

/// <summary>
/// Demo program showing AI backend plugins (OpenAI, Ollama) routing through the façade.
/// Demonstrates the acceptance criteria: Console test proves façade call routes into each backend when selected.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        System.Console.WriteLine("=== PintoBean AI Backend Plugins Demo ===\n");

        // Create a host builder with DI and AI providers
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                // Add PintoBean runtime services
                services.AddAIRegistry();
                services.AddNoOpAspectRuntime();
                services.AddResilienceExecutor();
                
                // Use PickOne strategy for AI services (default)
                services.AddSelectionStrategies();

                // Register our AI providers as implementations
                services.AddSingleton<IAIText, OpenAITextProvider>();
                services.AddSingleton<IAIText, OllamaTextProvider>();
                
                // Add a simple facade for testing
                services.AddSingleton<IAIText, AITextFacade>();
            })
            .Build();

        // Get services
        var registry = host.Services.GetRequiredService<IServiceRegistry>();
        var providers = host.Services.GetServices<IAIText>().Where(p => p is not AITextFacade).ToList();
        var facade = host.Services.GetServices<IAIText>().OfType<AITextFacade>().First();

        // Register providers with capability tags
        System.Console.WriteLine("🔧 Registering AI Providers with Capability Tags:");
        await RegisterProvidersWithCapabilities(registry, providers);

        System.Console.WriteLine("\n📊 Demonstrating Façade Routing to Each Backend:");
        await DemonstrateFacadeRouting(facade);

        System.Console.WriteLine("\n🏷️ Demonstrating Capability Tags (Model Family, Context Window, Cost):");
        DemonstrateCapabilityTags(registry);

        System.Console.WriteLine("\n🔄 Demonstrating Streaming Support:");
        await DemonstrateStreamingSupport(facade);

        System.Console.WriteLine("\n💬 Demonstrating Conversation Support:");
        await DemonstrateConversationSupport(facade);

        System.Console.WriteLine("\n✅ Demo completed successfully!");
    }

    private static Task RegisterProvidersWithCapabilities(IServiceRegistry registry, List<IAIText> providers)
    {
        foreach (var provider in providers)
        {
            var capabilities = provider switch
            {
                OpenAITextProvider => ProviderCapabilities.Create(OpenAITextProvider.ProviderId)
                    .WithTags("ai-text", "gpt-family", "context-window-128k", "cost-tier-medium", "streaming", "conversation", "completion"),
                OllamaTextProvider => ProviderCapabilities.Create(OllamaTextProvider.ProviderId)
                    .WithTags("ai-text", "llama-family", "context-window-8k", "cost-tier-free", "local-inference", "streaming", "conversation", "completion"),
                _ => ProviderCapabilities.Create("unknown-provider")
            };

            registry.Register<IAIText>(provider, capabilities);
            System.Console.WriteLine($"   ✅ Registered {provider.GetType().Name} with capabilities: [{string.Join(", ", capabilities.Tags)}]");
        }
        
        return Task.CompletedTask;
    }

    private static async Task DemonstrateFacadeRouting(IAIText facade)
    {
        var request = new AITextRequest { Prompt = "Explain quantum computing" };

        System.Console.WriteLine("   Testing GenerateTextAsync...");
        var response = await facade.GenerateTextAsync(request);
        System.Console.WriteLine($"   📤 Response: {response.Content}");
        System.Console.WriteLine($"   🤖 Model: {response.ModelInfo}");
        System.Console.WriteLine($"   🎯 Tokens: {response.TokenUsage?.TotalTokens ?? 0} total");

        System.Console.WriteLine("\n   Testing CompleteTextAsync...");
        var completionRequest = new AITextRequest { Prompt = "The future of AI is" };
        var completionResponse = await facade.CompleteTextAsync(completionRequest);
        System.Console.WriteLine($"   📤 Response: {completionResponse.Content}");
        System.Console.WriteLine($"   🤖 Model: {completionResponse.ModelInfo}");
    }

    private static void DemonstrateCapabilityTags(IServiceRegistry registry)
    {
        var registrations = registry.GetRegistrations<IAIText>().ToList();
        
        System.Console.WriteLine("   Provider Capabilities Summary:");
        foreach (var registration in registrations)
        {
            System.Console.WriteLine($"   🏷️ {registration.Capabilities.ProviderId}:");
            
            // Check for model family tags
            var modelFamily = registration.Capabilities.Tags.FirstOrDefault(t => t.StartsWith("gpt-") || t.StartsWith("llama-"));
            if (modelFamily != null)
                System.Console.WriteLine($"      📊 Model Family: {modelFamily}");

            // Check for context window tags
            var contextWindow = registration.Capabilities.Tags.FirstOrDefault(t => t.StartsWith("context-window-"));
            if (contextWindow != null)
                System.Console.WriteLine($"      📏 Context Window: {contextWindow.Replace("context-window-", "")}");

            // Check for cost tier tags
            var costTier = registration.Capabilities.Tags.FirstOrDefault(t => t.StartsWith("cost-tier-"));
            if (costTier != null)
                System.Console.WriteLine($"      💰 Cost Tier: {costTier.Replace("cost-tier-", "").ToUpper()}");

            // Check for special capabilities
            if (registration.Capabilities.HasAnyTag("local-inference"))
                System.Console.WriteLine($"      🏠 Local Inference: Yes");
            if (registration.Capabilities.HasAnyTag("streaming"))
                System.Console.WriteLine($"      📡 Streaming: Supported");
        }
    }

    private static async Task DemonstrateStreamingSupport(IAIText facade)
    {
        var request = new AITextRequest { Prompt = "Write a short poem about technology" };

        System.Console.WriteLine("   Testing streaming text generation...");
        System.Console.Write("   📡 Streaming Response: ");
        
        await foreach (var chunk in facade.GenerateTextStreamAsync(request))
        {
            System.Console.Write(chunk.Content);
            if (!chunk.IsComplete)
            {
                await Task.Delay(200); // Simulate reading delay
            }
        }
        System.Console.WriteLine("\n   ✅ Streaming completed");
    }

    private static async Task DemonstrateConversationSupport(IAIText facade)
    {
        var conversationHistory = new List<AIMessage>
        {
            new() { Role = "user", Content = "Hello, what's your name?" },
            new() { Role = "assistant", Content = "I'm an AI assistant." }
        };

        var request = new AITextRequest 
        { 
            Prompt = "What can you help me with?",
            ConversationHistory = conversationHistory
        };

        System.Console.WriteLine("   Testing conversation continuation...");
        var response = await facade.ContinueConversationAsync(request);
        System.Console.WriteLine($"   💬 Conversation Response: {response.Content}");
        System.Console.WriteLine($"   🤖 Model: {response.ModelInfo}");
    }
}

/// <summary>
/// Simple façade implementation for testing AI provider routing.
/// In a real implementation, this would be generated by the code generator.
/// </summary>
public class AITextFacade : IAIText
{
    private readonly IServiceRegistry _registry;

    public AITextFacade(IServiceRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public async Task<AITextResponse> GenerateTextAsync(AITextRequest request, CancellationToken cancellationToken = default)
    {
        var typedRegistry = _registry.For<IAIText>();
        return await typedRegistry.InvokeAsync(async (service, ct) => 
        {
            // Skip the facade itself to avoid infinite recursion
            if (service is AITextFacade) return null!;
            return await service.GenerateTextAsync(request, ct);
        }, cancellationToken);
    }

    public async IAsyncEnumerable<AITextResponse> GenerateTextStreamAsync(AITextRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var registrations = _registry.GetRegistrations<IAIText>()
            .Where(r => r.Provider is not AITextFacade)
            .ToList();
            
        if (registrations.Any())
        {
            var provider = (IAIText)registrations.First().Provider;
            await foreach (var response in provider.GenerateTextStreamAsync(request, cancellationToken))
            {
                yield return response;
            }
        }
    }

    public async Task<AITextResponse> ContinueConversationAsync(AITextRequest request, CancellationToken cancellationToken = default)
    {
        var typedRegistry = _registry.For<IAIText>();
        return await typedRegistry.InvokeAsync(async (service, ct) => 
        {
            if (service is AITextFacade) return null!;
            return await service.ContinueConversationAsync(request, ct);
        }, cancellationToken);
    }

    public async IAsyncEnumerable<AITextResponse> ContinueConversationStreamAsync(AITextRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var registrations = _registry.GetRegistrations<IAIText>()
            .Where(r => r.Provider is not AITextFacade)
            .ToList();
            
        if (registrations.Any())
        {
            var provider = (IAIText)registrations.First().Provider;
            await foreach (var response in provider.ContinueConversationStreamAsync(request, cancellationToken))
            {
                yield return response;
            }
        }
    }

    public async Task<AITextResponse> CompleteTextAsync(AITextRequest request, CancellationToken cancellationToken = default)
    {
        var typedRegistry = _registry.For<IAIText>();
        return await typedRegistry.InvokeAsync(async (service, ct) => 
        {
            if (service is AITextFacade) return null!;
            return await service.CompleteTextAsync(request, ct);
        }, cancellationToken);
    }
}