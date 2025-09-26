# Unity Strategy Configuration Guide

This guide explains how to configure PintoBean selection strategies in Unity using ScriptableObject assets.

## Overview

Unity projects can configure strategy defaults (PickOne/FanOut/Sharded) via ScriptableObjects that are automatically imported at boot time. This provides a visual, designer-friendly way to configure complex strategy mappings without requiring code changes.

## Getting Started

### 1. Create Configuration Assets

Use the Unity menu to create default configuration assets:

**Menu: PintoBean → Create Default Strategy Config**

This creates:
- `Assets/Config/PintoBean/DefaultStrategyMapping.asset` - Strategy mappings
- `Assets/Config/PintoBean/DefaultAnalyticsShardMap.asset` - Shard mappings for Analytics

### 2. Configure Strategy Mappings

Select the `DefaultStrategyMapping.asset` and configure:

#### Category Defaults
- **Analytics**: `FanOut` (broadcast to all providers)
- **Resources**: `PickOne` (primary with fallbacks)
- **SceneFlow**: `PickOne` (deterministic flow)
- **AI**: `PickOne` (single provider selection)

#### Contract-Specific Overrides
Add specific service contracts that should use different strategies:

```
Contract Type Name: "MyGame.IPlayerAnalyticsService"
Strategy: Sharded
Category: Analytics
```

### 3. Configure Shard Mappings (Optional)

For `Sharded` strategies, configure explicit shard key mappings in `DefaultAnalyticsShardMap.asset`:

```
Shard Key: "player"     → Provider ID: "UnityAnalyticsProvider"
Shard Key: "system"     → Provider ID: "FirebaseAnalyticsProvider"  
Shard Key: "debug"      → Provider ID: "DebugAnalyticsProvider"
```

Keys not listed here will use consistent hashing.

### 4. Bootstrap Configuration

Add the `StrategyConfigBootstrap` component to a GameObject in your first scene:

```csharp
// Automatically imports strategy configuration on Start()
public class StrategyConfigBootstrap : MonoBehaviour
{
    [SerializeField] private bool forceReimport = false;
    [SerializeField] private bool verboseLogging = true;
    
    // Component handles import automatically on Start()
}
```

**OR** use manual service collection configuration:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Enable selection strategies with Unity ScriptableObject configuration
    services.AddSelectionStrategies()
            .AddUnityStrategyConfiguration();
}
```

## Asset Types

### StrategyMappingAsset

Defines strategy mappings for service contracts and categories.

**Properties:**
- **Contract Mappings**: Override strategy for specific service types
- **Category Mappings**: Set default strategy for service categories

**Menu:** `PintoBean/Strategy Mapping`

### ShardMapAsset  

Defines explicit shard key to provider mappings for Sharded strategies.

**Properties:**
- **Shard Mappings**: Array of key → provider ID mappings
- **Description**: Documentation for this shard map

**Menu:** `PintoBean/Shard Map`

## Example Configuration

### Strategy Mapping Example

```csharp
// This configuration would be set via ScriptableObject Inspector:

Category Mappings:
- Analytics: Sharded    (route by event type)
- Resources: PickOne    (primary/fallback pattern)
- AI: FanOut           (broadcast to multiple AI services)

Contract Mappings:  
- "MyGame.IPlayerStatsService": PickOne
- "MyGame.IDebugAnalyticsService": FanOut
```

### Shard Map Example

```csharp
// For Analytics events using Sharded strategy:

Shard Mappings:
- "player.*": "UnityAnalyticsProvider"     // player.level.complete, player.inventory.changed
- "system.*": "FirebaseAnalyticsProvider"  // system.startup, system.error
- "debug.*": "LocalAnalyticsProvider"      // debug.test.event

// Other event types use consistent hashing
```

### Generated Logs

When entering play mode with verbose logging enabled:

```
[StrategyConfigImporter] Starting strategy configuration import...
[StrategyMappingAsset] Applying strategy mappings from DefaultStrategyMapping
[StrategyMappingAsset] Set category Analytics default to Sharded
[StrategyMappingAsset] Set category AI default to FanOut
[StrategyMappingAsset] Set contract MyGame.IPlayerStatsService strategy to PickOne
[ShardMapAsset] Converting shard mappings from DefaultAnalyticsShardMap (3 entries)
[ShardMapAsset] Mapped shard key 'player' -> 'UnityAnalyticsProvider'
[ShardMapAsset] Mapped shard key 'system' -> 'FirebaseAnalyticsProvider'
[StrategyConfigImporter] Strategy configuration import completed successfully
```

## Advanced Usage

### Multiple Assets

You can create multiple `StrategyMappingAsset` and `ShardMapAsset` files. The importer will:
- Apply all `StrategyMappingAsset` files in sequence
- Combine all `ShardMapAsset` mappings (later assets override earlier ones for duplicate keys)

### Custom Shard Strategies

Use `StrategyConfigImporter` helper methods to create Sharded strategies with your explicit mappings:

```csharp
// Creates Analytics Sharded strategy with explicit mappings from all ShardMapAssets
var analyticsStrategy = StrategyConfigImporter.CreateAnalyticsShardedStrategy<IAnalyticsService>(registry);

// Creates custom Sharded strategy with explicit mappings  
var customStrategy = StrategyConfigImporter.CreateShardedStrategy<ICustomService>(
    metadata => ExtractCustomShardKey(metadata), 
    registry);
```

### Integration with Dependency Injection

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Register service providers
    services.AddSingleton<IAnalyticsService, UnityAnalyticsService>();
    services.AddSingleton<IAnalyticsService, FirebaseAnalyticsService>();
    
    // Configure strategies with Unity ScriptableObject assets
    services.AddSelectionStrategies()
            .AddUnityStrategyConfiguration(forceReimport: false);
            
    // Service resolution will now use your configured strategies
}
```

## File Locations

Place ScriptableObject assets in `Resources` folders so they can be loaded at runtime:

```
Assets/
├── Config/PintoBean/          # Recommended location
│   ├── DefaultStrategyMapping.asset
│   └── DefaultAnalyticsShardMap.asset
├── Resources/                 # Alternative: Direct in Resources
│   ├── StrategyMapping.asset
│   └── ShardMap.asset
└── MyProject/
    └── Resources/Config/      # Alternative: Project-specific Resources subfolder
        ├── StrategyConfig.asset
        └── AnalyticsShards.asset
```

## Error Handling

The system gracefully handles missing or invalid configurations:

- **No assets found**: Uses RFC-0003 defaults (Analytics: FanOut, others: PickOne)
- **Invalid contract types**: Logs warnings, skips invalid entries
- **Duplicate shard keys**: Later mappings override earlier ones with warnings
- **Empty/null values**: Skipped with optional warnings

All errors are logged to Unity Console with `[StrategyConfigImporter]` prefix.