using System;
using System.Collections.Generic;
using Yokan.PintoBean.Runtime;
using Yokan.PintoBean.Runtime.Unity;

namespace PintoBean.Unity.StrategyDemo.Console;

/// <summary>
/// Demonstrates the Unity Game vs Editor profile functionality.
/// Shows how GameProfileAsset and EditorProfileAsset work to configure
/// different settings based on Unity mode detection.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        System.Console.WriteLine("=== PintoBean Unity Profile Configuration Demo (P6-04) ===");
        System.Console.WriteLine();

        // Create options instances (normally created by DI container)
        var selectionOptions = new SelectionStrategyOptions();
        var resilienceOptions = new PollyResilienceExecutorOptions();

        System.Console.WriteLine("Initial configuration (RFC-0003 defaults):");
        LogCurrentConfiguration(selectionOptions, resilienceOptions);
        System.Console.WriteLine();

        // Demonstrate Game profile
        System.Console.WriteLine("1. Applying Game Profile (Play mode simulation):");
        DemonstrateGameProfile(selectionOptions, resilienceOptions);
        System.Console.WriteLine();

        // Reset to defaults
        selectionOptions = new SelectionStrategyOptions();
        resilienceOptions = new PollyResilienceExecutorOptions();

        // Demonstrate Editor profile
        System.Console.WriteLine("2. Applying Editor Profile (Editor mode simulation):");
        DemonstrateEditorProfile(selectionOptions, resilienceOptions);
        System.Console.WriteLine();

        System.Console.WriteLine("Demo completed successfully!");
        System.Console.WriteLine();
        System.Console.WriteLine("In Unity, the StrategyConfigBootstrap component would:");
        System.Console.WriteLine("- Detect Application.isEditor && !Application.isPlaying");
        System.Console.WriteLine("- Select EditorProfileAsset for Editor, GameProfileAsset for Play");
        System.Console.WriteLine("- Apply profile settings to DI container options at startup");
        System.Console.WriteLine("- Log the selected profile and mode for verification");
    }

    private static void DemonstrateGameProfile(SelectionStrategyOptions selectionOptions, PollyResilienceExecutorOptions resilienceOptions)
    {
        // Create a test Game profile asset
        var gameProfile = new TestGameProfileAsset();
        
        System.Console.WriteLine($"  Created GameProfileAsset: {gameProfile.Description}");
        System.Console.WriteLine($"  Profile settings:");
        System.Console.WriteLine($"    - Analytics Strategy: {gameProfile.AnalyticsStrategy}");
        System.Console.WriteLine($"    - Default Timeout: {gameProfile.DefaultTimeoutSeconds}s");
        System.Console.WriteLine($"    - Max Retries: {gameProfile.MaxRetryAttempts}");
        System.Console.WriteLine($"    - Circuit Breaker: {gameProfile.EnableCircuitBreaker}");
        System.Console.WriteLine($"    - Sampling Rate: {gameProfile.SamplingRate:P0}");

        // Apply to options
        gameProfile.ApplyToSelectionOptions(selectionOptions);
        gameProfile.ApplyToResilienceOptions(resilienceOptions);
        System.Console.WriteLine("  ✓ Applied Game profile settings");

        System.Console.WriteLine("  Result:");
        LogCurrentConfiguration(selectionOptions, resilienceOptions);
    }

    private static void DemonstrateEditorProfile(SelectionStrategyOptions selectionOptions, PollyResilienceExecutorOptions resilienceOptions)
    {
        // Create a test Editor profile asset
        var editorProfile = new TestEditorProfileAsset();
        
        System.Console.WriteLine($"  Created EditorProfileAsset: {editorProfile.Description}");
        System.Console.WriteLine($"  Profile settings:");
        System.Console.WriteLine($"    - Analytics Strategy: {editorProfile.AnalyticsStrategy}");
        System.Console.WriteLine($"    - Default Timeout: {editorProfile.DefaultTimeoutSeconds}s");
        System.Console.WriteLine($"    - Max Retries: {editorProfile.MaxRetryAttempts}");
        System.Console.WriteLine($"    - Circuit Breaker: {editorProfile.EnableCircuitBreaker}");
        System.Console.WriteLine($"    - Sampling Rate: {editorProfile.SamplingRate:P0}");

        // Apply to options
        editorProfile.ApplyToSelectionOptions(selectionOptions);
        editorProfile.ApplyToResilienceOptions(resilienceOptions);
        System.Console.WriteLine("  ✓ Applied Editor profile settings");

        System.Console.WriteLine("  Result:");
        LogCurrentConfiguration(selectionOptions, resilienceOptions);
    }

    private static void LogCurrentConfiguration(SelectionStrategyOptions selectionOptions, PollyResilienceExecutorOptions resilienceOptions)
    {
        System.Console.WriteLine($"  Selection Strategies:");
        System.Console.WriteLine($"    Analytics: {selectionOptions.Analytics}");
        System.Console.WriteLine($"    Resources: {selectionOptions.Resources}");
        System.Console.WriteLine($"    SceneFlow: {selectionOptions.SceneFlow}");
        System.Console.WriteLine($"    AI: {selectionOptions.AI}");
        System.Console.WriteLine($"  Resilience Settings:");
        System.Console.WriteLine($"    Default Timeout: {resilienceOptions.DefaultTimeoutSeconds}s");
        System.Console.WriteLine($"    Max Retries: {resilienceOptions.MaxRetryAttempts}");
        System.Console.WriteLine($"    Circuit Breaker: {resilienceOptions.EnableCircuitBreaker}");
        System.Console.WriteLine($"    Analytics Timeout: {resilienceOptions.GetTimeoutSeconds(null, "Analytics")}s");
    }

    /// <summary>
    /// Test implementation that simulates Game profile behavior for demonstration purposes.
    /// </summary>
    private class TestGameProfileAsset
    {
        public SelectionStrategyType AnalyticsStrategy => SelectionStrategyType.FanOut;
        public SelectionStrategyType ResourcesStrategy => SelectionStrategyType.PickOne;
        public SelectionStrategyType SceneFlowStrategy => SelectionStrategyType.PickOne;
        public SelectionStrategyType AIStrategy => SelectionStrategyType.PickOne;
        
        public double DefaultTimeoutSeconds => 30.0;
        public int MaxRetryAttempts => 3;
        public double BaseRetryDelayMilliseconds => 1000.0;
        public bool EnableCircuitBreaker => false;
        
        public double AnalyticsTimeoutSeconds => 10.0;
        public double ResourcesTimeoutSeconds => 15.0;
        public double SceneFlowTimeoutSeconds => 5.0;
        public double AITimeoutSeconds => 20.0;
        
        public float SamplingRate => 0.1f;
        public string Description => "Game mode profile with robust settings for production gameplay";

        public void ApplyToSelectionOptions(SelectionStrategyOptions options)
        {
            options.SetCategoryDefault(ServiceCategory.Analytics, AnalyticsStrategy);
            options.SetCategoryDefault(ServiceCategory.Resources, ResourcesStrategy);
            options.SetCategoryDefault(ServiceCategory.SceneFlow, SceneFlowStrategy);
            options.SetCategoryDefault(ServiceCategory.AI, AIStrategy);
        }

        public void ApplyToResilienceOptions(PollyResilienceExecutorOptions resilienceOptions)
        {
            resilienceOptions.DefaultTimeoutSeconds = DefaultTimeoutSeconds;
            resilienceOptions.MaxRetryAttempts = MaxRetryAttempts;
            resilienceOptions.BaseRetryDelayMilliseconds = BaseRetryDelayMilliseconds;
            resilienceOptions.EnableCircuitBreaker = EnableCircuitBreaker;

            resilienceOptions.SetCategoryTimeout(ServiceCategory.Analytics, AnalyticsTimeoutSeconds);
            resilienceOptions.SetCategoryTimeout(ServiceCategory.Resources, ResourcesTimeoutSeconds);
            resilienceOptions.SetCategoryTimeout(ServiceCategory.SceneFlow, SceneFlowTimeoutSeconds);
            resilienceOptions.SetCategoryTimeout(ServiceCategory.AI, AITimeoutSeconds);
        }
    }

    /// <summary>
    /// Test implementation that simulates Editor profile behavior for demonstration purposes.
    /// </summary>
    private class TestEditorProfileAsset
    {
        public SelectionStrategyType AnalyticsStrategy => SelectionStrategyType.PickOne;
        public SelectionStrategyType ResourcesStrategy => SelectionStrategyType.PickOne;
        public SelectionStrategyType SceneFlowStrategy => SelectionStrategyType.PickOne;
        public SelectionStrategyType AIStrategy => SelectionStrategyType.PickOne;
        
        public double DefaultTimeoutSeconds => 5.0;
        public int MaxRetryAttempts => 1;
        public double BaseRetryDelayMilliseconds => 100.0;
        public bool EnableCircuitBreaker => false;
        
        public double AnalyticsTimeoutSeconds => 2.0;
        public double ResourcesTimeoutSeconds => 3.0;
        public double SceneFlowTimeoutSeconds => 2.0;
        public double AITimeoutSeconds => 4.0;
        
        public float SamplingRate => 1.0f;
        public string Description => "Editor mode profile with fast feedback settings for development";

        public void ApplyToSelectionOptions(SelectionStrategyOptions options)
        {
            options.SetCategoryDefault(ServiceCategory.Analytics, AnalyticsStrategy);
            options.SetCategoryDefault(ServiceCategory.Resources, ResourcesStrategy);
            options.SetCategoryDefault(ServiceCategory.SceneFlow, SceneFlowStrategy);
            options.SetCategoryDefault(ServiceCategory.AI, AIStrategy);
        }

        public void ApplyToResilienceOptions(PollyResilienceExecutorOptions resilienceOptions)
        {
            resilienceOptions.DefaultTimeoutSeconds = DefaultTimeoutSeconds;
            resilienceOptions.MaxRetryAttempts = MaxRetryAttempts;
            resilienceOptions.BaseRetryDelayMilliseconds = BaseRetryDelayMilliseconds;
            resilienceOptions.EnableCircuitBreaker = EnableCircuitBreaker;

            resilienceOptions.SetCategoryTimeout(ServiceCategory.Analytics, AnalyticsTimeoutSeconds);
            resilienceOptions.SetCategoryTimeout(ServiceCategory.Resources, ResourcesTimeoutSeconds);
            resilienceOptions.SetCategoryTimeout(ServiceCategory.SceneFlow, SceneFlowTimeoutSeconds);
            resilienceOptions.SetCategoryTimeout(ServiceCategory.AI, AITimeoutSeconds);
        }
    }
}