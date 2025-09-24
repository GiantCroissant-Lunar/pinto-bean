using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Integration tests for SelectionStrategyOptions DI and configuration binding.
/// </summary>
public class SelectionStrategyOptionsIntegrationTests
{
    [Fact]
    public void AddSelectionStrategies_WithoutParameters_ShouldRegisterIOptionsPattern()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSelectionStrategies();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<SelectionStrategyOptions>>();
        Assert.NotNull(options);
        Assert.NotNull(options.Value);

        var selectionOptions = serviceProvider.GetService<SelectionStrategyOptions>();
        Assert.NotNull(selectionOptions);
        Assert.Same(options.Value, selectionOptions);
    }

    [Fact]
    public void AddSelectionStrategies_WithConfiguration_ShouldBindFromConfiguration()
    {
        // Arrange
        var configurationData = new Dictionary<string, string?>
        {
            { "Analytics", "PickOne" },
            { "Resources", "FanOut" },
            { "SceneFlow", "Sharded" },
            { "AI", "FanOut" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddSelectionStrategies(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetRequiredService<SelectionStrategyOptions>();
        Assert.Equal(SelectionStrategyType.PickOne, options.GetDefaultForCategory(ServiceCategory.Analytics));
        Assert.Equal(SelectionStrategyType.FanOut, options.GetDefaultForCategory(ServiceCategory.Resources));
        Assert.Equal(SelectionStrategyType.Sharded, options.GetDefaultForCategory(ServiceCategory.SceneFlow));
        Assert.Equal(SelectionStrategyType.FanOut, options.GetDefaultForCategory(ServiceCategory.AI));
    }

    [Fact]
    public void AddSelectionStrategies_WithConfigurationSection_ShouldBindFromSection()
    {
        // Arrange
        var configurationData = new Dictionary<string, string?>
        {
            { "SelectionStrategies:Analytics", "Sharded" },
            { "SelectionStrategies:Resources", "PickOne" },
            { "SelectionStrategies:SceneFlow", "FanOut" },
            { "SelectionStrategies:AI", "PickOne" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddSelectionStrategies(configuration, "SelectionStrategies");
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetRequiredService<SelectionStrategyOptions>();
        Assert.Equal(SelectionStrategyType.Sharded, options.GetDefaultForCategory(ServiceCategory.Analytics));
        Assert.Equal(SelectionStrategyType.PickOne, options.GetDefaultForCategory(ServiceCategory.Resources));
        Assert.Equal(SelectionStrategyType.FanOut, options.GetDefaultForCategory(ServiceCategory.SceneFlow));
        Assert.Equal(SelectionStrategyType.PickOne, options.GetDefaultForCategory(ServiceCategory.AI));
    }

    [Fact]
    public void AddSelectionStrategies_ConfigurationBinding_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var configurationData = new Dictionary<string, string?>
        {
            { "Analytics", "Sharded" },
            { "Resources", "FanOut" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddSelectionStrategies(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetRequiredService<SelectionStrategyOptions>();
        
        // Verify properties are set correctly
        Assert.Equal(SelectionStrategyType.Sharded, options.Analytics);
        Assert.Equal(SelectionStrategyType.FanOut, options.Resources);
        
        // Verify unspecified properties keep RFC-0003 defaults
        Assert.Equal(SelectionStrategyType.PickOne, options.SceneFlow);
        Assert.Equal(SelectionStrategyType.PickOne, options.AI);
    }

    [Fact]
    public void AddSelectionStrategies_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            services.AddSelectionStrategies((IConfiguration)null!));
    }

    [Fact]
    public void AddSelectionStrategies_WithNullSectionName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            services.AddSelectionStrategies(configuration, null!));
    }

    [Fact]
    public void AddSelectionStrategies_IOptionsPattern_ShouldWorkWithIOptionsMonitor()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSelectionStrategies();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var optionsMonitor = serviceProvider.GetService<IOptionsMonitor<SelectionStrategyOptions>>();
        Assert.NotNull(optionsMonitor);
        Assert.NotNull(optionsMonitor.CurrentValue);
        
        // Verify RFC-0003 defaults are preserved
        var options = optionsMonitor.CurrentValue;
        Assert.Equal(SelectionStrategyType.FanOut, options.GetDefaultForCategory(ServiceCategory.Analytics));
        Assert.Equal(SelectionStrategyType.PickOne, options.GetDefaultForCategory(ServiceCategory.Resources));
        Assert.Equal(SelectionStrategyType.PickOne, options.GetDefaultForCategory(ServiceCategory.SceneFlow));
        Assert.Equal(SelectionStrategyType.PickOne, options.GetDefaultForCategory(ServiceCategory.AI));
    }

    [Fact]
    public void AddSelectionStrategies_IOptionsPattern_ShouldWorkWithExistingLegacyRegistration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Register using legacy method first, then IOptions pattern
        services.AddSelectionStrategies(options => 
            options.SetCategoryDefault(ServiceCategory.Analytics, SelectionStrategyType.Sharded));
        
        services.AddSelectionStrategies(); // Add IOptions pattern
        
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetRequiredService<SelectionStrategyOptions>();
        var iOptions = serviceProvider.GetRequiredService<IOptions<SelectionStrategyOptions>>();
        
        Assert.NotNull(options);
        Assert.NotNull(iOptions);
        
        // Both should resolve to same configured instance
        Assert.Same(options, iOptions.Value);
        Assert.Equal(SelectionStrategyType.Sharded, options.GetDefaultForCategory(ServiceCategory.Analytics));
    }

    [Fact]
    public void SelectionStrategyOptions_Properties_ShouldSupportGetAndSet()
    {
        // Arrange
        var options = new SelectionStrategyOptions();

        // Act & Assert - Test property setters and getters
        options.Analytics = SelectionStrategyType.Sharded;
        Assert.Equal(SelectionStrategyType.Sharded, options.Analytics);
        Assert.Equal(SelectionStrategyType.Sharded, options.GetDefaultForCategory(ServiceCategory.Analytics));

        options.Resources = SelectionStrategyType.FanOut;
        Assert.Equal(SelectionStrategyType.FanOut, options.Resources);
        Assert.Equal(SelectionStrategyType.FanOut, options.GetDefaultForCategory(ServiceCategory.Resources));

        options.SceneFlow = SelectionStrategyType.Sharded;
        Assert.Equal(SelectionStrategyType.Sharded, options.SceneFlow);
        Assert.Equal(SelectionStrategyType.Sharded, options.GetDefaultForCategory(ServiceCategory.SceneFlow));

        options.AI = SelectionStrategyType.FanOut;
        Assert.Equal(SelectionStrategyType.FanOut, options.AI);
        Assert.Equal(SelectionStrategyType.FanOut, options.GetDefaultForCategory(ServiceCategory.AI));
    }

    [Fact]
    public void SelectionStrategyOptions_Properties_ShouldStartWithRFC0003Defaults()
    {
        // Arrange & Act
        var options = new SelectionStrategyOptions();

        // Assert - Verify RFC-0003 defaults through properties
        Assert.Equal(SelectionStrategyType.FanOut, options.Analytics);
        Assert.Equal(SelectionStrategyType.PickOne, options.Resources);
        Assert.Equal(SelectionStrategyType.PickOne, options.SceneFlow);
        Assert.Equal(SelectionStrategyType.PickOne, options.AI);
    }
}