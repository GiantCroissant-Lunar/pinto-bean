using System;
using System.Collections.Generic;
using Yokan.PintoBean.Runtime;
using Yokan.PintoBean.Runtime.Unity;

namespace PintoBean.Unity.StrategyDemo.Console;

/// <summary>
/// Demonstrates the Unity ScriptableObject strategy mapping functionality.
/// This shows how StrategyMappingAsset and ShardMapAsset work together
/// to configure PintoBean selection strategies.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        System.Console.WriteLine("=== PintoBean Unity Strategy Configuration Demo ===");
        System.Console.WriteLine();

        // Create a SelectionStrategyOptions instance (normally created by DI container)
        var options = new SelectionStrategyOptions();

        System.Console.WriteLine("Initial configuration (RFC-0003 defaults):");
        LogCurrentConfiguration(options);
        System.Console.WriteLine();

        // Demonstrate StrategyMappingAsset functionality
        System.Console.WriteLine("1. Creating and applying StrategyMappingAsset:");
        DemonstrateStrategyMapping(options);
        System.Console.WriteLine();

        // Demonstrate ShardMapAsset functionality
        System.Console.WriteLine("2. Creating and applying ShardMapAsset:");
        DemonstrateShardMapping();
        System.Console.WriteLine();

        // Show combined result
        System.Console.WriteLine("Final configuration after applying Unity assets:");
        LogCurrentConfiguration(options);
        System.Console.WriteLine();

        System.Console.WriteLine("Demo completed successfully!");
        System.Console.WriteLine();
        System.Console.WriteLine("In a Unity project, these assets would be:");
        System.Console.WriteLine("- Created via 'PintoBean/Create Default Strategy Config' menu");
        System.Console.WriteLine("- Loaded automatically at startup via StrategyConfigImporter");
        System.Console.WriteLine("- Applied to SelectionOptions via service collection extensions");
    }

    private static void DemonstrateStrategyMapping(SelectionStrategyOptions options)
    {
        // Create a test strategy mapping asset
        var strategyAsset = new TestStrategyMappingAsset();
        
        // Configure it with some example mappings
        strategyAsset.SetupExampleMappings();

        System.Console.WriteLine($"  Created StrategyMappingAsset with:");
        System.Console.WriteLine($"    - {strategyAsset.CategoryMappings.Count} category mappings");
        System.Console.WriteLine($"    - {strategyAsset.ContractMappings.Count} contract mappings");

        // Apply to options
        strategyAsset.ApplyToOptions(options, logWarnings: false);
        System.Console.WriteLine("  Applied mappings to SelectionStrategyOptions");
    }

    private static void DemonstrateShardMapping()
    {
        // Create a test shard map asset
        var shardAsset = new TestShardMapAsset();
        
        // Configure it with some example mappings
        shardAsset.SetupExampleMappings();

        System.Console.WriteLine($"  Created ShardMapAsset with:");
        System.Console.WriteLine($"    - {shardAsset.ShardMappings.Count} shard mappings");

        var shardDict = shardAsset.ToDictionary(logWarnings: false);
        foreach (var kvp in shardDict)
        {
            System.Console.WriteLine($"      '{kvp.Key}' -> '{kvp.Value}'");
        }
    }

    private static void LogCurrentConfiguration(SelectionStrategyOptions options)
    {
        System.Console.WriteLine($"  Analytics: {options.Analytics}");
        System.Console.WriteLine($"  Resources: {options.Resources}");
        System.Console.WriteLine($"  SceneFlow: {options.SceneFlow}");
        System.Console.WriteLine($"  AI: {options.AI}");
    }

    /// <summary>
    /// Test implementation of StrategyMappingAsset for demonstration purposes.
    /// </summary>
    private class TestStrategyMappingAsset : StrategyMappingAsset
    {
        private readonly List<CategoryStrategyMapping> _categoryMappings = new();
        private readonly List<ContractStrategyMapping> _contractMappings = new();

        public override IReadOnlyList<CategoryStrategyMapping> CategoryMappings => _categoryMappings;
        public override IReadOnlyList<ContractStrategyMapping> ContractMappings => _contractMappings;

        public void SetupExampleMappings()
        {
            // Override some defaults
            _categoryMappings.Add(new CategoryStrategyMapping(ServiceCategory.Analytics, SelectionStrategyType.Sharded));
            _categoryMappings.Add(new CategoryStrategyMapping(ServiceCategory.AI, SelectionStrategyType.FanOut));

            // Add some contract-specific overrides
            _contractMappings.Add(new ContractStrategyMapping("MyProject.ISpecialAnalyticsService", SelectionStrategyType.PickOne, ServiceCategory.Analytics));
        }
    }

    /// <summary>
    /// Test implementation of ShardMapAsset for demonstration purposes.
    /// </summary>
    private class TestShardMapAsset : ShardMapAsset
    {
        private readonly List<ShardMapping> _shardMappings = new();

        public override IReadOnlyList<ShardMapping> ShardMappings => _shardMappings;
        public override string Description => "Example shard mappings for analytics events";

        public void SetupExampleMappings()
        {
            _shardMappings.Add(new ShardMapping("player", "PlayerAnalyticsProvider"));
            _shardMappings.Add(new ShardMapping("system", "SystemAnalyticsProvider"));
            _shardMappings.Add(new ShardMapping("debug", "DebugAnalyticsProvider"));
        }
    }
}