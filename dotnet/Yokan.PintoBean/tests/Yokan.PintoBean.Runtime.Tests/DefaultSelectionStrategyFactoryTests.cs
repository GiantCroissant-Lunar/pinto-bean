using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for the DefaultSelectionStrategyFactory class.
/// </summary>
public class DefaultSelectionStrategyFactoryTests
{
    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new SelectionStrategyOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DefaultSelectionStrategyFactory(null!, options));
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DefaultSelectionStrategyFactory(serviceProvider, null!));
    }

    [Fact]
    public void CreateStrategy_Generic_ShouldReturnCorrectStrategyType()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IServiceRegistry, ServiceRegistry>();
        var serviceProvider = services.BuildServiceProvider();
        var options = new SelectionStrategyOptions();
        var factory = new DefaultSelectionStrategyFactory(serviceProvider, options);

        // Act
        var strategy = factory.CreateStrategy<IAnalyticsService>();

        // Assert
        Assert.NotNull(strategy);
        Assert.Equal(SelectionStrategyType.FanOut, strategy.StrategyType); // Analytics default is FanOut
        Assert.Equal(typeof(IAnalyticsService), ((ISelectionStrategy)strategy).ServiceType);
    }

    [Fact]
    public void CreateStrategy_Generic_WithOverride_ShouldReturnOverriddenStrategyType()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IServiceRegistry, ServiceRegistry>();
        var serviceProvider = services.BuildServiceProvider();
        var options = new SelectionStrategyOptions();
        options.UseStrategyFor<IAnalyticsService>(SelectionStrategyType.PickOne);
        var factory = new DefaultSelectionStrategyFactory(serviceProvider, options);

        // Act
        var strategy = factory.CreateStrategy<IAnalyticsService>();

        // Assert
        Assert.NotNull(strategy);
        Assert.Equal(SelectionStrategyType.PickOne, strategy.StrategyType);
    }

    [Fact]
    public void CreateStrategy_Generic_WithCustomFactory_ShouldReturnCustomStrategy()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = new SelectionStrategyOptions();
        var customStrategy = new TestSelectionStrategy<ITestService>();
        options.UseCustomStrategyFor<ITestService>(_ => customStrategy);
        var factory = new DefaultSelectionStrategyFactory(serviceProvider, options);

        // Act
        var strategy = factory.CreateStrategy<ITestService>();

        // Assert
        Assert.Same(customStrategy, strategy);
    }

    [Fact]
    public void CreateStrategy_Generic_WithInvalidCustomFactory_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = new SelectionStrategyOptions();
        var invalidStrategy = new TestSelectionStrategy<IAnalyticsService>();
        options.UseCustomStrategyFor(typeof(ITestService), _ => invalidStrategy);
        var factory = new DefaultSelectionStrategyFactory(serviceProvider, options);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            factory.CreateStrategy<ITestService>());
        Assert.Contains("must return ISelectionStrategy<ITestService>", exception.Message);
    }

    [Fact]
    public void CreateStrategy_NonGeneric_ShouldReturnNonGenericStrategy()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = new SelectionStrategyOptions();
        var factory = new DefaultSelectionStrategyFactory(serviceProvider, options);

        // Act
        var strategy = factory.CreateStrategy(typeof(IResourceService));

        // Assert
        Assert.NotNull(strategy);
        Assert.Equal(SelectionStrategyType.PickOne, strategy.StrategyType); // Resources default is PickOne
        Assert.Equal(typeof(IResourceService), strategy.ServiceType);
    }

    [Fact]
    public void CreateStrategy_NonGeneric_WithNullType_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = new SelectionStrategyOptions();
        var factory = new DefaultSelectionStrategyFactory(serviceProvider, options);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            factory.CreateStrategy(null!));
    }

    [Theory]
    [InlineData(typeof(IAnalyticsService), SelectionStrategyType.FanOut)]
    [InlineData(typeof(ITelemetryService), SelectionStrategyType.FanOut)]
    [InlineData(typeof(IResourceService), SelectionStrategyType.PickOne)]
    [InlineData(typeof(IDataService), SelectionStrategyType.PickOne)]
    [InlineData(typeof(ISceneFlowService), SelectionStrategyType.PickOne)]
    [InlineData(typeof(INarrativeService), SelectionStrategyType.PickOne)]
    [InlineData(typeof(IAIService), SelectionStrategyType.PickOne)]
    [InlineData(typeof(IIntelligenceService), SelectionStrategyType.PickOne)]
    [InlineData(typeof(IUnknownService), SelectionStrategyType.PickOne)] // Default to Resources
    public void CreateStrategy_CategoryInference_ShouldReturnCorrectDefaultStrategy(Type serviceType, SelectionStrategyType expectedStrategy)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IServiceRegistry, ServiceRegistry>();
        var serviceProvider = services.BuildServiceProvider();
        var options = new SelectionStrategyOptions();
        var factory = new DefaultSelectionStrategyFactory(serviceProvider, options);

        // Act
        var strategy = factory.CreateStrategy(serviceType);

        // Assert
        Assert.Equal(expectedStrategy, strategy.StrategyType);
        Assert.Equal(serviceType, strategy.ServiceType);
    }

    [Fact]
    public void CreateStrategy_NonGeneric_WithCustomFactory_ShouldUseCustomFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = new SelectionStrategyOptions();
        var customStrategy = new TestNonGenericSelectionStrategy(typeof(ITestService));
        options.UseCustomStrategyFor(typeof(ITestService), _ => customStrategy);
        var factory = new DefaultSelectionStrategyFactory(serviceProvider, options);

        // Act
        var strategy = factory.CreateStrategy(typeof(ITestService));

        // Assert
        Assert.Same(customStrategy, strategy);
    }

    /// <summary>
    /// Test analytics service interface.
    /// </summary>
    public interface IAnalyticsService
    {
        void Track(string eventName);
    }

    /// <summary>
    /// Test telemetry service interface.
    /// </summary>
    public interface ITelemetryService
    {
        void Record(string metric);
    }

    /// <summary>
    /// Test resource service interface.
    /// </summary>
    public interface IResourceService
    {
        string LoadResource(string key);
    }

    /// <summary>
    /// Test data service interface.
    /// </summary>
    public interface IDataService
    {
        T GetData<T>(string key);
    }

    /// <summary>
    /// Test scene flow service interface.
    /// </summary>
    public interface ISceneFlowService
    {
        void AdvanceScene();
    }

    /// <summary>
    /// Test narrative service interface.
    /// </summary>
    public interface INarrativeService
    {
        void TellStory();
    }

    /// <summary>
    /// Test AI service interface.
    /// </summary>
    public interface IAIService
    {
        string Predict(string input);
    }

    /// <summary>
    /// Test intelligence service interface.
    /// </summary>
    public interface IIntelligenceService
    {
        void Learn();
    }

    /// <summary>
    /// Test unknown service interface.
    /// </summary>
    public interface IUnknownService
    {
        void DoSomething();
    }

    /// <summary>
    /// Test service interface.
    /// </summary>
    public interface ITestService
    {
        string GetName();
    }

    /// <summary>
    /// Test selection strategy implementation.
    /// </summary>
    public class TestSelectionStrategy<TService> : ISelectionStrategy<TService>, ISelectionStrategy
        where TService : class
    {
        public SelectionStrategyType StrategyType => SelectionStrategyType.PickOne;
        public Type ServiceType => typeof(TService);

        public ISelectionResult<TService> SelectProviders(ISelectionContext<TService> context)
        {
            throw new NotImplementedException();
        }

        public bool CanHandle(ISelectionContext<TService> context)
        {
            return true;
        }
    }

    /// <summary>
    /// Test non-generic selection strategy implementation.
    /// </summary>
    public class TestNonGenericSelectionStrategy : ISelectionStrategy
    {
        public SelectionStrategyType StrategyType => SelectionStrategyType.PickOne;
        public Type ServiceType { get; }

        public TestNonGenericSelectionStrategy(Type serviceType)
        {
            ServiceType = serviceType;
        }
    }
}
