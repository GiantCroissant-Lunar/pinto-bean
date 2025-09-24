using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Integration tests demonstrating end-to-end functionality of the selection strategy DI integration.
/// </summary>
public class IntegrationTests
{
    [Fact]
    public void End2End_AddSelectionStrategies_ShouldConfigureDefaultsAndCreateStrategies()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Add selection strategies with default configuration
        services.AddSelectionStrategies();
        var serviceProvider = services.BuildServiceProvider();

        // Get the factory and create strategies for different service categories
        var factory = serviceProvider.GetRequiredService<ISelectionStrategyFactory>();

        // Assert - Verify Analytics gets FanOut by default
        var analyticsStrategy = factory.CreateStrategy<IAnalyticsService>();
        Assert.Equal(SelectionStrategyType.FanOut, analyticsStrategy.StrategyType);

        // Assert - Verify Resources gets PickOne by default
        var resourceStrategy = factory.CreateStrategy<IResourceService>();
        Assert.Equal(SelectionStrategyType.PickOne, resourceStrategy.StrategyType);

        // Assert - Verify AI gets PickOne by default
        var aiStrategy = factory.CreateStrategy<IAIService>();
        Assert.Equal(SelectionStrategyType.PickOne, aiStrategy.StrategyType);

        // Assert - Verify SceneFlow gets PickOne by default
        var sceneFlowStrategy = factory.CreateStrategy<ISceneFlowService>();
        Assert.Equal(SelectionStrategyType.PickOne, sceneFlowStrategy.StrategyType);
    }

    [Fact]
    public void End2End_AddSelectionStrategiesWithCustomConfiguration_ShouldRespectOverrides()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Add selection strategies with custom configuration
        services.AddSelectionStrategies(options =>
        {
            // Override Analytics to use PickOne instead of default FanOut
            options.SetCategoryDefault(ServiceCategory.Analytics, SelectionStrategyType.PickOne);

            // Override specific service to use Sharded
            options.UseStrategyFor<IResourceService>(SelectionStrategyType.Sharded);
        });

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<ISelectionStrategyFactory>();

        // Assert - Verify Analytics override is respected
        var analyticsStrategy = factory.CreateStrategy<IAnalyticsService>();
        Assert.Equal(SelectionStrategyType.PickOne, analyticsStrategy.StrategyType);

        // Assert - Verify specific service override is respected
        var resourceStrategy = factory.CreateStrategy<IResourceService>();
        Assert.Equal(SelectionStrategyType.Sharded, resourceStrategy.StrategyType);

        // Assert - Verify other services still use defaults
        var aiStrategy = factory.CreateStrategy<IAIService>();
        Assert.Equal(SelectionStrategyType.PickOne, aiStrategy.StrategyType);
    }

    [Fact]
    public void End2End_UseSpecificHelperMethods_ShouldSetCorrectStrategies()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Use specific helper methods for each service
        services.UsePickOneFor<IAnalyticsService>()  // Override default FanOut for Analytics
                .UseFanOutFor<IResourceService>()    // Override default PickOne for Resources
                .UseShardedFor<IAIService>();        // Override default PickOne for AI

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<ISelectionStrategyFactory>();

        // Assert - Verify each service uses the explicitly configured strategy
        var analyticsStrategy = factory.CreateStrategy<IAnalyticsService>();
        Assert.Equal(SelectionStrategyType.PickOne, analyticsStrategy.StrategyType);

        var resourceStrategy = factory.CreateStrategy<IResourceService>();
        Assert.Equal(SelectionStrategyType.FanOut, resourceStrategy.StrategyType);

        var aiStrategy = factory.CreateStrategy<IAIService>();
        Assert.Equal(SelectionStrategyType.Sharded, aiStrategy.StrategyType);
    }

    [Fact]
    public void End2End_CategoryInference_ShouldWorkWithVariousNamingPatterns()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSelectionStrategies();

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<ISelectionStrategyFactory>();

        // Act & Assert - Test various analytics-related naming patterns
        var metricsStrategy = factory.CreateStrategy<IMetricsService>();
        Assert.Equal(SelectionStrategyType.FanOut, metricsStrategy.StrategyType);

        var telemetryStrategy = factory.CreateStrategy<ITelemetryService>();
        Assert.Equal(SelectionStrategyType.FanOut, telemetryStrategy.StrategyType);

        var trackingStrategy = factory.CreateStrategy<ITrackingService>();
        Assert.Equal(SelectionStrategyType.FanOut, trackingStrategy.StrategyType);

        // Test AI-related patterns
        var mlStrategy = factory.CreateStrategy<IMLService>();
        Assert.Equal(SelectionStrategyType.PickOne, mlStrategy.StrategyType);

        var intelligenceStrategy = factory.CreateStrategy<IIntelligenceService>();
        Assert.Equal(SelectionStrategyType.PickOne, intelligenceStrategy.StrategyType);

        // Test resource-related patterns
        var dataStrategy = factory.CreateStrategy<IDataService>();
        Assert.Equal(SelectionStrategyType.PickOne, dataStrategy.StrategyType);

        var repositoryStrategy = factory.CreateStrategy<IRepositoryService>();
        Assert.Equal(SelectionStrategyType.PickOne, repositoryStrategy.StrategyType);

        // Test scene/narrative patterns
        var narrativeStrategy = factory.CreateStrategy<INarrativeService>();
        Assert.Equal(SelectionStrategyType.PickOne, narrativeStrategy.StrategyType);

        var storyStrategy = factory.CreateStrategy<IStoryService>();
        Assert.Equal(SelectionStrategyType.PickOne, storyStrategy.StrategyType);
    }

    // Test service interfaces for category inference
    public interface IAnalyticsService { void Track(string eventName); }
    public interface IResourceService { string LoadResource(string key); }
    public interface IAIService { string Predict(string input); }
    public interface ISceneFlowService { void AdvanceScene(); }
    public interface IMetricsService { void RecordMetric(string name, double value); }
    public interface ITelemetryService { void SendTelemetry(object data); }
    public interface ITrackingService { void TrackEvent(string eventName); }
    public interface IMLService { void Train(object data); }
    public interface IIntelligenceService { void Learn(); }
    public interface IDataService { T GetData<T>(string key); }
    public interface IRepositoryService { void Save(object entity); }
    public interface INarrativeService { void TellStory(); }
    public interface IStoryService { void ProgressStory(); }
}
