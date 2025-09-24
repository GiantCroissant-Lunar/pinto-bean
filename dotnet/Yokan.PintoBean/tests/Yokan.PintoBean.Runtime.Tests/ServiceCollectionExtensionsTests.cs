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

    [Fact]
    public void AddSelectionStrategies_ShouldRegisterRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSelectionStrategies();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var registry = serviceProvider.GetService<IServiceRegistry>();
        var options = serviceProvider.GetService<SelectionStrategyOptions>();
        var factory = serviceProvider.GetService<ISelectionStrategyFactory>();

        Assert.NotNull(registry);
        Assert.NotNull(options);
        Assert.NotNull(factory);
        Assert.IsType<DefaultSelectionStrategyFactory>(factory);
    }

    [Fact]
    public void AddSelectionStrategies_WithConfiguration_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurationCalled = false;

        // Act
        services.AddSelectionStrategies(options =>
        {
            configurationCalled = true;
            options.SetCategoryDefault(ServiceCategory.Analytics, SelectionStrategyType.PickOne);
            Assert.NotNull(options);
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<SelectionStrategyOptions>();
        Assert.NotNull(options);
        Assert.True(configurationCalled);
        Assert.Equal(SelectionStrategyType.PickOne, options.GetDefaultForCategory(ServiceCategory.Analytics));
    }

    [Fact]
    public void AddSelectionStrategies_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddSelectionStrategies());
    }

    [Fact]
    public void UsePickOneFor_ShouldConfigurePickOneStrategy()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.UsePickOneFor<ITestSelectionService>();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<SelectionStrategyOptions>();
        Assert.NotNull(options);
        Assert.Equal(SelectionStrategyType.PickOne, options.GetStrategyOverride(typeof(ITestSelectionService)));
    }

    [Fact]
    public void UseFanOutFor_ShouldConfigureFanOutStrategy()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.UseFanOutFor<ITestSelectionService>();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<SelectionStrategyOptions>();
        Assert.NotNull(options);
        Assert.Equal(SelectionStrategyType.FanOut, options.GetStrategyOverride(typeof(ITestSelectionService)));
    }

    [Fact]
    public void UseShardedFor_ShouldConfigureShardedStrategy()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.UseShardedFor<ITestSelectionService>();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<SelectionStrategyOptions>();
        Assert.NotNull(options);
        Assert.Equal(SelectionStrategyType.Sharded, options.GetStrategyOverride(typeof(ITestSelectionService)));
    }

    [Fact]
    public void UsePickOneFor_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).UsePickOneFor<ITestSelectionService>());
    }

    [Fact]
    public void UseFanOutFor_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).UseFanOutFor<ITestSelectionService>());
    }

    [Fact]
    public void UseShardedFor_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).UseShardedFor<ITestSelectionService>());
    }

    /// <summary>
    /// Test service interface for selection strategy testing.
    /// </summary>
    public interface ITestSelectionService
    {
        string GetName();
    }
}
