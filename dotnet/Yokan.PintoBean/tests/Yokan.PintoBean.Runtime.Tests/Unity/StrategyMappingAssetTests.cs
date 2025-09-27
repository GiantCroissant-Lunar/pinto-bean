using System;
using System.Linq;
using Xunit;
using Yokan.PintoBean.Runtime;
using Yokan.PintoBean.Runtime.Unity;

namespace Yokan.PintoBean.Runtime.Tests.Unity;

/// <summary>
/// Tests for the StrategyMappingAsset class functionality.
/// Note: These tests don't involve actual Unity ScriptableObject serialization,
/// but test the core logic and data structures.
/// </summary>
public class StrategyMappingAssetTests
{
    [Fact]
    public void ApplyToOptions_WithValidCategoryMappings_ShouldUpdateOptions()
    {
        // Arrange
        var asset = CreateStrategyMappingAsset();
        var options = new SelectionStrategyOptions();
        
        // Act
        asset.ApplyToOptions(options, logWarnings: false);
        
        // Assert
        Assert.Equal(SelectionStrategyType.FanOut, options.GetDefaultForCategory(ServiceCategory.Analytics));
        Assert.Equal(SelectionStrategyType.PickOne, options.GetDefaultForCategory(ServiceCategory.Resources));
        Assert.Equal(SelectionStrategyType.PickOne, options.GetDefaultForCategory(ServiceCategory.SceneFlow));
        Assert.Equal(SelectionStrategyType.PickOne, options.GetDefaultForCategory(ServiceCategory.AI));
    }

    [Fact]
    public void ApplyToOptions_WithContractMappings_ShouldSetOverrides()
    {
        // Arrange
        var asset = CreateStrategyMappingAssetWithContracts();
        var options = new SelectionStrategyOptions();
        
        // Act
        asset.ApplyToOptions(options, logWarnings: false);
        
        // Assert
        var testServiceOverride = options.GetStrategyOverride(typeof(ITestService));
        Assert.Equal(SelectionStrategyType.Sharded, testServiceOverride);
    }

    [Fact]
    public void ApplyToOptions_WithNullOptions_ShouldNotThrow()
    {
        // Arrange
        var asset = CreateStrategyMappingAsset();
        
        // Act & Assert
        asset.ApplyToOptions(null!, logWarnings: false); // Should not throw
    }

    [Fact]
    public void ContractMappings_ShouldReturnReadOnlyCollection()
    {
        // Arrange
        var asset = CreateStrategyMappingAsset();
        
        // Act
        var mappings = asset.ContractMappings;
        
        // Assert
        Assert.NotNull(mappings);
        Assert.IsAssignableFrom<System.Collections.Generic.IReadOnlyList<StrategyMappingAsset.ContractStrategyMapping>>(mappings);
    }

    [Fact]
    public void CategoryMappings_ShouldReturnReadOnlyCollection()
    {
        // Arrange
        var asset = CreateStrategyMappingAsset();
        
        // Act
        var mappings = asset.CategoryMappings;
        
        // Assert
        Assert.NotNull(mappings);
        Assert.IsAssignableFrom<System.Collections.Generic.IReadOnlyList<StrategyMappingAsset.CategoryStrategyMapping>>(mappings);
        Assert.Equal(4, mappings.Count); // Should have all 4 categories by default
    }

    private static StrategyMappingAsset CreateStrategyMappingAsset()
    {
        // Create a mock asset with default category mappings
        var asset = new TestStrategyMappingAsset();
        asset.InitializeWithDefaults();
        return asset;
    }

    private static StrategyMappingAsset CreateStrategyMappingAssetWithContracts()
    {
        // Create a mock asset with contract mappings
        var asset = new TestStrategyMappingAsset();
        asset.InitializeWithContracts();
        return asset;
    }

    /// <summary>
    /// Test interface for contract mapping tests.
    /// </summary>
    public interface ITestService
    {
        void DoSomething();
    }

    /// <summary>
    /// Testable implementation of StrategyMappingAsset that doesn't require Unity.
    /// </summary>
    private class TestStrategyMappingAsset : StrategyMappingAsset
    {
        private StrategyMappingAsset.ContractStrategyMapping[] _contractMappings = Array.Empty<StrategyMappingAsset.ContractStrategyMapping>();
        private StrategyMappingAsset.CategoryStrategyMapping[] _categoryMappings = Array.Empty<StrategyMappingAsset.CategoryStrategyMapping>();

        public override System.Collections.Generic.IReadOnlyList<StrategyMappingAsset.ContractStrategyMapping> ContractMappings => _contractMappings;
        public override System.Collections.Generic.IReadOnlyList<StrategyMappingAsset.CategoryStrategyMapping> CategoryMappings => _categoryMappings;

        public void InitializeWithDefaults()
        {
            _categoryMappings = new[]
            {
                CreateCategoryMapping(ServiceCategory.Analytics, SelectionStrategyType.FanOut),
                CreateCategoryMapping(ServiceCategory.Resources, SelectionStrategyType.PickOne),
                CreateCategoryMapping(ServiceCategory.SceneFlow, SelectionStrategyType.PickOne),
                CreateCategoryMapping(ServiceCategory.AI, SelectionStrategyType.PickOne)
            };
        }

        public void InitializeWithContracts()
        {
            InitializeWithDefaults();
            _contractMappings = new[]
            {
                CreateContractMapping(typeof(ITestService).FullName!, SelectionStrategyType.Sharded, ServiceCategory.Analytics)
            };
        }

        private static StrategyMappingAsset.CategoryStrategyMapping CreateCategoryMapping(ServiceCategory category, SelectionStrategyType strategy)
        {
            var mapping = new StrategyMappingAsset.CategoryStrategyMapping();
            // Use reflection to set private fields since they're serialized fields
            var categoryField = typeof(StrategyMappingAsset.CategoryStrategyMapping).GetField("category", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var strategyField = typeof(StrategyMappingAsset.CategoryStrategyMapping).GetField("strategy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            categoryField?.SetValue(mapping, category);
            strategyField?.SetValue(mapping, strategy);
            
            return mapping;
        }

        private static StrategyMappingAsset.ContractStrategyMapping CreateContractMapping(string contractTypeName, SelectionStrategyType strategy, ServiceCategory category)
        {
            var mapping = new StrategyMappingAsset.ContractStrategyMapping();
            // Use reflection to set private fields since they're serialized fields
            var typeNameField = typeof(StrategyMappingAsset.ContractStrategyMapping).GetField("contractTypeName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var strategyField = typeof(StrategyMappingAsset.ContractStrategyMapping).GetField("strategy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var categoryField = typeof(StrategyMappingAsset.ContractStrategyMapping).GetField("category", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            typeNameField?.SetValue(mapping, contractTypeName);
            strategyField?.SetValue(mapping, strategy);
            categoryField?.SetValue(mapping, category);
            
            return mapping;
        }
    }
}