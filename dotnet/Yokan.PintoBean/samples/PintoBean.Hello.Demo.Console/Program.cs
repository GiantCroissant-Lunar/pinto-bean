using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime;
using Yokan.PintoBean.Providers.Stub;

// Console demo application for Yokan PintoBean service platform
Console.WriteLine("PintoBean Hello Demo Console");
Console.WriteLine("============================");
Console.WriteLine();
Console.WriteLine($"Abstractions Version: {PintoBeanAbstractions.Version}");
Console.WriteLine($"Runtime Version: {PintoBeanRuntime.Version}");
Console.WriteLine($"Providers.Stub Version: {PintoBeanProvidersStub.Version}");
Console.WriteLine();
Console.WriteLine("Tier-1 Seed Contract Available:");
Console.WriteLine($"- {nameof(IHelloService)} interface loaded");
Console.WriteLine($"- {nameof(HelloRequest)} DTO available");
Console.WriteLine($"- {nameof(HelloResponse)} DTO available");
Console.WriteLine();
Console.WriteLine("This demonstrates the Tier-1 contracts are available for");
Console.WriteLine("use with the Yokan PintoBean service platform.");
