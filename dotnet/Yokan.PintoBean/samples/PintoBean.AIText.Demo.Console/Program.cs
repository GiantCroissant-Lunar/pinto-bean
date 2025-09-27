using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PintoBean.AIText.Demo.Console;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime;

Console.WriteLine("PintoBean AIText Streaming + Cancellation Demo");
Console.WriteLine("==============================================");
Console.WriteLine();

// Build services with streaming support
var services = new ServiceCollection();
services.AddAIRegistry();
services.AddNoOpAspectRuntime();
services.AddResilienceExecutor();

var serviceProvider = services.BuildServiceProvider();
var registry = serviceProvider.GetRequiredService<IServiceRegistry>();

// Register demo AI text providers
var provider1 = new DemoAITextProvider("GPT-Demo", streamDelayMs: 150);
var provider2 = new DemoAITextProvider("Claude-Demo", streamDelayMs: 200);

registry.Register<IAIText>(provider1, ProviderCapabilities.Create("gpt-provider")
    .WithPriority(Priority.High)
    .WithTags("model-family:gpt", "context-window:4096"));

registry.Register<IAIText>(provider2, ProviderCapabilities.Create("claude-provider")
    .WithPriority(Priority.Normal)
    .WithTags("model-family:claude", "context-window:8192"));

// Create the generated AIText façade
var aiService = new AITextService(
    registry, 
    serviceProvider.GetRequiredService<IResilienceExecutor>(),
    serviceProvider.GetRequiredService<IAspectRuntime>());

Console.WriteLine("Available demonstration modes:");
Console.WriteLine("1. Streaming text generation (press Ctrl+C to cancel)");
Console.WriteLine("2. Streaming conversation (press Ctrl+C to cancel)");
Console.WriteLine("3. Non-streaming operations");
Console.WriteLine();

Console.Write("Select mode (1-3): ");
var mode = Console.ReadLine();

switch (mode)
{
    case "1":
        await DemoStreamingGeneration(aiService);
        break;
    case "2":
        await DemoStreamingConversation(aiService);
        break;
    case "3":
        await DemoNonStreaming(aiService);
        break;
    default:
        Console.WriteLine("Invalid selection. Running streaming generation demo...");
        await DemoStreamingGeneration(aiService);
        break;
}

Console.WriteLine("\nDemo completed.");

static async Task DemoStreamingGeneration(AITextService aiService)
{
    Console.WriteLine("\n🚀 Streaming Text Generation Demo");
    Console.WriteLine("Press Ctrl+C to cancel gracefully...");
    Console.WriteLine();

    using var cts = new CancellationTokenSource();
    
    // Setup Ctrl+C handling
    Console.CancelKeyPress += (sender, e) =>
    {
        Console.WriteLine("\n🛑 Cancellation requested...");
        e.Cancel = true; // Don't terminate immediately
        cts.Cancel();
    };

    try
    {
        var request = new AITextRequest 
        { 
            Prompt = "Write a short story about a robot learning to dream" 
        };

        Console.WriteLine($"Prompt: {request.Prompt}");
        Console.WriteLine("Streaming response:");
        Console.WriteLine("---");

        await foreach (var response in aiService.GenerateTextStreamAsync(request, cts.Token))
        {
            Console.Write($"{response.Content} ");
            
            if (response.IsComplete)
            {
                Console.WriteLine("\n---");
                Console.WriteLine($"✅ Generation complete from {response.ModelInfo}");
            }
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("\n❌ Operation was cancelled by user");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n❌ Error: {ex.Message}");
    }
}

static async Task DemoStreamingConversation(AITextService aiService)
{
    Console.WriteLine("\n💬 Streaming Conversation Demo");
    Console.WriteLine("Press Ctrl+C to cancel gracefully...");
    Console.WriteLine();

    using var cts = new CancellationTokenSource();
    
    Console.CancelKeyPress += (sender, e) =>
    {
        Console.WriteLine("\n🛑 Cancellation requested...");
        e.Cancel = true;
        cts.Cancel();
    };

    try
    {
        var request = new AITextRequest
        {
            Prompt = "I'm interested in learning about artificial intelligence",
            SystemMessage = "This is a continuation of our previous discussion about technology."
        };

        Console.WriteLine($"System context: {request.SystemMessage}");
        Console.WriteLine($"Your message: {request.Prompt}");
        Console.WriteLine("AI response stream:");
        Console.WriteLine("---");

        await foreach (var response in aiService.ContinueConversationStreamAsync(request, cts.Token))
        {
            Console.WriteLine(response.Content);
            
            if (response.IsComplete)
            {
                Console.WriteLine("---");
                Console.WriteLine($"✅ Conversation response complete from {response.ModelInfo}");
            }
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("\n❌ Conversation was cancelled by user");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n❌ Error: {ex.Message}");
    }
}

static async Task DemoNonStreaming(AITextService aiService)
{
    Console.WriteLine("\n📝 Non-Streaming Operations Demo");
    Console.WriteLine();

    try
    {
        // Test regular text generation
        var request1 = new AITextRequest { Prompt = "Hello, world!" };
        var response1 = await aiService.GenerateTextAsync(request1, CancellationToken.None);
        Console.WriteLine($"Generate Text: {response1.Content}");
        Console.WriteLine($"Provider: {response1.ModelInfo}");
        Console.WriteLine();

        // Test text completion
        var request2 = new AITextRequest { Prompt = "The quick brown fox" };
        var response2 = await aiService.CompleteTextAsync(request2, CancellationToken.None);
        Console.WriteLine($"Complete Text: {response2.Content}");
        Console.WriteLine($"Provider: {response2.ModelInfo}");
        Console.WriteLine();

        // Test conversation
        var request3 = new AITextRequest 
        { 
            Prompt = "What's the weather like?",
            SystemMessage = "We were talking about outdoor activities."
        };
        var response3 = await aiService.ContinueConversationAsync(request3, CancellationToken.None);
        Console.WriteLine($"Continue Conversation: {response3.Content}");
        Console.WriteLine($"Provider: {response3.ModelInfo}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error: {ex.Message}");
    }
}