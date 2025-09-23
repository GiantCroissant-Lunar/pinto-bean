# Yokan PintoBean

Cross-engine service platform with 4-tier architecture for Unity, Godot, and custom .NET applications.

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

## Documentation

See [RFCs](docs/rfcs/) for detailed architecture documentation:

- [RFC-0001: Service Platform Core (4-Tier, Reverse-Mode Façade)](docs/rfcs/rfc-0001-service-platform-core.md)
- [RFC-0002: Code Generation & Consumption (Façades, Registry, Packaging)](docs/rfcs/rfc-0002-codegen-and-consumption.md)
- [RFC-0003: Selection Strategies (PickOne, FanOut, Sharded) & Category Defaults](docs/rfcs/rfc-0003-selection-strategies.md)