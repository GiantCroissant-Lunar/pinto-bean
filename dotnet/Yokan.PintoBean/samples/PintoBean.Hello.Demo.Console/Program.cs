using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime;
using Yokan.PintoBean.Providers.Stub;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

// Console demo application for Yokan PintoBean service platform
Console.WriteLine("PintoBean Hello Demo Console with OpenTelemetry");
Console.WriteLine("==============================================");
Console.WriteLine();

// Display version information
Console.WriteLine($"Abstractions Version: {PintoBeanAbstractions.Version}");
Console.WriteLine($"Runtime Version: {PintoBeanRuntime.Version}");
Console.WriteLine($"Providers.Stub Version: {PintoBeanProvidersStub.Version}");
Console.WriteLine();

Console.WriteLine("Choose demonstration mode:");
Console.WriteLine("1. NoOp AspectRuntime + PassThrough ResilienceExecutor (baseline)");
Console.WriteLine("2. OpenTelemetry AspectRuntime + Polly ResilienceExecutor (with tracing and resilience)");
Console.Write("Enter choice (1 or 2, or press Enter for OpenTelemetry + Polly): ");

var choice = Console.ReadLine();
var useOpenTelemetry = string.IsNullOrWhiteSpace(choice) || choice != "1";
var usePollyResilience = useOpenTelemetry; // Same choice controls both

Console.WriteLine();
Console.WriteLine($"🔧 Setting up Dependency Injection with {(useOpenTelemetry ? "OpenTelemetry AspectRuntime" : "NoOp AspectRuntime")} and {(usePollyResilience ? "Polly ResilienceExecutor" : "PassThrough ResilienceExecutor")}...");

var services = new ServiceCollection();

// Configure OpenTelemetry if chosen
if (useOpenTelemetry)
{
    services.AddOpenTelemetry()
        .WithTracing(builder => builder
            .AddSource("PintoBean.Demo")
            .AddConsoleExporter())
        .WithMetrics(builder => builder
            .AddMeter("PintoBean.Demo")
            .AddConsoleExporter());
    
    // Register OpenTelemetry AspectRuntime
    services.AddOpenTelemetryAspectRuntime("PintoBean.Demo", "PintoBean.Demo");
}
else
{
    // Register NoOp AspectRuntime
    services.AddNoOpAspectRuntime();
}

// Configure resilience executor
if (usePollyResilience)
{
    services.AddPollyResilience(options =>
    {
        options.DefaultTimeoutSeconds = 5.0; // 5 second timeout for demo
        options.MaxRetryAttempts = 3;
        options.BaseRetryDelayMilliseconds = 500.0; // Start with 500ms delays
        options.EnableCircuitBreaker = false; // Keep it simple for demo
    });
}
else
{
    // Register default pass-through resilience executor
    services.AddResilienceExecutor();
}

// Register required services
services.AddServiceRegistry(registry =>
{
    Console.WriteLine("📝 Registering providers during DI setup:");

    // Register a primary provider with high priority
    var primaryProvider = new DemoHelloService("PrimaryGreetingService");
    var primaryCapabilities = ProviderCapabilities.Create("primary-hello")
        .WithPriority(Priority.High)
        .WithPlatform(Platform.Any)
        .WithTags("primary", "greeting", "production");

    registry.Register<IHelloService>(primaryProvider, primaryCapabilities);
    Console.WriteLine($"  ✅ Registered: {primaryCapabilities.ProviderId} (Priority: {primaryCapabilities.Priority})");

    // Register a fallback provider with lower priority
    var fallbackProvider = new DemoHelloService("FallbackGreetingService");
    var fallbackCapabilities = ProviderCapabilities.Create("fallback-hello")
        .WithPriority(Priority.Normal)
        .WithPlatform(Platform.Any)
        .WithTags("fallback", "greeting", "backup");

    registry.Register<IHelloService>(fallbackProvider, fallbackCapabilities);
    Console.WriteLine($"  ✅ Registered: {fallbackCapabilities.ProviderId} (Priority: {fallbackCapabilities.Priority})");
});

// Add required runtime services
services.AddSelectionStrategies();

var serviceProvider = services.BuildServiceProvider();

Console.WriteLine();
Console.WriteLine("🚀 Demonstrating Registry Runtime Features with AspectRuntime:");
Console.WriteLine();

// Get the service registry and aspect runtime from DI
var registry = serviceProvider.GetRequiredService<IServiceRegistry>();
var aspectRuntime = serviceProvider.GetRequiredService<IAspectRuntime>();
var resilienceExecutor = serviceProvider.GetRequiredService<IResilienceExecutor>();

Console.WriteLine($"AspectRuntime Type: {aspectRuntime.GetType().Name}");
Console.WriteLine($"ResilienceExecutor Type: {resilienceExecutor.GetType().Name}");
Console.WriteLine();

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
Console.WriteLine("🎯 Testing Provider Selection with AspectRuntime tracking:");

// Demonstrate custom operation tracking
using (var operation = aspectRuntime.StartOperation("demo-workflow", new Dictionary<string, object>
{
    ["demo.type"] = "service-registry",
    ["demo.mode"] = useOpenTelemetry ? "otel" : "noop"
}))
{
    // Record a custom metric
    aspectRuntime.RecordMetric("demo.operation.start", 1.0, ("operation", "demo-workflow"));

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

    // Record success metric
    aspectRuntime.RecordMetric("demo.operation.success", 1.0, ("operation", "hello-call"));

    Console.WriteLine();
    Console.WriteLine("🔄 Testing Dynamic Provider Registration:");

    // Register a new high-priority provider at runtime
    var runtimeProvider = new DemoHelloService("RuntimeGreetingService");
    var runtimeCapabilities = ProviderCapabilities.Create("runtime-hello")
        .WithPriority(Priority.Critical)
        .WithPlatform(Platform.Any)
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

    // Record completion metric
    aspectRuntime.RecordMetric("demo.operation.complete", 1.0, ("operation", "demo-workflow"));
}

Console.WriteLine();
Console.WriteLine("🛡️  Testing Resilience Patterns:");
Console.WriteLine($"   ResilienceExecutor: {resilienceExecutor.GetType().Name}");

// Get typed registry for resilience testing
var resilienceTypedRegistry = registry.For<IHelloService>();
var resilienceRequest = new HelloRequest
{
    Name = "World",
    Language = "en",
    Context = "resilience-demo"
};

if (usePollyResilience)
{
    Console.WriteLine("   Demonstrating timeout and retry with Polly policies...");
    
    // Add a provider that can simulate failures
    var unreliableProvider = new UnreliableHelloService("UnreliableService");
    var unreliableCapabilities = ProviderCapabilities.Create("unreliable-hello")
        .WithPriority(Priority.Critical)
        .WithPlatform(Platform.Any)
        .WithTags("demo", "unreliable");
    
    var unreliableRegistration = registry.Register<IHelloService>(unreliableProvider, unreliableCapabilities);
    
    Console.WriteLine("   ➕ Registered unreliable service provider");
    
    try 
    {
        // First call should succeed (reset failure count)
        unreliableProvider.ResetFailures();
        var successResponse = await resilienceTypedRegistry.InvokeAsync((service, ct) => 
            resilienceExecutor.ExecuteAsync(async (innerCt) => 
                await service.SayHelloAsync(resilienceRequest, innerCt), ct));
        
        Console.WriteLine($"   ✅ Success on attempt: {successResponse.Message}");
        
        // Second call should trigger retries
        unreliableProvider.SetFailureMode(2); // Fail first 2 attempts, succeed on 3rd
        Console.WriteLine("   ⚠️  Simulating transient failures (will retry)...");
        
        var start = DateTime.UtcNow;
        var retryResponse = await resilienceTypedRegistry.InvokeAsync((service, ct) => 
            resilienceExecutor.ExecuteAsync(async (innerCt) => 
                await service.SayHelloAsync(resilienceRequest, innerCt), ct));
        var elapsed = DateTime.UtcNow - start;
        
        Console.WriteLine($"   ✅ Success after retries: {retryResponse.Message}");
        Console.WriteLine($"   ⏱️  Total time with retries: {elapsed.TotalMilliseconds:F0}ms");
        Console.WriteLine($"   📊 Call attempts: {unreliableProvider.CallAttempts}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"   ❌ Failed after all retries: {ex.GetType().Name}: {ex.Message}");
    }
    finally
    {
        registry.Unregister(unreliableRegistration);
        Console.WriteLine("   ➖ Removed unreliable service provider");
    }
}
else
{
    Console.WriteLine("   Using PassThrough executor - no resilience patterns applied");
    
    // Show direct execution timing for comparison
    var start = DateTime.UtcNow;
    var directResponse = await resilienceTypedRegistry.InvokeAsync((service, ct) => 
        resilienceExecutor.ExecuteAsync(async (innerCt) => 
            await service.SayHelloAsync(resilienceRequest, innerCt), ct));
    var elapsed = DateTime.UtcNow - start;
    
    Console.WriteLine($"   ✅ Direct execution: {directResponse.Message}");
    Console.WriteLine($"   ⏱️  Execution time: {elapsed.TotalMilliseconds:F0}ms");
}

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
Console.WriteLine("   The PintoBean runtime with AspectRuntime is working as expected:");
Console.WriteLine("   - ✅ Provider registration and unregistration");
Console.WriteLine("   - ✅ Priority-based provider selection");
Console.WriteLine("   - ✅ Typed For<IHelloService>() resolution");
Console.WriteLine("   - ✅ ProviderChanged events for cache invalidation");
Console.WriteLine("   - ✅ Dependency injection integration");
Console.WriteLine($"   - ✅ {(useOpenTelemetry ? "OpenTelemetry AspectRuntime" : "NoOp AspectRuntime")} telemetry");
Console.WriteLine($"   - ✅ {(usePollyResilience ? "Polly ResilienceExecutor with retry/timeout" : "PassThrough ResilienceExecutor")} patterns");

if (useOpenTelemetry)
{
    Console.WriteLine();
    Console.WriteLine("📈 OpenTelemetry telemetry data should appear above!");
    Console.WriteLine("    Look for Activity traces and Histogram metrics.");
}

if (usePollyResilience)
{
    Console.WriteLine();
    Console.WriteLine("🛡️  Polly resilience patterns demonstrated!");
    Console.WriteLine("    Look for retry attempts and timing information above.");
}

// Properly dispose of the service provider to clean up OpenTelemetry resources
serviceProvider.Dispose();

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

// Unreliable demo service for demonstrating resilience patterns
public class UnreliableHelloService : IHelloService
{
    public string Name { get; }
    public int CallAttempts { get; private set; }
    private int _failuresRemaining;

    public UnreliableHelloService(string name)
    {
        Name = name;
    }

    public void ResetFailures()
    {
        CallAttempts = 0;
        _failuresRemaining = 0;
    }

    public void SetFailureMode(int failuresToSimulate)
    {
        CallAttempts = 0;
        _failuresRemaining = failuresToSimulate;
    }

    public async Task<HelloResponse> SayHelloAsync(HelloRequest request, CancellationToken cancellationToken = default)
    {
        CallAttempts++;
        
        if (_failuresRemaining > 0)
        {
            _failuresRemaining--;
            Console.WriteLine($"      🔄 Attempt {CallAttempts}: Simulated failure (remaining: {_failuresRemaining})");
            await Task.Delay(50, cancellationToken); // Small delay to show retry timing
            throw new InvalidOperationException($"Simulated transient failure from {Name}");
        }

        Console.WriteLine($"      ✅ Attempt {CallAttempts}: Success!");
        return new HelloResponse
        {
            Message = $"Hello, {request.Name}! Greetings from {Name} (after {CallAttempts} attempts).",
            ServiceInfo = Name,
            Language = request.Language ?? "en"
        };
    }

    public async Task<HelloResponse> SayGoodbyeAsync(HelloRequest request, CancellationToken cancellationToken = default)
    {
        CallAttempts++;
        
        if (_failuresRemaining > 0)
        {
            _failuresRemaining--;
            Console.WriteLine($"      🔄 Attempt {CallAttempts}: Simulated failure (remaining: {_failuresRemaining})");
            await Task.Delay(50, cancellationToken);
            throw new InvalidOperationException($"Simulated transient failure from {Name}");
        }

        Console.WriteLine($"      ✅ Attempt {CallAttempts}: Success!");
        return new HelloResponse
        {
            Message = $"Farewell, {request.Name}! Until next time, from {Name} (after {CallAttempts} attempts).",
            ServiceInfo = Name,
            Language = request.Language ?? "en"
        };
    }
}
