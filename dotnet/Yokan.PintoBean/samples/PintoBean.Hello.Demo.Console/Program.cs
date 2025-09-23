using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime;
using Yokan.PintoBean.Providers.Stub;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;

// Console demo application for Yokan PintoBean service platform
Console.WriteLine("PintoBean Hello Demo Console");
Console.WriteLine("============================");
Console.WriteLine();

// Display version information
Console.WriteLine($"Abstractions Version: {PintoBeanAbstractions.Version}");
Console.WriteLine($"Runtime Version: {PintoBeanRuntime.Version}");
Console.WriteLine($"Providers.Stub Version: {PintoBeanProvidersStub.Version}");
Console.WriteLine();

// Demonstrate the service registry functionality
Console.WriteLine("🔧 Setting up Dependency Injection with Service Registry...");
var services = new ServiceCollection();

// Register the service registry with pre-configured providers
services.AddServiceRegistry(registry =>
{
    Console.WriteLine("📝 Registering providers during DI setup:");

    // Register a primary provider with high priority
    var primaryProvider = new DemoHelloService("PrimaryGreetingService");
    var primaryCapabilities = ProviderCapabilities.Create("primary-hello")
        .WithPriority(Priority.High)
        .WithPlatform(Platform.DotNet)
        .WithTags("primary", "greeting", "production");

    registry.Register<IHelloService>(primaryProvider, primaryCapabilities);
    Console.WriteLine($"  ✅ Registered: {primaryCapabilities.ProviderId} (Priority: {primaryCapabilities.Priority})");

    // Register a fallback provider with lower priority
    var fallbackProvider = new DemoHelloService("FallbackGreetingService");
    var fallbackCapabilities = ProviderCapabilities.Create("fallback-hello")
        .WithPriority(Priority.Normal)
        .WithPlatform(Platform.DotNet)
        .WithTags("fallback", "greeting", "backup");

    registry.Register<IHelloService>(fallbackProvider, fallbackCapabilities);
    Console.WriteLine($"  ✅ Registered: {fallbackCapabilities.ProviderId} (Priority: {fallbackCapabilities.Priority})");
});

var serviceProvider = services.BuildServiceProvider();

Console.WriteLine();
Console.WriteLine("🚀 Demonstrating Registry Runtime Features:");
Console.WriteLine();

// Get the service registry from DI
var registry = serviceProvider.GetRequiredService<IServiceRegistry>();

// Subscribe to provider changes
registry.ProviderChanged += (sender, e) =>
{
    Console.WriteLine($"📡 ProviderChanged Event: {e.ChangeType} - {e.Registration.Capabilities.ProviderId}");
};

// Display current registrations
Console.WriteLine("📋 Current Provider Registrations:");
var registrations = registry.GetRegistrations<IHelloService>();
foreach (var reg in registrations)
{
    Console.WriteLine($"  • {reg.Capabilities.ProviderId}:");
    Console.WriteLine($"    - Priority: {reg.Capabilities.Priority}");
    Console.WriteLine($"    - Platform: {reg.Capabilities.Platform}");
    Console.WriteLine($"    - Tags: [{string.Join(", ", reg.Capabilities.Tags)}]");
    Console.WriteLine($"    - Active: {reg.IsActive}");
}

Console.WriteLine();
Console.WriteLine("🎯 Testing Provider Selection (For<IHelloService>()):");

// Get typed registry and demonstrate provider selection
var typedRegistry = registry.For<IHelloService>();
var request = new HelloRequest
{
    Name = "World",
    Language = "en",
    Context = "demo"
};

// Call through the registry - should select highest priority provider
var response = await typedRegistry.InvokeAsync((service, ct) => service.SayHelloAsync(request, ct));

Console.WriteLine($"✨ Response: {response.Message}");
Console.WriteLine($"   Service: {response.ServiceInfo}");
Console.WriteLine($"   Language: {response.Language}");
Console.WriteLine($"   Timestamp: {response.Timestamp:HH:mm:ss.fff}");

Console.WriteLine();
Console.WriteLine("🔄 Testing Dynamic Provider Registration:");

// Register a new high-priority provider at runtime
var runtimeProvider = new DemoHelloService("RuntimeGreetingService");
var runtimeCapabilities = ProviderCapabilities.Create("runtime-hello")
    .WithPriority(Priority.Critical)
    .WithPlatform(Platform.DotNet)
    .WithTags("runtime", "dynamic", "critical")
    .AddMetadata("version", "1.0.0")
    .AddMetadata("registered-at", DateTime.UtcNow);

var runtimeRegistration = registry.Register<IHelloService>(runtimeProvider, runtimeCapabilities);
Console.WriteLine($"➕ Added runtime provider: {runtimeCapabilities.ProviderId}");

// Call again - should now select the new critical priority provider
var response2 = await typedRegistry.InvokeAsync((service, ct) => service.SayGoodbyeAsync(request, ct));

Console.WriteLine($"✨ Response: {response2.Message}");
Console.WriteLine($"   Service: {response2.ServiceInfo} (should be RuntimeGreetingService)");

Console.WriteLine();
Console.WriteLine("🗑️  Testing Provider Unregistration:");

// Remove the runtime provider
registry.Unregister(runtimeRegistration);
Console.WriteLine($"➖ Removed runtime provider: {runtimeCapabilities.ProviderId}");

// Call again - should fall back to the primary provider
var response3 = await typedRegistry.InvokeAsync((service, ct) => service.SayHelloAsync(request, ct));

Console.WriteLine($"✨ Response: {response3.Message}");
Console.WriteLine($"   Service: {response3.ServiceInfo} (should be back to PrimaryGreetingService)");

Console.WriteLine();
Console.WriteLine("📊 Final Registry State:");
var finalRegistrations = registry.GetRegistrations<IHelloService>();
Console.WriteLine($"Active Providers: {finalRegistrations.Count()}");
foreach (var reg in finalRegistrations.OrderByDescending(r => (int)r.Capabilities.Priority))
{
    Console.WriteLine($"  • {reg.Capabilities.ProviderId} (Priority: {reg.Capabilities.Priority})");
}

Console.WriteLine();
Console.WriteLine("✅ Demo completed successfully!");
Console.WriteLine("   The PintoBean runtime registry is working as expected:");
Console.WriteLine("   - ✅ Provider registration and unregistration");
Console.WriteLine("   - ✅ Priority-based provider selection");
Console.WriteLine("   - ✅ Typed For<IHelloService>() resolution");
Console.WriteLine("   - ✅ ProviderChanged events for cache invalidation");
Console.WriteLine("   - ✅ Dependency injection integration");

// Demo implementation of IHelloService
public class DemoHelloService : IHelloService
{
    public string Name { get; }

    public DemoHelloService(string name)
    {
        Name = name;
    }

    public Task<HelloResponse> SayHelloAsync(HelloRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HelloResponse
        {
            Message = $"Hello, {request.Name}! Greetings from {Name}.",
            ServiceInfo = Name,
            Language = request.Language ?? "en"
        });
    }

    public Task<HelloResponse> SayGoodbyeAsync(HelloRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HelloResponse
        {
            Message = $"Farewell, {request.Name}! Until next time, from {Name}.",
            ServiceInfo = Name,
            Language = request.Language ?? "en"
        });
    }
}
