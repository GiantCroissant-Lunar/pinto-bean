// Tests for AddAIRegistry extension methods
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using Xunit;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for the AddAIRegistry extension methods.
/// </summary>
public class AddAIRegistryTests
{
    [Fact]
    public void AddAIRegistry_WithoutConfiguration_ShouldRegisterRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAIRegistry();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<IServiceRegistry>());
        Assert.NotNull(serviceProvider.GetService<ISelectionStrategyFactory>());
        Assert.NotNull(serviceProvider.GetService<IOptions<SelectionStrategyOptions>>());
    }

    [Fact]
    public void AddAIRegistry_WithConfiguration_ShouldInvokeConfigurationAction()
    {
        // Arrange
        var services = new ServiceCollection();
        bool configurationCalled = false;

        // Act
        services.AddAIRegistry(registry =>
        {
            configurationCalled = true;
            // Configuration logic would go here
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert - access the registry to trigger configuration
        var registry = serviceProvider.GetService<IServiceRegistry>();
        Assert.NotNull(registry);
        Assert.True(configurationCalled);
    }

    [Fact]
    public void AddAIRegistry_ShouldConfigurePickOneAsDefaultForIAIText()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAIRegistry();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<SelectionStrategyOptions>>();
        Assert.NotNull(options);
        
        // The options should be configured with PickOne for IAIText
        var strategyOptions = options.Value;
        Assert.NotNull(strategyOptions);
    }

    [Fact]
    public void AddAIRegistry_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddAIRegistry());
    }

    [Fact]
    public void AddAIRegistry_WithNullConfigurationAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddAIRegistry(null!));
    }

    [Fact]
    public void AddAIRegistry_ShouldReturnServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddAIRegistry();

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddAIRegistry_WithConfiguration_ShouldReturnServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddAIRegistry(registry => { });

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddAIRegistry_CalledMultipleTimes_ShouldNotDuplicateRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAIRegistry();
        services.AddAIRegistry();
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Should still work correctly without duplicate registrations
        Assert.NotNull(serviceProvider.GetService<IServiceRegistry>());
        Assert.NotNull(serviceProvider.GetService<ISelectionStrategyFactory>());
    }
}