using System;
using System.Linq;
using Xunit;
using Yokan.PintoBean.Runtime.Unity;

namespace Yokan.PintoBean.Runtime.Tests.Unity;

/// <summary>
/// Tests for the ShardMapAsset class functionality.
/// Note: These tests don't involve actual Unity ScriptableObject serialization,
/// but test the core logic and data structures.
/// </summary>
public class ShardMapAssetTests
{
    [Fact]
    public void ToDictionary_WithValidMappings_ShouldReturnCorrectDictionary()
    {
        // Arrange
        var asset = CreateShardMapAsset();
        
        // Act
        var dictionary = asset.ToDictionary(logWarnings: false);
        
        // Assert
        Assert.NotNull(dictionary);
        Assert.Equal(3, dictionary.Count);
        Assert.Equal("PrimaryProvider", dictionary["player"]);
        Assert.Equal("SystemProvider", dictionary["system"]);
        Assert.Equal("DebugProvider", dictionary["debug"]);
    }

    [Fact]
    public void ToDictionary_WithEmptyMappings_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var asset = CreateEmptyShardMapAsset();
        
        // Act
        var dictionary = asset.ToDictionary(logWarnings: false);
        
        // Assert
        Assert.NotNull(dictionary);
        Assert.Empty(dictionary);
    }

    [Fact]
    public void ToDictionary_WithInvalidMappings_ShouldSkipInvalid()
    {
        // Arrange
        var asset = CreateShardMapAssetWithInvalidEntries();
        
        // Act
        var dictionary = asset.ToDictionary(logWarnings: false);
        
        // Assert
        Assert.NotNull(dictionary);
        Assert.Single(dictionary); // Only the valid mapping should be included
        Assert.Equal("ValidProvider", dictionary["valid"]);
    }

    [Fact]
    public void GetShardMappings_ShouldReturnReadOnlyDictionary()
    {
        // Arrange
        var asset = CreateShardMapAsset();
        
        // Act
        var mappings = asset.GetShardMappings(logWarnings: false);
        
        // Assert
        Assert.NotNull(mappings);
        Assert.IsAssignableFrom<System.Collections.Generic.IReadOnlyDictionary<string, string>>(mappings);
        Assert.Equal(3, mappings.Count);
    }

    [Fact]
    public void ShardMappings_ShouldReturnReadOnlyCollection()
    {
        // Arrange
        var asset = CreateShardMapAsset();
        
        // Act
        var mappings = asset.ShardMappings;
        
        // Assert
        Assert.NotNull(mappings);
        Assert.IsAssignableFrom<System.Collections.Generic.IReadOnlyList<ShardMapAsset.ShardMapping>>(mappings);
    }

    [Fact]
    public void Description_ShouldReturnNonNullString()
    {
        // Arrange
        var asset = CreateShardMapAsset();
        
        // Act
        var description = asset.Description;
        
        // Assert
        Assert.NotNull(description);
    }

    private static ShardMapAsset CreateShardMapAsset()
    {
        var asset = new TestShardMapAsset();
        asset.InitializeWithValidMappings();
        return asset;
    }

    private static ShardMapAsset CreateEmptyShardMapAsset()
    {
        var asset = new TestShardMapAsset();
        asset.InitializeEmpty();
        return asset;
    }

    private static ShardMapAsset CreateShardMapAssetWithInvalidEntries()
    {
        var asset = new TestShardMapAsset();
        asset.InitializeWithInvalidEntries();
        return asset;
    }

    /// <summary>
    /// Testable implementation of ShardMapAsset that doesn't require Unity.
    /// </summary>
    private class TestShardMapAsset : ShardMapAsset
    {
        private ShardMapAsset.ShardMapping[] _shardMappings = Array.Empty<ShardMapAsset.ShardMapping>();
        private string _description = "Test shard map asset";

        public override System.Collections.Generic.IReadOnlyList<ShardMapAsset.ShardMapping> ShardMappings => _shardMappings;
        public override string Description => _description;

        public void InitializeWithValidMappings()
        {
            _shardMappings = new[]
            {
                CreateShardMapping("player", "PrimaryProvider"),
                CreateShardMapping("system", "SystemProvider"),
                CreateShardMapping("debug", "DebugProvider")
            };
            _description = "Test shard map with valid mappings";
        }

        public void InitializeEmpty()
        {
            _shardMappings = Array.Empty<ShardMapAsset.ShardMapping>();
            _description = "Empty test shard map";
        }

        public void InitializeWithInvalidEntries()
        {
            _shardMappings = new[]
            {
                CreateShardMapping("", "EmptyKeyProvider"), // Invalid: empty key
                CreateShardMapping("nullProvider", ""), // Invalid: empty provider
                CreateShardMapping("", "NullKeyProvider"), // Invalid: null key (replaced null with empty string)
                CreateShardMapping("valid", "ValidProvider") // Valid entry
            };
            _description = "Test shard map with invalid entries";
        }

        private static ShardMapAsset.ShardMapping CreateShardMapping(string shardKey, string providerId)
        {
            var mapping = new ShardMapAsset.ShardMapping();
            // Use reflection to set private fields since they're serialized fields
            var keyField = typeof(ShardMapAsset.ShardMapping).GetField("shardKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var providerField = typeof(ShardMapAsset.ShardMapping).GetField("providerId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            keyField?.SetValue(mapping, shardKey);
            providerField?.SetValue(mapping, providerId);
            
            return mapping;
        }
    }
}