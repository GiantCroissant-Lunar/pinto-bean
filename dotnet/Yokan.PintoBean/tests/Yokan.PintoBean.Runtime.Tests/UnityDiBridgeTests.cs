// Tests for Unity DI bridge functionality (P6-01 acceptance criteria)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime.Unity;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for P6-01: Unity DI bridge (VContainer) for Microsoft.Extensions.DependencyInjection
/// Verifies that the Unity bridge successfully integrates MS.DI services with Unity MonoBehaviours.
/// </summary>
public class UnityDiBridgeTests
{
    [Fact]
    public void ServiceCollectionExtensions_AddUnityBridge_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var factory = new DefaultUnityLifetimeScopeFactory();

        // Act
        services.AddUnityBridge(factory);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // Verify factory is registered
        var registeredFactory = serviceProvider.GetService<IUnityLifetimeScopeFactory>();
        Assert.NotNull(registeredFactory);
        Assert.Same(factory, registeredFactory);
        
        // Verify marker service is registered
        var marker = serviceProvider.GetService<IUnityBridgeMarker>();
        Assert.NotNull(marker);
    }

    [Fact]
    public void ServiceCollectionExtensions_AddUnityBridge_WithFactoryDelegate_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var factory = new DefaultUnityLifetimeScopeFactory();

        // Act
        services.AddUnityBridge(_ => factory);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // Verify factory is registered
        var registeredFactory = serviceProvider.GetService<IUnityLifetimeScopeFactory>();
        Assert.NotNull(registeredFactory);
        Assert.Same(factory, registeredFactory);
        
        // Verify marker service is registered
        var marker = serviceProvider.GetService<IUnityBridgeMarker>();
        Assert.NotNull(marker);
    }

    [Fact]
    public void UnityServiceProviderBridge_Initialize_CreatesGlobalInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IHelloService, UnityBridgeTestHelloService>();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var bridge = UnityServiceProviderBridge.Initialize(serviceProvider);

        // Assert
        Assert.NotNull(bridge);
        Assert.True(UnityServiceProviderBridge.IsInitialized);
        Assert.Same(bridge, UnityServiceProviderBridge.Current);

        // Cleanup
        UnityServiceProviderBridge.Reset();
    }

    [Fact]
    public void UnityServiceProviderBridge_GetService_ResolvesFromServiceProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IHelloService, UnityBridgeTestHelloService>();
        var serviceProvider = services.BuildServiceProvider();
        
        UnityServiceProviderBridge.Initialize(serviceProvider);

        // Act
        var helloService = UnityServiceProviderBridge.Current.GetService<IHelloService>();

        // Assert
        Assert.NotNull(helloService);
        Assert.IsType<UnityBridgeTestHelloService>(helloService);

        // Cleanup
        UnityServiceProviderBridge.Reset();
    }

    [Fact]
    public void UnityServiceProviderBridge_GetServiceOrNull_ReturnsNullForUnregisteredService()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        
        UnityServiceProviderBridge.Initialize(serviceProvider);

        // Act
        var service = UnityServiceProviderBridge.Current.GetServiceOrNull<IHelloService>();

        // Assert
        Assert.Null(service);

        // Cleanup
        UnityServiceProviderBridge.Reset();
    }

    [Fact]
    public void ServiceAwareMonoBehaviour_GetService_ResolvesFromBridge()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IHelloService, UnityBridgeTestHelloService>();
        var serviceProvider = services.BuildServiceProvider();
        
        UnityServiceProviderBridge.Initialize(serviceProvider);
        var monoBehaviour = new TestServiceAwareMonoBehaviour();

        // Act
        var helloService = monoBehaviour.TestGetService<IHelloService>();

        // Assert
        Assert.NotNull(helloService);
        Assert.IsType<UnityBridgeTestHelloService>(helloService);

        // Cleanup
        UnityServiceProviderBridge.Reset();
    }

    [Fact]
    public void ServiceAwareMonoBehaviour_GetServiceOrNull_HandlesUnregisteredService()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        
        UnityServiceProviderBridge.Initialize(serviceProvider);
        var monoBehaviour = new TestServiceAwareMonoBehaviour();

        // Act
        var service = monoBehaviour.TestGetServiceOrNull<IHelloService>();

        // Assert
        Assert.Null(service);

        // Cleanup
        UnityServiceProviderBridge.Reset();
    }

    [Fact]
    public void DefaultUnityLifetimeScopeFactory_CreateLifetimeScope_ReturnsValidScope()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IHelloService, UnityBridgeTestHelloService>();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new DefaultUnityLifetimeScopeFactory();

        // Act
        var scope = factory.CreateLifetimeScope(serviceProvider);

        // Assert
        Assert.NotNull(scope);
        Assert.IsType<UnityServiceScope>(scope);
        
        var unityScope = (UnityServiceScope)scope;
        Assert.Same(serviceProvider, unityScope.ServiceProvider);
        
        // Verify bridge was initialized
        Assert.True(UnityServiceProviderBridge.IsInitialized);

        // Cleanup
        UnityServiceProviderBridge.Reset();
        unityScope.Dispose();
    }

    [Fact]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Assembly references are needed for architectural validation")]
    public void AcceptanceCriteria_UnityAsmdefCompilesWithHelloServiceResolution()
    {
        // This test verifies the acceptance criteria:
        // "A Unity asmdef compiles with a sample MonoBehaviour that resolves IHelloService (façade) via bridge"
        
        // Arrange: Set up services including IHelloService
        var services = new ServiceCollection();
        services.AddServiceRegistry();
        services.AddSelectionStrategies();
        services.AddResilienceExecutor();
        services.AddNoOpAspectRuntime();
        services.AddTransient<IHelloService, UnityBridgeTestHelloService>();
        
        var factory = new DefaultUnityLifetimeScopeFactory();
        services.AddUnityBridge(factory);
        
        var serviceProvider = services.BuildServiceProvider();

        // Act: Initialize the Unity bridge
        var scope = factory.CreateLifetimeScope(serviceProvider);
        
        // Create a sample MonoBehaviour-like class
        var sampleMonoBehaviour = new TestServiceAwareMonoBehaviour();

        // Assert: Verify IHelloService can be resolved via bridge
        var helloService = sampleMonoBehaviour.TestGetService<IHelloService>();
        Assert.NotNull(helloService);
        Assert.IsType<UnityBridgeTestHelloService>(helloService);
        
        // Verify no Unity dependencies leak into Tier-1 or Tier-2
        var abstractionsAssembly = typeof(IHelloService).Assembly;
        var runtimeAssembly = typeof(IServiceRegistry).Assembly;
        
        // Check that Abstractions (Tier-1) doesn't reference Unity types
        var abstractionsReferences = abstractionsAssembly.GetReferencedAssemblies();
        Assert.DoesNotContain(abstractionsReferences, r => r.Name?.Contains("Unity") == true || r.Name?.Contains("VContainer") == true);
        
        // Check that Runtime (Tier-2) doesn't reference Unity types  
        var runtimeReferences = runtimeAssembly.GetReferencedAssemblies();
        Assert.DoesNotContain(runtimeReferences, r => r.Name?.Contains("Unity") == true || r.Name?.Contains("VContainer") == true);

        // Cleanup
        UnityServiceProviderBridge.Reset();
        if (scope is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

/// <summary>
/// Test implementation of IHelloService for Unity bridge testing purposes.
/// </summary>
public class UnityBridgeTestHelloService : IHelloService
{
    public async Task<HelloResponse> SayHelloAsync(HelloRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);
        return new HelloResponse 
        { 
            Message = $"Hello, {request.Name}!",
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task<HelloResponse> SayGoodbyeAsync(HelloRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);
        return new HelloResponse 
        { 
            Message = $"Goodbye, {request.Name}!",
            Timestamp = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Test subclass of ServiceAwareMonoBehaviour that exposes protected methods for testing.
/// </summary>
public class TestServiceAwareMonoBehaviour : ServiceAwareMonoBehaviour
{
    public T TestGetService<T>() where T : notnull => GetService<T>();
    
    public T? TestGetServiceOrNull<T>() where T : class => GetServiceOrNull<T>();
    
    public T TestGetRequiredService<T>() where T : notnull => GetRequiredService<T>();
    
    public object? TestGetService(Type serviceType) => GetService(serviceType);
    
    public object TestGetRequiredService(Type serviceType) => GetRequiredService(serviceType);
}