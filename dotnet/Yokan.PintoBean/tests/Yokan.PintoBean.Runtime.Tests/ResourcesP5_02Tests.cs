using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for P5-02: Resources sample demonstrating PickOne selection strategy with resilience fallback.
/// </summary>
public class ResourcesP5_02Tests
{
    [Fact]
    public async Task PickOneStrategy_SelectsHighestPriorityProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPollyResilience();
        services.AddServiceRegistry(registry =>
        {
            // Register providers with different priorities
            var lowPriorityStore = new TestResourceStore("LowPriority");
            var lowCapabilities = ProviderCapabilities.Create("low-priority")
                .WithPriority(Priority.Normal);
            registry.Register<ITestResourceStore>(lowPriorityStore, lowCapabilities);

            var highPriorityStore = new TestResourceStore("HighPriority");
            var highCapabilities = ProviderCapabilities.Create("high-priority")
                .WithPriority(Priority.Critical);
            registry.Register<ITestResourceStore>(highPriorityStore, highCapabilities);
        });
        services.AddSelectionStrategies();

        var serviceProvider = services.BuildServiceProvider();
        var typedRegistry = serviceProvider.GetRequiredService<IServiceRegistry>().For<ITestResourceStore>();

        // Act
        var result = await typedRegistry.InvokeAsync(async (store, ct) => await store.LoadAsync("test-key", ct));

        // Assert
        Assert.Equal("HighPriority", result); // Should select the highest priority provider
    }

    [Fact]
    public async Task ResilienceExecutor_RetriesTransientFailures()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPollyResilience(options =>
        {
            options.MaxRetryAttempts = 2;
            options.BaseRetryDelayMilliseconds = 10; // Very short for testing
        });
        services.AddServiceRegistry(registry =>
        {
            var transientFailureStore = new TransientFailureResourceStore();
            var capabilities = ProviderCapabilities.Create("transient-failure")
                .WithPriority(Priority.High);
            registry.Register<ITestResourceStore>(transientFailureStore, capabilities);
        });
        services.AddSelectionStrategies();

        var serviceProvider = services.BuildServiceProvider();
        var typedRegistry = serviceProvider.GetRequiredService<IServiceRegistry>().For<ITestResourceStore>();
        var resilienceExecutor = serviceProvider.GetRequiredService<IResilienceExecutor>();

        // Act
        var result = await typedRegistry.InvokeAsync(async (store, ct) =>
            await resilienceExecutor.ExecuteAsync(async (innerCt) => 
                await store.LoadAsync("test-key", innerCt), ct));

        // Assert
        Assert.Equal("Success after retry", result); // Should succeed after retry
    }

    [Fact]
    public async Task ProviderFallback_WorksWhenProvidersAreUnregistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPollyResilience();
        services.AddServiceRegistry(registry =>
        {
            var primaryStore = new TestResourceStore("Primary");
            var primaryCapabilities = ProviderCapabilities.Create("primary")
                .WithPriority(Priority.High);
            registry.Register<ITestResourceStore>(primaryStore, primaryCapabilities);

            var fallbackStore = new TestResourceStore("Fallback");
            var fallbackCapabilities = ProviderCapabilities.Create("fallback")
                .WithPriority(Priority.Normal);
            registry.Register<ITestResourceStore>(fallbackStore, fallbackCapabilities);
        });
        services.AddSelectionStrategies();

        var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IServiceRegistry>();
        var typedRegistry = registry.For<ITestResourceStore>();

        // Act - Initially should use primary
        var initialResult = await typedRegistry.InvokeAsync(async (store, ct) => await store.LoadAsync("test-key", ct));
        Assert.Equal("Primary", initialResult);

        // Remove primary provider
        var primaryRegistration = typedRegistry.GetRegistrations()
            .First(r => r.Capabilities.ProviderId == "primary");
        registry.Unregister(primaryRegistration);

        // Act - Should now use fallback
        var fallbackResult = await typedRegistry.InvokeAsync(async (store, ct) => await store.LoadAsync("test-key", ct));

        // Assert
        Assert.Equal("Fallback", fallbackResult);
    }

    [Fact]
    public async Task NoProviders_ThrowsAppropriateException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPollyResilience();
        services.AddServiceRegistry(registry => { }); // No providers registered
        services.AddSelectionStrategies();

        var serviceProvider = services.BuildServiceProvider();
        var typedRegistry = serviceProvider.GetRequiredService<IServiceRegistry>().For<ITestResourceStore>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await typedRegistry.InvokeAsync(async (store, ct) => await store.LoadAsync("test-key", ct)));

        Assert.Contains("No providers registered", exception.Message);
    }

    // Test resource store interface and implementations for testing
    public interface ITestResourceStore
    {
        Task<string> LoadAsync(string key, CancellationToken cancellationToken = default);
    }

    public class TestResourceStore : ITestResourceStore
    {
        private readonly string _name;

        public TestResourceStore(string name)
        {
            _name = name;
        }

        public async Task<string> LoadAsync(string key, CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken); // Simulate work
            return _name;
        }
    }

    public class TransientFailureResourceStore : ITestResourceStore
    {
        private int _attemptCount = 0;

        public async Task<string> LoadAsync(string key, CancellationToken cancellationToken = default)
        {
            _attemptCount++;
            if (_attemptCount == 1)
            {
                await Task.Delay(10, cancellationToken);
                throw new TimeoutException("Transient failure");
            }

            await Task.Delay(10, cancellationToken);
            return "Success after retry";
        }
    }
}