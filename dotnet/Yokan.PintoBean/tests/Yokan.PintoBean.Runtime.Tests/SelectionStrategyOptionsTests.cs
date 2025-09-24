using System;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for the SelectionStrategyOptions class.
/// </summary>
public class SelectionStrategyOptionsTests
{
    [Fact]
    public void Constructor_ShouldSetRFC0003CategoryDefaults()
    {
        // Act
        var options = new SelectionStrategyOptions();

        // Assert - Verify RFC-0003 defaults
        Assert.Equal(SelectionStrategyType.FanOut, options.GetDefaultForCategory(ServiceCategory.Analytics));
        Assert.Equal(SelectionStrategyType.PickOne, options.GetDefaultForCategory(ServiceCategory.Resources));
        Assert.Equal(SelectionStrategyType.PickOne, options.GetDefaultForCategory(ServiceCategory.SceneFlow));
        Assert.Equal(SelectionStrategyType.PickOne, options.GetDefaultForCategory(ServiceCategory.AI));
    }

    [Fact]
    public void SetCategoryDefault_ShouldUpdateCategoryDefault()
    {
        // Arrange
        var options = new SelectionStrategyOptions();

        // Act
        var result = options.SetCategoryDefault(ServiceCategory.Analytics, SelectionStrategyType.PickOne);

        // Assert
        Assert.Same(options, result); // Should return this for method chaining
        Assert.Equal(SelectionStrategyType.PickOne, options.GetDefaultForCategory(ServiceCategory.Analytics));
    }

    [Fact]
    public void UseStrategyFor_WithType_ShouldSetStrategyOverride()
    {
        // Arrange
        var options = new SelectionStrategyOptions();
        var serviceType = typeof(ITestService);

        // Act
        var result = options.UseStrategyFor(serviceType, SelectionStrategyType.FanOut);

        // Assert
        Assert.Same(options, result); // Should return this for method chaining
        Assert.Equal(SelectionStrategyType.FanOut, options.GetStrategyOverride(serviceType));
    }

    [Fact]
    public void UseStrategyFor_WithGeneric_ShouldSetStrategyOverride()
    {
        // Arrange
        var options = new SelectionStrategyOptions();

        // Act
        var result = options.UseStrategyFor<ITestService>(SelectionStrategyType.FanOut);

        // Assert
        Assert.Same(options, result); // Should return this for method chaining
        Assert.Equal(SelectionStrategyType.FanOut, options.GetStrategyOverride(typeof(ITestService)));
    }

    [Fact]
    public void UseStrategyFor_WithNullType_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new SelectionStrategyOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            options.UseStrategyFor(null!, SelectionStrategyType.PickOne));
    }

    [Fact]
    public void UseCustomStrategyFor_WithType_ShouldSetCustomFactory()
    {
        // Arrange
        var options = new SelectionStrategyOptions();
        var serviceType = typeof(ITestService);
        Func<IServiceProvider, ISelectionStrategy> factory = _ => new TestSelectionStrategy();

        // Act
        var result = options.UseCustomStrategyFor(serviceType, factory);

        // Assert
        Assert.Same(options, result); // Should return this for method chaining
        Assert.Same(factory, options.GetCustomStrategyFactory(serviceType));
    }

    [Fact]
    public void UseCustomStrategyFor_WithGeneric_ShouldSetCustomFactory()
    {
        // Arrange
        var options = new SelectionStrategyOptions();
        Func<IServiceProvider, ISelectionStrategy<ITestService>> factory = _ => new TestSelectionStrategy();

        // Act
        var result = options.UseCustomStrategyFor<ITestService>(factory);

        // Assert
        Assert.Same(options, result); // Should return this for method chaining
        Assert.NotNull(options.GetCustomStrategyFactory(typeof(ITestService)));
    }

    [Fact]
    public void UseCustomStrategyFor_WithNullType_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new SelectionStrategyOptions();
        Func<IServiceProvider, ISelectionStrategy> factory = _ => new TestSelectionStrategy();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            options.UseCustomStrategyFor(null!, factory));
    }

    [Fact]
    public void UseCustomStrategyFor_WithNullFactory_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new SelectionStrategyOptions();
        var serviceType = typeof(ITestService);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            options.UseCustomStrategyFor(serviceType, null!));
    }

    [Fact]
    public void UseCustomStrategyFor_WithNullGenericFactory_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new SelectionStrategyOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            options.UseCustomStrategyFor<ITestService>(null!));
    }

    [Fact]
    public void GetStrategyOverride_WithNoOverride_ShouldReturnNull()
    {
        // Arrange
        var options = new SelectionStrategyOptions();
        var serviceType = typeof(ITestService);

        // Act
        var result = options.GetStrategyOverride(serviceType);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetCustomStrategyFactory_WithNoFactory_ShouldReturnNull()
    {
        // Arrange
        var options = new SelectionStrategyOptions();
        var serviceType = typeof(ITestService);

        // Act
        var result = options.GetCustomStrategyFactory(serviceType);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAllStrategyOverrides_ShouldReturnAllConfiguredOverrides()
    {
        // Arrange
        var options = new SelectionStrategyOptions();
        var serviceType1 = typeof(ITestService);
        var serviceType2 = typeof(ITestService2);

        // Act
        options.UseStrategyFor(serviceType1, SelectionStrategyType.FanOut);
        options.UseStrategyFor(serviceType2, SelectionStrategyType.Sharded);
        var result = options.GetAllStrategyOverrides();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey(serviceType1));
        Assert.True(result.ContainsKey(serviceType2));
        Assert.Equal(SelectionStrategyType.FanOut, result[serviceType1]);
        Assert.Equal(SelectionStrategyType.Sharded, result[serviceType2]);
    }

    [Fact]
    public void GetAllCustomStrategyFactories_ShouldReturnAllConfiguredFactories()
    {
        // Arrange
        var options = new SelectionStrategyOptions();
        var serviceType1 = typeof(ITestService);
        var serviceType2 = typeof(ITestService2);
        Func<IServiceProvider, ISelectionStrategy> factory1 = _ => new TestSelectionStrategy();
        Func<IServiceProvider, ISelectionStrategy> factory2 = _ => new TestSelectionStrategy();

        // Act
        options.UseCustomStrategyFor(serviceType1, factory1);
        options.UseCustomStrategyFor(serviceType2, factory2);
        var result = options.GetAllCustomStrategyFactories();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey(serviceType1));
        Assert.True(result.ContainsKey(serviceType2));
        Assert.Same(factory1, result[serviceType1]);
        Assert.Same(factory2, result[serviceType2]);
    }

    /// <summary>
    /// Test service interface.
    /// </summary>
    public interface ITestService
    {
        string GetName();
    }

    /// <summary>
    /// Second test service interface.
    /// </summary>
    public interface ITestService2
    {
        string GetValue();
    }

    /// <summary>
    /// Test selection strategy implementation.
    /// </summary>
    public class TestSelectionStrategy : ISelectionStrategy<ITestService>, ISelectionStrategy
    {
        public SelectionStrategyType StrategyType => SelectionStrategyType.PickOne;
        public Type ServiceType => typeof(ITestService);

        public ISelectionResult<ITestService> SelectProviders(ISelectionContext<ITestService> context)
        {
            throw new NotImplementedException();
        }

        public bool CanHandle(ISelectionContext<ITestService> context)
        {
            return true;
        }
    }
}