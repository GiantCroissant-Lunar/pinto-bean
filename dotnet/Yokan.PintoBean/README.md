# Yokan PintoBean

Cross-engine service platform with 4-tier architecture for Unity, Godot, and custom .NET applications.

## Quick Start - Dependency Injection Configuration

Minimal setup with resilience and telemetry seams (following RFC-0001/0002 hooks):

```csharp
using Microsoft.Extensions.DependencyInjection;
using Yokan.PintoBean.Runtime;

var services = new ServiceCollection();

// Core service registry
services.AddServiceRegistry();
services.AddSelectionStrategies();

// Choose your runtime seams:
// Option 1: Baseline (no telemetry, pass-through resilience)
services.AddNoOpAspectRuntime();
services.AddResilienceExecutor(); // Uses NoOpResilienceExecutor

// Option 2: Full observability and resilience
services.AddOpenTelemetryAspectRuntime("MyApp", "MyApp.Metrics");
services.AddPollyResilience(options => {
    options.DefaultTimeoutSeconds = 30.0;
    options.MaxRetryAttempts = 3;
    options.BaseRetryDelayMilliseconds = 1000.0;
});

var serviceProvider = services.BuildServiceProvider();

// Register providers and use the service registry
var registry = serviceProvider.GetRequiredService<IServiceRegistry>();
// ... register your service providers
```

## Architecture

- **Tier-1**: `Yokan.PintoBean.Abstractions` - Contracts and interfaces
- **Tier-2**: `Yokan.PintoBean.CodeGen` - Incremental source generators and analyzers  
- **Tier-3**: `Yokan.PintoBean.Runtime` - Adapters and registry runtime
- **Tier-4**: `Yokan.PintoBean.Providers.Stub` - Example provider implementations

## Building and Testing

```bash
dotnet build
dotnet test
dotnet run --project samples/PintoBean.Hello.Demo.Console
```

The console sample demonstrates both baseline and full observability modes, showcasing:
- AspectRuntime telemetry seams (NoOp vs OpenTelemetry)
- ResilienceExecutor patterns (PassThrough vs Polly retry/timeout)
- Timing capture and retry attempt logging
- Provider registration and selection strategies

## Documentation

See [RFCs](docs/rfcs/) for detailed architecture documentation:

- [RFC-0001: Service Platform Core (4-Tier, Reverse-Mode Façade)](docs/rfcs/rfc-0001-service-platform-core.md) - Defines AspectRuntime and ResilienceExecutor hooks
- [RFC-0002: Code Generation & Consumption (Façades, Registry, Packaging)](docs/rfcs/rfc-0002-codegen-and-consumption.md) - Describes telemetry integration patterns  
- [RFC-0003: Selection Strategies (PickOne, FanOut, Sharded) & Category Defaults](docs/rfcs/rfc-0003-selection-strategies.md)