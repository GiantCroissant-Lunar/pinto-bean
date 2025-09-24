// Tier-3: Platform detection utility for runtime selection strategies

using System;
using System.Runtime.InteropServices;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Utility class for detecting the current runtime platform.
/// Used by selection strategies for platform-based filtering.
/// </summary>
public static class PlatformDetector
{
    private static Platform? _cachedCurrentPlatform;

    /// <summary>
    /// Gets the current runtime platform.
    /// </summary>
    public static Platform CurrentPlatform
    {
        get
        {
            if (_cachedCurrentPlatform.HasValue)
                return _cachedCurrentPlatform.Value;

            _cachedCurrentPlatform = DetectCurrentPlatform();
            return _cachedCurrentPlatform.Value;
        }
    }

    /// <summary>
    /// Determines if a provider's platform is compatible with the current platform.
    /// </summary>
    /// <param name="providerPlatform">The provider's target platform.</param>
    /// <returns>True if the provider is compatible with the current platform.</returns>
    public static bool IsCompatible(Platform providerPlatform)
    {
        // Platform.Any is always compatible
        if (providerPlatform == Platform.Any)
            return true;

        // Exact match
        if (providerPlatform == CurrentPlatform)
            return true;

        // Special compatibility rules
        return CurrentPlatform switch
        {
            Platform.Unity => providerPlatform == Platform.Any || providerPlatform == Platform.Unity,
            Platform.Godot => providerPlatform == Platform.Any || providerPlatform == Platform.Godot,
            Platform.DotNet => providerPlatform == Platform.Any || providerPlatform == Platform.DotNet,
            Platform.Web => providerPlatform == Platform.Any || providerPlatform == Platform.Web,
            Platform.Mobile => providerPlatform == Platform.Any || providerPlatform == Platform.Mobile,
            Platform.Desktop => providerPlatform == Platform.Any || providerPlatform == Platform.Desktop,
            _ => false
        };
    }

    private static Platform DetectCurrentPlatform()
    {
        // Check for Unity first (Unity defines UNITY_5_3_OR_NEWER or similar)
        if (IsUnityRuntime())
            return Platform.Unity;

        // Check for Godot (Godot defines specific assemblies)
        if (IsGodotRuntime())
            return Platform.Godot;

        // Check for web/browser environment
        if (IsWebRuntime())
            return Platform.Web;

        // Check for mobile platforms
        if (IsMobileRuntime())
            return Platform.Mobile;

        // Check for desktop platforms
        if (IsDesktopRuntime())
            return Platform.Desktop;

        // Default to .NET
        return Platform.DotNet;
    }

    private static bool IsUnityRuntime()
    {
        // Unity typically has specific assemblies or defines
        // For now, we'll use a simple heuristic based on loaded assemblies
        try
        {
            var unityEngineType = Type.GetType("UnityEngine.Application, UnityEngine");
            return unityEngineType != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsGodotRuntime()
    {
        // Godot has specific assemblies
        try
        {
            var godotType = Type.GetType("Godot.Engine, GodotSharp");
            return godotType != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsWebRuntime()
    {
        // Web assembly or browser detection
        return RuntimeInformation.ProcessArchitecture == Architecture.Wasm;
    }

    private static bool IsMobileRuntime()
    {
        // Mobile platform detection
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID")))
            return true;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("IOS")))
            return true;

        return false;
    }

    private static bool IsDesktopRuntime()
    {
        // Desktop platform detection
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
               RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
               RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }
}