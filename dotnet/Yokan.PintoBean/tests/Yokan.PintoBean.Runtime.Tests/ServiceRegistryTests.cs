using Xunit;
using Yokan.PintoBean.Runtime;
using Yokan.PintoBean.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Test implementation of IHelloService for service registry testing.
/// </summary>
public class TestHelloService : IHelloService
{
    public string Name { get; }

    public TestHelloService(string name = "TestService")
    {
        Name = name;
    }

    public Task<HelloResponse> SayHelloAsync(HelloRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HelloResponse
        {
            Message = $"Hello, {request.Name}! (from {Name})",
            Language = request.Language ?? "en",
            ServiceInfo = Name
        });
    }

    public Task<HelloResponse> SayGoodbyeAsync(HelloRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HelloResponse
        {
            Message = $"Goodbye, {request.Name}! (from {Name})",
            Language = request.Language ?? "en",
            ServiceInfo = Name
        });
    }
}

/// <summary>
/// Comprehensive tests for the service registry implementation.
/// </summary>
public class ServiceRegistryTests
{
    [Fact]
    public void ServiceRegistry_ShouldBeCreatedSuccessfully()
    {
        // Act
        var registry = new ServiceRegistry();

        // Assert
        Assert.NotNull(registry);
        Assert.IsAssignableFrom<IServiceRegistry>(registry);
    }

    [Fact]
    public void Register_ShouldRegisterProviderSuccessfully()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var provider = new TestHelloService("TestProvider1");
        var capabilities = ProviderCapabilities.Create("test-provider-1");

        // Act
        var registration = registry.Register<IHelloService>(provider, capabilities);

        // Assert
        Assert.NotNull(registration);
        Assert.Equal(typeof(IHelloService), registration.ServiceType);
        Assert.Same(provider, registration.Provider);
        Assert.Equal(capabilities.ProviderId, registration.Capabilities.ProviderId);
        Assert.True(registration.IsActive);
    }

    [Fact]
    public void Register_WithIncompatibleProvider_ShouldThrowArgumentException()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var incompatibleProvider = "not a service";
        var capabilities = ProviderCapabilities.Create("test-provider-1");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            registry.Register(typeof(IHelloService), incompatibleProvider, capabilities));
    }

    [Fact]
    public void HasRegistrations_WithNoProviders_ShouldReturnFalse()
    {
        // Arrange
        var registry = new ServiceRegistry();

        // Act & Assert
        Assert.False(registry.HasRegistrations<IHelloService>());
        Assert.False(registry.HasRegistrations(typeof(IHelloService)));
    }

    [Fact]
    public void HasRegistrations_WithProviders_ShouldReturnTrue()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var provider = new TestHelloService("TestProvider1");
        var capabilities = ProviderCapabilities.Create("test-provider-1");

        // Act
        registry.Register<IHelloService>(provider, capabilities);

        // Assert
        Assert.True(registry.HasRegistrations<IHelloService>());
        Assert.True(registry.HasRegistrations(typeof(IHelloService)));
    }

    [Fact]
    public void GetRegistrations_WithNoProviders_ShouldReturnEmpty()
    {
        // Arrange
        var registry = new ServiceRegistry();

        // Act
        var registrations = registry.GetRegistrations<IHelloService>();

        // Assert
        Assert.Empty(registrations);
    }

    [Fact]
    public void GetRegistrations_WithProviders_ShouldReturnAll()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var provider1 = new TestHelloService("TestProvider1");
        var provider2 = new TestHelloService("TestProvider2");
        var capabilities1 = ProviderCapabilities.Create("test-provider-1");
        var capabilities2 = ProviderCapabilities.Create("test-provider-2");

        // Act
        registry.Register<IHelloService>(provider1, capabilities1);
        registry.Register<IHelloService>(provider2, capabilities2);
        var registrations = registry.GetRegistrations<IHelloService>().ToList();

        // Assert
        Assert.Equal(2, registrations.Count);
        Assert.Contains(registrations, r => r.Capabilities.ProviderId == "test-provider-1");
        Assert.Contains(registrations, r => r.Capabilities.ProviderId == "test-provider-2");
    }

    [Fact]
    public void Unregister_WithValidRegistration_ShouldReturnTrue()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var provider = new TestHelloService("TestProvider1");
        var capabilities = ProviderCapabilities.Create("test-provider-1");
        var registration = registry.Register<IHelloService>(provider, capabilities);

        // Act
        var result = registry.Unregister(registration);

        // Assert
        Assert.True(result);
        Assert.False(registry.HasRegistrations<IHelloService>());
    }

    [Fact]
    public void ClearRegistrations_ShouldRemoveAllProviders()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var provider1 = new TestHelloService("TestProvider1");
        var provider2 = new TestHelloService("TestProvider2");
        var capabilities1 = ProviderCapabilities.Create("test-provider-1");
        var capabilities2 = ProviderCapabilities.Create("test-provider-2");

        registry.Register<IHelloService>(provider1, capabilities1);
        registry.Register<IHelloService>(provider2, capabilities2);

        // Act
        var count = registry.ClearRegistrations<IHelloService>();

        // Assert
        Assert.Equal(2, count);
        Assert.False(registry.HasRegistrations<IHelloService>());
    }

    [Fact]
    public void For_ShouldReturnTypedRegistry()
    {
        // Arrange
        var registry = new ServiceRegistry();

        // Act
        var typedRegistry = registry.For<IHelloService>();

        // Assert
        Assert.NotNull(typedRegistry);
        Assert.IsAssignableFrom<IServiceRegistry<IHelloService>>(typedRegistry);
        Assert.Same(registry, typedRegistry.Registry);
    }

    [Fact]
    public async Task TypedRegistry_InvokeAsync_ShouldCallProviderMethod()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var provider = new TestHelloService("TestProvider1");
        var capabilities = ProviderCapabilities.Create("test-provider-1");
        registry.Register<IHelloService>(provider, capabilities);

        var typedRegistry = registry.For<IHelloService>();
        var request = new HelloRequest { Name = "World" };

        // Act
        var response = await typedRegistry.InvokeAsync((service, ct) =>
            service.SayHelloAsync(request, ct));

        // Assert
        Assert.NotNull(response);
        Assert.Equal("Hello, World! (from TestProvider1)", response.Message);
        Assert.Equal("TestProvider1", response.ServiceInfo);
    }

    [Fact]
    public void TypedRegistry_Invoke_WithNoProviders_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var typedRegistry = registry.For<IHelloService>();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            typedRegistry.Invoke(service => { }));
    }

    [Fact]
    public void TypedRegistry_InvokeWithResult_ShouldReturnResult()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var provider = new TestHelloService("TestProvider1");
        var capabilities = ProviderCapabilities.Create("test-provider-1");
        registry.Register<IHelloService>(provider, capabilities);

        var typedRegistry = registry.For<IHelloService>();

        // Act
        var result = typedRegistry.Invoke(service => service.GetType().Name);

        // Assert
        Assert.Equal("TestHelloService", result);
    }

    [Fact]
    public void ProviderSelection_ShouldSelectHighestPriorityProvider()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var lowPriorityProvider = new TestHelloService("LowPriority");
        var highPriorityProvider = new TestHelloService("HighPriority");

        var lowCapabilities = ProviderCapabilities.Create("low-priority")
            .WithPriority(Priority.Low);
        var highCapabilities = ProviderCapabilities.Create("high-priority")
            .WithPriority(Priority.High);

        // Register in low-priority-first order to test priority selection
        registry.Register<IHelloService>(lowPriorityProvider, lowCapabilities);
        registry.Register<IHelloService>(highPriorityProvider, highCapabilities);

        var typedRegistry = registry.For<IHelloService>();

        // Act
        var selectedName = typedRegistry.Invoke(service => ((TestHelloService)service).Name);

        // Assert
        Assert.Equal("HighPriority", selectedName);
    }

    [Fact]
    public void ProviderChanged_ShouldBeRaisedOnRegister()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var provider = new TestHelloService("TestProvider1");
        var capabilities = ProviderCapabilities.Create("test-provider-1");

        ProviderChangedEventArgs? receivedEventArgs = null;
        registry.ProviderChanged += (sender, e) => receivedEventArgs = e;

        // Act
        var registration = registry.Register<IHelloService>(provider, capabilities);

        // Assert
        Assert.NotNull(receivedEventArgs);
        Assert.Equal(ProviderChangeType.Added, receivedEventArgs.ChangeType);
        Assert.Equal(typeof(IHelloService), receivedEventArgs.ServiceType);
        Assert.Same(registration, receivedEventArgs.Registration);
    }

    [Fact]
    public void ProviderChanged_ShouldBeRaisedOnUnregister()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var provider = new TestHelloService("TestProvider1");
        var capabilities = ProviderCapabilities.Create("test-provider-1");
        var registration = registry.Register<IHelloService>(provider, capabilities);

        ProviderChangedEventArgs? receivedEventArgs = null;
        registry.ProviderChanged += (sender, e) => receivedEventArgs = e;

        // Act
        registry.Unregister(registration);

        // Assert
        Assert.NotNull(receivedEventArgs);
        Assert.Equal(ProviderChangeType.Removed, receivedEventArgs.ChangeType);
        Assert.Equal(typeof(IHelloService), receivedEventArgs.ServiceType);
        Assert.Same(registration, receivedEventArgs.Registration);
    }

    [Fact]
    public void ProviderChanged_ShouldBeRaisedOnClearRegistrations()
    {
        // Arrange
        var registry = new ServiceRegistry();
        var provider = new TestHelloService("TestProvider1");
        var capabilities = ProviderCapabilities.Create("test-provider-1");
        registry.Register<IHelloService>(provider, capabilities);

        var eventCount = 0;
        registry.ProviderChanged += (sender, e) =>
        {
            if (e.ChangeType == ProviderChangeType.Removed)
                eventCount++;
        };

        // Act
        registry.ClearRegistrations<IHelloService>();

        // Assert
        Assert.Equal(1, eventCount);
    }
}
