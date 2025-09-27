# P6-07: Unity Analytics + Resources Sample

## Overview

This sample demonstrates a Unity scene with a MonoBehaviour that integrates both Analytics and Resource management via the PintoBean DI bridge. It showcases the difference between FanOut and Sharded routing strategies for Analytics while also demonstrating Resource loading via PickOne strategy.

## Key Components

### AnalyticsResourcesSample.cs
MonoBehaviour that demonstrates:
- **Analytics Events**: Sends 2 different types of analytics events on Start()
  - Player event (`player.level.start`) 
  - System event (`system.performance.metrics`)
- **Resource Loading**: Loads a test resource (`unity-test-config.json`)
- **Logging**: Outputs results via Debug.Log/Console.WriteLine showing routing behavior

### ScriptableObject Assets

#### FanOutStrategyConfig.asset
Configures Analytics to use **FanOut strategy**:
- Events are sent to ALL registered Analytics providers
- Demonstrates dual-send behavior (Unity + Firebase)

#### ShardedStrategyConfig.asset  
Configures Analytics to use **Sharded strategy**:
- Events are routed to specific providers based on event name prefix
- Uses AnalyticsShardMap.asset for routing rules

#### AnalyticsShardMap.asset
Defines explicit shard routing:
- `player.*` events → Unity Analytics
- `system.*` events → Firebase Analytics
- Other events use consistent hashing

## Expected Behavior

### FanOut Strategy (Default)
When using FanOutStrategyConfig.asset:
```
📤 Sending player event: player.level.start
[Unity Analytics] Tracking event 'player.level.start' with 3 properties for user unity-player-001
[Firebase Analytics] Event: player.level.start [level, character, timestamp] | User: unity-player-001 | Session: unity-session-123

📤 Sending system event: system.performance.metrics  
[Unity Analytics] Tracking event 'system.performance.metrics' with 4 properties
[Firebase Analytics] Event: system.performance.metrics [fps, memory_mb, cpu_usage, timestamp]
```

### Sharded Strategy
When using ShardedStrategyConfig.asset + AnalyticsShardMap.asset:
```
📤 Sending player event: player.level.start
[Unity Analytics] Tracking event 'player.level.start' with 3 properties for user unity-player-001

📤 Sending system event: system.performance.metrics
[Firebase Analytics] Event: system.performance.metrics [fps, memory_mb, cpu_usage, timestamp]
```

### Resource Loading
Both configurations show:
```
📁 Loading resource: unity-test-config.json
✅ Resource loaded successfully from: CacheStore
📄 Resource content preview: {"resourceKey": "unity-test-config.json", "source": "cache"...
```

## Usage in Unity

1. Add `AnalyticsResourcesSample` script to a GameObject in your scene
2. Configure strategy by placing either:
   - `FanOutStrategyConfig.asset` in Resources folder (for FanOut behavior)
   - `ShardedStrategyConfig.asset` + `AnalyticsShardMap.asset` (for Sharded behavior)
3. Enter Play mode to see the demonstration

## Key Insights

- **FanOut**: Broadcasts to all providers (redundant delivery)
- **Sharded**: Routes based on business logic (targeted delivery)
- **Resources**: Uses PickOne with priority-based fallback
- **DI Bridge**: Seamless integration between Unity and PintoBean services

This sample validates the complete integration of Analytics façades with Resource management in a Unity environment.