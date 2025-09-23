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
/// Integration tests that validate the full acceptance criteria for P1-05.
/// These tests demonstrate the complete registry runtime functionality.
/// </summary>
public class AcceptanceCriteriaTests
{
    /// <summary>
    /// Mock hello service implementation for acceptance testing.
    /// </summary>
    public class MockHelloService : IHelloService
    {
        public string ServiceId { get; }
        public List<string> CallLog { get; } = new();

        public MockHelloService(string serviceId)
        {
            ServiceId = serviceId;
        }

        public Task<HelloResponse> SayHelloAsync(HelloRequest request, CancellationToken cancellationToken = default)
        {
            CallLog.Add($"SayHello({request.Name})");
            return Task.FromResult(new HelloResponse
            {
                Message = $"Hello, {request.Name}! (from {ServiceId})",
                ServiceInfo = ServiceId,
                Language = request.Language ?? "en"
            });
        }

        public Task<HelloResponse> SayGoodbyeAsync(HelloRequest request, CancellationToken cancellationToken = default)
        {
            CallLog.Add($"SayGoodbye({request.Name})");
            return Task.FromResult(new HelloResponse
            {
                Message = $"Goodbye, {request.Name}! (from {ServiceId})",
                ServiceInfo = ServiceId,
                Language = request.Language ?? "en"
            });
        }
    }

    [Fact]
    public void AcceptanceCriteria_RegisterProviders_And_ResolveForIHelloService()
    {
        // Arrange: Create service registry and providers
        var registry = new ServiceRegistry();
        
        var primaryProvider = new MockHelloService("PrimaryHelloService");
        var fallbackProvider = new MockHelloService("FallbackHelloService");
        
        var primaryCapabilities = ProviderCapabilities.Create("primary-hello")
            .WithPriority(Priority.High)
            .WithPlatform(Platform.Any)
            .WithTags("primary", "greeting");
            
        var fallbackCapabilities = ProviderCapabilities.Create("fallback-hello")
            .WithPriority(Priority.Low)
            .WithPlatform(Platform.Any)
            .WithTags("fallback", "greeting");

        // Act: Register providers
        var primaryRegistration = registry.Register<IHelloService>(primaryProvider, primaryCapabilities);
        var fallbackRegistration = registry.Register<IHelloService>(fallbackProvider, fallbackCapabilities);

        // Assert: Providers are registered successfully
        Assert.NotNull(primaryRegistration);
        Assert.NotNull(fallbackRegistration);
        Assert.True(registry.HasRegistrations<IHelloService>());
        
        var registrations = registry.GetRegistrations<IHelloService>().ToList();
        Assert.Equal(2, registrations.Count);
        
        // Verify registration details
        var primaryReg = registrations.First(r => r.Capabilities.ProviderId == "primary-hello");
        var fallbackReg = registrations.First(r => r.Capabilities.ProviderId == "fallback-hello");
        
        Assert.Equal(Priority.High, primaryReg.Capabilities.Priority);
        Assert.Equal(Priority.Low, fallbackReg.Capabilities.Priority);
        Assert.True(primaryReg.Capabilities.HasTags("primary", "greeting"));
        Assert.True(fallbackReg.Capabilities.HasTags("fallback", "greeting"));
    }

    [Fact]
    public async Task AcceptanceCriteria_ForIHelloService_InvokesCorrectProvider()
    {
        // Arrange: Set up registry with prioritized providers
        var registry = new ServiceRegistry();
        
        var primaryProvider = new MockHelloService("PrimaryService");
        var secondaryProvider = new MockHelloService("SecondaryService");
        
        // Register secondary first to test priority selection
        registry.Register<IHelloService>(secondaryProvider, 
            ProviderCapabilities.Create("secondary").WithPriority(Priority.Low));
        registry.Register<IHelloService>(primaryProvider, 
            ProviderCapabilities.Create("primary").WithPriority(Priority.High));

        var typedRegistry = registry.For<IHelloService>();
        var request = new HelloRequest { Name = "World" };

        // Act: Invoke through typed registry
        var response = await typedRegistry.InvokeAsync((service, ct) =>
            service.SayHelloAsync(request, ct));

        // Assert: Primary (high priority) provider was selected
        Assert.NotNull(response);
        Assert.Equal("Hello, World! (from PrimaryService)", response.Message);
        Assert.Equal("PrimaryService", response.ServiceInfo);
        
        // Verify call was logged in primary provider
        Assert.Single(primaryProvider.CallLog);
        Assert.Equal("SayHello(World)", primaryProvider.CallLog[0]);
        
        // Verify secondary provider was not called
        Assert.Empty(secondaryProvider.CallLog);
    }

    [Fact]
    public void AcceptanceCriteria_ProviderChanged_RaisedOnRegistrationChanges()
    {
        // Arrange: Set up registry with event tracking
        var registry = new ServiceRegistry();
        var eventList = new List<ProviderChangedEventArgs>();
        
        registry.ProviderChanged += (sender, e) => eventList.Add(e);
        
        var provider = new MockHelloService("TestService");
        var capabilities = ProviderCapabilities.Create("test-service");

        // Act & Assert: Test registration event
        var registration = registry.Register<IHelloService>(provider, capabilities);
        
        Assert.Single(eventList);
        var addEvent = eventList[0];
        Assert.Equal(ProviderChangeType.Added, addEvent.ChangeType);
        Assert.Equal(typeof(IHelloService), addEvent.ServiceType);
        Assert.Same(registration, addEvent.Registration);
        
        eventList.Clear();

        // Act & Assert: Test unregistration event
        registry.Unregister(registration);
        
        Assert.Single(eventList);
        var removeEvent = eventList[0];
        Assert.Equal(ProviderChangeType.Removed, removeEvent.ChangeType);
        Assert.Equal(typeof(IHelloService), removeEvent.ServiceType);
        Assert.Same(registration, removeEvent.Registration);
        
        eventList.Clear();

        // Act & Assert: Test clear registrations event
        registry.Register<IHelloService>(provider, capabilities);
        eventList.Clear(); // Clear the registration event from above
        
        registry.ClearRegistrations<IHelloService>();
        
        Assert.Single(eventList);
        var clearEvent = eventList[0];
        Assert.Equal(ProviderChangeType.Removed, clearEvent.ChangeType);
        Assert.Equal(typeof(IHelloService), clearEvent.ServiceType);
    }

    [Fact]
    public void AcceptanceCriteria_DependencyInjection_Integration()
    {
        // Arrange: Set up DI container with service registry
        var services = new ServiceCollection();
        services.AddServiceRegistry();
        
        var serviceProvider = services.BuildServiceProvider();

        // Act: Resolve service registry from DI
        var registry = serviceProvider.GetRequiredService<IServiceRegistry>();

        // Assert: Registry is available and functional
        Assert.NotNull(registry);
        Assert.IsType<ServiceRegistry>(registry);
        
        // Verify it's a singleton
        var registry2 = serviceProvider.GetRequiredService<IServiceRegistry>();
        Assert.Same(registry, registry2);
        
        // Verify it works with provider registration
        var provider = new MockHelloService("DIRegisteredService");
        var capabilities = ProviderCapabilities.Create("di-service");
        
        var registration = registry.Register<IHelloService>(provider, capabilities);
        Assert.NotNull(registration);
        Assert.True(registry.HasRegistrations<IHelloService>());
    }

    [Fact]
    public void AcceptanceCriteria_DependencyInjection_WithConfiguration()
    {
        // Arrange: Set up DI container with registry configuration
        var services = new ServiceCollection();
        var configurationCalled = false;
        
        services.AddServiceRegistry(registry =>
        {
            configurationCalled = true;
            
            // Pre-register a provider during configuration
            var provider = new MockHelloService("ConfiguredService");
            var capabilities = ProviderCapabilities.Create("configured-service")
                .WithPriority(Priority.High)
                .WithTags("configured", "bootstrap");
                
            registry.Register<IHelloService>(provider, capabilities);
        });
        
        var serviceProvider = services.BuildServiceProvider();

        // Act: Resolve and verify configured registry
        var registry = serviceProvider.GetRequiredService<IServiceRegistry>();

        // Assert: Configuration was applied
        Assert.True(configurationCalled);
        Assert.True(registry.HasRegistrations<IHelloService>());
        
        var registrations = registry.GetRegistrations<IHelloService>().ToList();
        Assert.Single(registrations);
        
        var configuredReg = registrations[0];
        Assert.Equal("configured-service", configuredReg.Capabilities.ProviderId);
        Assert.Equal(Priority.High, configuredReg.Capabilities.Priority);
        Assert.True(configuredReg.Capabilities.HasTags("configured", "bootstrap"));
    }

    [Fact]
    public async Task AcceptanceCriteria_CompleteWorkflow_RegisterResolveInvoke()
    {
        // Arrange: Complete end-to-end workflow test
        var services = new ServiceCollection();
        services.AddServiceRegistry(registry =>
        {
            // Register multiple providers with different capabilities
            var analyticsProvider = new MockHelloService("AnalyticsService");
            var primaryProvider = new MockHelloService("PrimaryService");
            var fallbackProvider = new MockHelloService("FallbackService");
            
            registry.Register<IHelloService>(analyticsProvider,
                ProviderCapabilities.Create("analytics-hello")
                    .WithPriority(Priority.Normal)
                    .WithTags("analytics", "telemetry"));
                    
            registry.Register<IHelloService>(primaryProvider,
                ProviderCapabilities.Create("primary-hello")
                    .WithPriority(Priority.High)
                    .WithTags("primary", "production"));
                    
            registry.Register<IHelloService>(fallbackProvider,
                ProviderCapabilities.Create("fallback-hello")
                    .WithPriority(Priority.Low)
                    .WithTags("fallback", "backup"));
        });
        
        var serviceProvider = services.BuildServiceProvider();

        // Act: Complete workflow
        var registry = serviceProvider.GetRequiredService<IServiceRegistry>();
        var typedRegistry = registry.For<IHelloService>();
        
        // Verify we have all three providers
        var allRegistrations = typedRegistry.GetRegistrations().ToList();
        Assert.Equal(3, allRegistrations.Count);
        
        // Invoke the service (should select highest priority = primary)
        var request = new HelloRequest { Name = "Integration Test" };
        var response = await typedRegistry.InvokeAsync((service, ct) =>
            service.SayHelloAsync(request, ct));

        // Assert: End-to-end functionality works correctly
        Assert.NotNull(response);
        Assert.Equal("Hello, Integration Test! (from PrimaryService)", response.Message);
        Assert.Equal("PrimaryService", response.ServiceInfo);
        
        // Verify provider selection worked correctly
        var primaryProvider = (MockHelloService)allRegistrations
            .First(r => r.Capabilities.ProviderId == "primary-hello").Provider;
        Assert.Single(primaryProvider.CallLog);
        Assert.Equal("SayHello(Integration Test)", primaryProvider.CallLog[0]);
    }

    [Fact]
    public void AcceptanceCriteria_CacheInvalidation_ThroughProviderChangedEvents()
    {
        // Arrange: Test that ProviderChanged events support cache invalidation
        var registry = new ServiceRegistry();
        var cacheInvalidationCount = 0;
        
        // Simulate cache invalidation on provider changes
        registry.ProviderChanged += (sender, e) =>
        {
            cacheInvalidationCount++;
            // In a real implementation, this would invalidate selection strategy caches
        };
        
        var provider1 = new MockHelloService("Service1");
        var provider2 = new MockHelloService("Service2");
        
        // Act: Multiple registry operations that should trigger cache invalidation
        var reg1 = registry.Register<IHelloService>(provider1,
            ProviderCapabilities.Create("service-1"));
        // Cache invalidation: +1
        
        var reg2 = registry.Register<IHelloService>(provider2,
            ProviderCapabilities.Create("service-2"));
        // Cache invalidation: +2
        
        registry.Unregister(reg1);
        // Cache invalidation: +3
        
        registry.ClearRegistrations<IHelloService>();
        // Cache invalidation: +4 (for removing reg2)

        // Assert: All operations triggered cache invalidation events
        Assert.Equal(4, cacheInvalidationCount);
    }
}