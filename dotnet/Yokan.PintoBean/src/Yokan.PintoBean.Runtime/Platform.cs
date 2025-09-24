// Tier-3: Platform enumeration for provider registration and selection

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Defines the target platform for service providers.
/// Used for platform-specific provider selection and filtering.
/// </summary>
public enum Platform
{
    /// <summary>
    /// Any platform - the provider supports all platforms.
    /// </summary>
    Any = 0,

    /// <summary>
    /// Unity game engine platform.
    /// </summary>
    Unity = 1,

    /// <summary>
    /// Godot game engine platform.
    /// </summary>
    Godot = 2,

    /// <summary>
    /// Generic .NET host platform.
    /// </summary>
    DotNet = 3,

    /// <summary>
    /// Web/browser platform.
    /// </summary>
    Web = 4,

    /// <summary>
    /// Mobile platforms (iOS, Android).
    /// </summary>
    Mobile = 5,

    /// <summary>
    /// Desktop platforms (Windows, macOS, Linux).
    /// </summary>
    Desktop = 6
}
