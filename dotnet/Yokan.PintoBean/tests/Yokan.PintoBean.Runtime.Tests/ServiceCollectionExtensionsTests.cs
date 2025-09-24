using Xunit;
using Yokan.PintoBean.Runtime;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for the dependency injection extensions.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddServiceRegistry_ShouldRegisterServiceRegistry()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddServiceRegistry();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var registry = serviceProvider.GetService<IServiceRegistry>();
        Assert.NotNull(registry);
        Assert.IsType<ServiceRegistry>(registry);
    }

    [Fact]
    public void AddServiceRegistry_ShouldRegisterAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddServiceRegistry();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var registry1 = serviceProvider.GetService<IServiceRegistry>();
        var registry2 = serviceProvider.GetService<IServiceRegistry>();
        Assert.Same(registry1, registry2);
    }

    [Fact]
    public void AddServiceRegistry_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddServiceRegistry());
    }

    [Fact]
    public void AddServiceRegistry_WithConfiguration_ShouldConfigureRegistry()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurationCalled = false;

        // Act
        services.AddServiceRegistry(registry =>
        {
            configurationCalled = true;
            // Configuration logic would go here
            Assert.NotNull(registry);
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var registry = serviceProvider.GetService<IServiceRegistry>();
        Assert.NotNull(registry);
        Assert.True(configurationCalled);
    }

    [Fact]
    public void AddServiceRegistry_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            services.AddServiceRegistry(null!));
    }
}