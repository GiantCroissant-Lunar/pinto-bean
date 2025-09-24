// P2-08: Sample test providers for IHelloService (for strategy tests)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Sample IHelloService provider A for strategy testing.
/// Configured with specific capabilities, priority, and platform support.
/// </summary>
public class HelloProviderA : IHelloService
{
    public string ProviderId { get; }
    public List<string> CallLog { get; } = new();
    public int CallCount => CallLog.Count;

    public HelloProviderA(string? providerId = null)
    {
        ProviderId = providerId ?? "hello-provider-a";
    }

    public Task<HelloResponse> SayHelloAsync(HelloRequest request, CancellationToken cancellationToken = default)
    {
        CallLog.Add($"SayHello({request.Name})");
        return Task.FromResult(new HelloResponse
        {
            Message = $"Greetings, {request.Name}! From Provider A",
            ServiceInfo = ProviderId,
            Language = request.Language ?? "en"
        });
    }

    public Task<HelloResponse> SayGoodbyeAsync(HelloRequest request, CancellationToken cancellationToken = default)
    {
        CallLog.Add($"SayGoodbye({request.Name})");
        return Task.FromResult(new HelloResponse
        {
            Message = $"Farewell, {request.Name}! From Provider A",
            ServiceInfo = ProviderId,
            Language = request.Language ?? "en"
        });
    }

    public void ClearCallLog() => CallLog.Clear();
}

/// <summary>
/// Sample IHelloService provider B for strategy testing.
/// Configured with different capabilities, priority, and platform support.
/// </summary>
public class HelloProviderB : IHelloService
{
    public string ProviderId { get; }
    public List<string> CallLog { get; } = new();
    public int CallCount => CallLog.Count;

    public HelloProviderB(string? providerId = null)
    {
        ProviderId = providerId ?? "hello-provider-b";
    }

    public Task<HelloResponse> SayHelloAsync(HelloRequest request, CancellationToken cancellationToken = default)
    {
        CallLog.Add($"SayHello({request.Name})");
        return Task.FromResult(new HelloResponse
        {
            Message = $"Hello there, {request.Name}! From Provider B",
            ServiceInfo = ProviderId,
            Language = request.Language ?? "en"
        });
    }

    public Task<HelloResponse> SayGoodbyeAsync(HelloRequest request, CancellationToken cancellationToken = default)
    {
        CallLog.Add($"SayGoodbye({request.Name})");
        return Task.FromResult(new HelloResponse
        {
            Message = $"See you later, {request.Name}! From Provider B", 
            ServiceInfo = ProviderId,
            Language = request.Language ?? "en"
        });
    }

    public void ClearCallLog() => CallLog.Clear();
}

/// <summary>
/// Registry registration helpers for IHelloService test providers.
/// Provides common configurations and patterns for strategy testing.
/// </summary>
public static class HelloTestProviderHelpers
{
    /// <summary>
    /// Creates a HelloProviderA with high priority and analytics capabilities.
    /// </summary>
    public static (HelloProviderA provider, ProviderCapabilities capabilities) CreateHighPriorityProviderA(
        string? providerId = null,
        Platform platform = Platform.Any)
    {
        var provider = new HelloProviderA(providerId);
        var capabilities = ProviderCapabilities.Create(provider.ProviderId)
            .WithPriority(Priority.High)
            .WithPlatform(platform)
            .WithTags("analytics", "primary", "greeting")
            .AddMetadata("provider_type", "A")
            .AddMetadata("version", "1.0.0");

        return (provider, capabilities);
    }

    /// <summary>
    /// Creates a HelloProviderB with normal priority and telemetry capabilities.
    /// </summary>
    public static (HelloProviderB provider, ProviderCapabilities capabilities) CreateNormalPriorityProviderB(
        string? providerId = null,
        Platform platform = Platform.Any)
    {
        var provider = new HelloProviderB(providerId);
        var capabilities = ProviderCapabilities.Create(provider.ProviderId)
            .WithPriority(Priority.Normal)
            .WithPlatform(platform)
            .WithTags("telemetry", "secondary", "greeting")
            .AddMetadata("provider_type", "B")
            .AddMetadata("version", "1.0.0");

        return (provider, capabilities);
    }

    /// <summary>
    /// Creates a HelloProviderA with low priority for fallback scenarios.
    /// </summary>
    public static (HelloProviderA provider, ProviderCapabilities capabilities) CreateFallbackProviderA(
        string? providerId = null,
        Platform platform = Platform.Any)
    {
        var provider = new HelloProviderA(providerId);
        var capabilities = ProviderCapabilities.Create(provider.ProviderId)
            .WithPriority(Priority.Low)
            .WithPlatform(platform)
            .WithTags("fallback", "greeting")
            .AddMetadata("provider_type", "A")
            .AddMetadata("role", "fallback");

        return (provider, capabilities);
    }

    /// <summary>
    /// Creates a HelloProviderB with critical priority for runtime scenarios.
    /// </summary>
    public static (HelloProviderB provider, ProviderCapabilities capabilities) CreateCriticalProviderB(
        string? providerId = null,
        Platform platform = Platform.Any)
    {
        var provider = new HelloProviderB(providerId);
        var capabilities = ProviderCapabilities.Create(provider.ProviderId)
            .WithPriority(Priority.Critical)
            .WithPlatform(platform)
            .WithTags("critical", "runtime", "greeting")
            .AddMetadata("provider_type", "B")
            .AddMetadata("role", "critical");

        return (provider, capabilities);
    }

    /// <summary>
    /// Creates a HelloProviderA with analytics-specific tags for sharded testing.
    /// </summary>
    public static (HelloProviderA provider, ProviderCapabilities capabilities) CreateAnalyticsProviderA(
        string? providerId = null,
        Platform platform = Platform.Any)
    {
        var provider = new HelloProviderA(providerId);
        var capabilities = ProviderCapabilities.Create(provider.ProviderId)
            .WithPriority(Priority.Normal)
            .WithPlatform(platform)
            .WithTags("analytics", "events", "player")
            .AddMetadata("provider_type", "A")
            .AddMetadata("shard_category", "player");

        return (provider, capabilities);
    }

    /// <summary>
    /// Creates a HelloProviderB with game-specific tags for sharded testing.
    /// </summary>
    public static (HelloProviderB provider, ProviderCapabilities capabilities) CreateGameProviderB(
        string? providerId = null,
        Platform platform = Platform.Any)
    {
        var provider = new HelloProviderB(providerId);
        var capabilities = ProviderCapabilities.Create(provider.ProviderId)
            .WithPriority(Priority.Normal)
            .WithPlatform(platform)
            .WithTags("analytics", "events", "game")
            .AddMetadata("provider_type", "B")
            .AddMetadata("shard_category", "game");

        return (provider, capabilities);
    }

    /// <summary>
    /// Registers a complete set of test providers for strategy testing.
    /// </summary>
    public static StrategyTestSetup RegisterTestProviders(IServiceRegistry registry)
    {
        var (providerA1, capabilitiesA1) = CreateHighPriorityProviderA("provider-a-high");
        var (providerB1, capabilitiesB1) = CreateNormalPriorityProviderB("provider-b-normal");
        var (providerA2, capabilitiesA2) = CreateFallbackProviderA("provider-a-fallback");
        var (providerB2, capabilitiesB2) = CreateCriticalProviderB("provider-b-critical");
        var (providerA3, capabilitiesA3) = CreateAnalyticsProviderA("provider-a-analytics");
        var (providerB3, capabilitiesB3) = CreateGameProviderB("provider-b-game");

        var registrations = new List<IProviderRegistration>
        {
            registry.Register<IHelloService>(providerA1, capabilitiesA1),
            registry.Register<IHelloService>(providerB1, capabilitiesB1),
            registry.Register<IHelloService>(providerA2, capabilitiesA2),
            registry.Register<IHelloService>(providerB2, capabilitiesB2),
            registry.Register<IHelloService>(providerA3, capabilitiesA3),
            registry.Register<IHelloService>(providerB3, capabilitiesB3)
        };

        return new StrategyTestSetup
        {
            Registry = registry,
            Registrations = registrations,
            HighPriorityA = providerA1,
            NormalPriorityB = providerB1,
            FallbackA = providerA2,
            CriticalB = providerB2,
            AnalyticsA = providerA3,
            GameB = providerB3
        };
    }
}

/// <summary>
/// Test setup structure for strategy testing with registered providers.
/// </summary>
public class StrategyTestSetup
{
    public IServiceRegistry Registry { get; init; } = null!;
    public List<IProviderRegistration> Registrations { get; init; } = new();
    public HelloProviderA HighPriorityA { get; init; } = null!;
    public HelloProviderB NormalPriorityB { get; init; } = null!;
    public HelloProviderA FallbackA { get; init; } = null!;
    public HelloProviderB CriticalB { get; init; } = null!;
    public HelloProviderA AnalyticsA { get; init; } = null!;  
    public HelloProviderB GameB { get; init; } = null!;

    /// <summary>
    /// Clears call logs for all providers.
    /// </summary>
    public void ClearAllCallLogs()
    {
        HighPriorityA.ClearCallLog();
        NormalPriorityB.ClearCallLog();
        FallbackA.ClearCallLog();
        CriticalB.ClearCallLog();
        AnalyticsA.ClearCallLog();
        GameB.ClearCallLog();
    }
}