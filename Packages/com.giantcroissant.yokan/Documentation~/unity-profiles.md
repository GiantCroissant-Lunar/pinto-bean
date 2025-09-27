# Unity Game vs Editor Profiles (P6-04)

This document explains how to use the Unity Game and Editor profile system to configure different PintoBean settings based on Unity's runtime mode.

## Overview

PintoBean supports different configuration profiles for Unity's two primary modes:

- **Game Profile**: Used when `Application.isPlaying = true` (Play mode in Editor, or built game)
- **Editor Profile**: Used when `Application.isEditor = true && Application.isPlaying = false` (Edit mode in Unity Editor)

This allows you to optimize settings for development vs production scenarios.

## Profile Assets

### GameProfileAsset

The Game profile is optimized for production gameplay scenarios:

- **Higher timeouts**: Default 30s for network operations
- **More retries**: 3 retry attempts for resilience
- **FanOut analytics**: Send to multiple analytics backends
- **Lower sampling**: 10% performance metric sampling to reduce overhead

### EditorProfileAsset  

The Editor profile is optimized for development and testing:

- **Shorter timeouts**: Default 5s for quick failure detection
- **Fewer retries**: 1 retry attempt for faster iteration
- **PickOne strategies**: Predictable single-provider selection for testing
- **Higher sampling**: 100% performance metric sampling for better diagnostics

## Creating Profile Assets

Use Unity's menu system to create profile assets:

1. **Right-click in Project window** → Create → PintoBean → Game Profile
2. **Right-click in Project window** → Create → PintoBean → Editor Profile

Or use the main menu:

- **PintoBean** → Create Game Profile Asset
- **PintoBean** → Create Editor Profile Asset

## Configuring StrategyConfigBootstrap

Add the `StrategyConfigBootstrap` component to a GameObject in your scene:

```csharp
// The component will automatically detect Unity mode and apply the appropriate profile
[SerializeField] private GameProfileAsset gameProfile;
[SerializeField] private EditorProfileAsset editorProfile;
```

1. **Drag your Game profile asset** to the "Game Profile" field
2. **Drag your Editor profile asset** to the "Editor Profile" field
3. **Enable "Verbose Logging"** to see profile selection in the console

## Mode Detection Logic

The bootstrap uses Unity's Application class to detect the current mode:

```csharp
bool isEditorMode = Application.isEditor && !Application.isPlaying;
```

- **Editor Mode**: Unity Editor with Play button NOT pressed
- **Game Mode**: Unity Editor with Play button pressed, OR built application

## Profile Settings

### Selection Strategy Settings

| Setting | Game Profile | Editor Profile | Reason |
|---------|-------------|----------------|---------|
| Analytics | FanOut | PickOne | Game needs multiple backends, Editor needs predictable testing |
| Resources | PickOne | PickOne | Both use primary-with-fallback pattern |
| SceneFlow | PickOne | PickOne | Both need deterministic flow control |
| AI | PickOne | PickOne | Both use router-decides-backend pattern |

### Resilience Settings

| Setting | Game Profile | Editor Profile | Reason |
|---------|-------------|----------------|---------|
| Default Timeout | 30s | 5s | Game tolerates slower networks, Editor needs fast feedback |
| Max Retries | 3 | 1 | Game prioritizes resilience, Editor prioritizes speed |
| Base Retry Delay | 1000ms | 100ms | Game can wait longer, Editor needs quick iteration |
| Circuit Breaker | Optional | Disabled | Game may use for protection, Editor doesn't need complexity |

### Category-Specific Timeouts

| Category | Game Profile | Editor Profile |
|----------|-------------|----------------|
| Analytics | 10s | 2s |
| Resources | 15s | 3s |
| SceneFlow | 5s | 2s |
| AI | 20s | 4s |

### Sampling Rates

| Profile | Sampling Rate | Use Case |
|---------|--------------|----------|
| Game | 10% | Reduced overhead in production |
| Editor | 100% | Full diagnostics for development |

## Logging Output

When the bootstrap runs, you'll see logs like:

```
[StrategyConfigBootstrap] Detected Unity mode: Editor, Selected profile: MyEditorProfile
[EditorProfileAsset] Applying Editor profile settings from MyEditorProfile
[EditorProfileAsset] Applied strategy settings: Analytics=PickOne, Resources=PickOne, SceneFlow=PickOne, AI=PickOne
[EditorProfileAsset] Applied resilience settings: Timeout=5s, Retries=1, CircuitBreaker=False
```

Or in Play mode:

```
[StrategyConfigBootstrap] Detected Unity mode: Play, Selected profile: MyGameProfile  
[GameProfileAsset] Applying Game profile settings from MyGameProfile
[GameProfileAsset] Applied strategy settings: Analytics=FanOut, Resources=PickOne, SceneFlow=PickOne, AI=PickOne
[GameProfileAsset] Applied resilience settings: Timeout=30s, Retries=3, CircuitBreaker=False
```

## Best Practices

### Profile Configuration

1. **Create separate profiles** for each environment (Development, Staging, Production)
2. **Use descriptive names** like "DevelopmentEditor" and "ProductionGame"
3. **Document your timeout choices** in the profile's Description field
4. **Test profile switching** by toggling Play mode in the Editor

### Asset Organization

```
Assets/
├── Settings/
│   └── PintoBean/
│       ├── DevelopmentEditor.asset
│       ├── DevelopmentGame.asset
│       ├── ProductionGame.asset
│       └── StrategyMapping.asset
```

### Performance Considerations

1. **Game profiles should have lower sampling rates** to reduce performance overhead
2. **Editor profiles can have higher sampling rates** for better diagnostics
3. **Consider shorter timeouts in Editor** for faster iteration cycles
4. **Use fewer retries in Editor** to fail fast during development

## Example Usage

```csharp
public class GameManager : MonoBehaviour
{
    [SerializeField] private StrategyConfigBootstrap bootstrap;
    
    void Start()
    {
        // The bootstrap automatically applies the correct profile
        // based on Unity's current mode
        
        Debug.Log($"Current Unity mode: {bootstrap.DetectedMode}");
        Debug.Log($"Selected profile: {bootstrap.SelectedProfile}");
        
        // Your game logic here...
    }
}
```

## Integration with Existing Systems

The profile system integrates seamlessly with existing PintoBean features:

- **Strategy Mapping Assets**: Applied after profile settings
- **Shard Map Assets**: Work with any selection strategy from profiles  
- **Service Registration**: Uses profile-configured strategies automatically
- **Resilience Patterns**: Respect profile timeout and retry settings

This allows you to layer configuration appropriately:
1. **Profiles** set the base mode-specific settings
2. **Strategy mappings** override specific service contracts
3. **Service registration** uses the final computed settings