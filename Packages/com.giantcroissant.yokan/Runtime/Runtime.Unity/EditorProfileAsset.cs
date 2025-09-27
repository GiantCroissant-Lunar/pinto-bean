// Unity ScriptableObject asset for Editor mode profile configuration
// Editor mode: shorter timeouts, fewer retries, faster feedback for development

using UnityEngine;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Unity
{
    /// <summary>
    /// ScriptableObject asset that defines Editor mode profile configuration.
    /// Editor mode typically uses shorter timeouts, fewer retries, and faster feedback
    /// settings optimized for development scenarios.
    /// </summary>
    [CreateAssetMenu(menuName = "PintoBean/Editor Profile", fileName = "EditorProfile")]
    public class EditorProfileAsset : ScriptableObject
    {
        [Header("Selection Strategy Settings")]
        [Tooltip("Analytics strategy for Editor mode (typically PickOne for faster testing)")]
        [SerializeField] private SelectionStrategyType analyticsStrategy = SelectionStrategyType.PickOne;
        
        [Tooltip("Resources strategy for Editor mode (typically PickOne with local fallback)")]
        [SerializeField] private SelectionStrategyType resourcesStrategy = SelectionStrategyType.PickOne;
        
        [Tooltip("SceneFlow strategy for Editor mode (typically PickOne for deterministic testing)")]
        [SerializeField] private SelectionStrategyType sceneFlowStrategy = SelectionStrategyType.PickOne;
        
        [Tooltip("AI strategy for Editor mode (typically PickOne for focused testing)")]
        [SerializeField] private SelectionStrategyType aiStrategy = SelectionStrategyType.PickOne;

        [Header("Resilience Settings")]
        [Tooltip("Default timeout for operations in Editor mode (seconds) - shorter for faster feedback")]
        [SerializeField] private double defaultTimeoutSeconds = 5.0;
        
        [Tooltip("Maximum retry attempts in Editor mode - fewer for faster failure detection")]
        [SerializeField] private int maxRetryAttempts = 1;
        
        [Tooltip("Base retry delay in Editor mode (milliseconds) - shorter for faster iteration")]
        [SerializeField] private double baseRetryDelayMilliseconds = 100.0;
        
        [Tooltip("Enable circuit breaker in Editor mode")]
        [SerializeField] private bool enableCircuitBreaker = false;
        
        [Tooltip("Circuit breaker failure threshold in Editor mode")]
        [SerializeField] private int circuitBreakerFailureThreshold = 3;
        
        [Tooltip("Circuit breaker break duration in Editor mode (seconds) - shorter for faster recovery")]
        [SerializeField] private double circuitBreakerDurationOfBreakSeconds = 10.0;

        [Header("Category-Specific Timeouts")]
        [Tooltip("Analytics operations timeout (seconds) - shorter for Editor testing")]
        [SerializeField] private double analyticsTimeoutSeconds = 2.0;
        
        [Tooltip("Resources operations timeout (seconds) - shorter for Editor testing")]
        [SerializeField] private double resourcesTimeoutSeconds = 3.0;
        
        [Tooltip("SceneFlow operations timeout (seconds) - shorter for Editor testing")]
        [SerializeField] private double sceneFlowTimeoutSeconds = 2.0;
        
        [Tooltip("AI operations timeout (seconds) - shorter for Editor testing")]
        [SerializeField] private double aiTimeoutSeconds = 4.0;

        [Header("Provider Preferences")]
        [Tooltip("Sampling rate for performance metrics (0.0 - 1.0) - higher for better Editor diagnostics")]
        [SerializeField, Range(0.0f, 1.0f)] private float samplingRate = 1.0f;

        [Header("Aspect Runtime Settings")]
        [Tooltip("Type of aspect runtime to use for telemetry and logging in Editor mode")]
        [SerializeField] private AspectRuntimeType aspectRuntimeType = AspectRuntimeType.Adaptive;
        
        [Tooltip("Enable metrics recording in aspect runtime")]
        [SerializeField] private bool enableMetrics = true;
        
        [Tooltip("Enable verbose logging in aspect runtime - often useful for Editor development")]
        [SerializeField] private bool verboseLogging = true;
        
        [Tooltip("ActivitySource name for OpenTelemetry tracing (used when AspectRuntimeType is OpenTelemetry or Adaptive)")]
        [SerializeField] private string activitySourceName = "PintoBean.Editor";
        
        [Tooltip("Meter name for OpenTelemetry metrics (used when AspectRuntimeType is OpenTelemetry or Adaptive)")]
        [SerializeField] private string meterName = "PintoBean.Editor";

        [Header("Description")]
        [Tooltip("Description of this Editor profile for documentation")]
        [SerializeField, TextArea(2, 4)] private string description = "Editor mode profile with fast feedback settings for development.";

        // Properties for accessing values
        public SelectionStrategyType AnalyticsStrategy => analyticsStrategy;
        public SelectionStrategyType ResourcesStrategy => resourcesStrategy;
        public SelectionStrategyType SceneFlowStrategy => sceneFlowStrategy;
        public SelectionStrategyType AIStrategy => aiStrategy;
        
        public double DefaultTimeoutSeconds => defaultTimeoutSeconds;
        public int MaxRetryAttempts => maxRetryAttempts;
        public double BaseRetryDelayMilliseconds => baseRetryDelayMilliseconds;
        public bool EnableCircuitBreaker => enableCircuitBreaker;
        public int CircuitBreakerFailureThreshold => circuitBreakerFailureThreshold;
        public double CircuitBreakerDurationOfBreakSeconds => circuitBreakerDurationOfBreakSeconds;
        
        public double AnalyticsTimeoutSeconds => analyticsTimeoutSeconds;
        public double ResourcesTimeoutSeconds => resourcesTimeoutSeconds;
        public double SceneFlowTimeoutSeconds => sceneFlowTimeoutSeconds;
        public double AITimeoutSeconds => aiTimeoutSeconds;
        
        public float SamplingRate => samplingRate;
        public string Description => description;

        public AspectRuntimeType AspectRuntimeType => aspectRuntimeType;
        public bool EnableMetrics => enableMetrics;
        public bool VerboseLogging => verboseLogging;
        public string ActivitySourceName => activitySourceName;
        public string MeterName => meterName;

        /// <summary>
        /// Applies this Editor profile configuration to SelectionStrategyOptions.
        /// </summary>
        /// <param name="options">The SelectionStrategyOptions to configure.</param>
        public void ApplyToSelectionOptions(SelectionStrategyOptions options)
        {
            if (options == null)
            {
                Debug.LogError($"[EditorProfileAsset] Cannot apply profile from {name}: options is null");
                return;
            }

            Debug.Log($"[EditorProfileAsset] Applying Editor profile settings from {name}");

            options.SetCategoryDefault(ServiceCategory.Analytics, analyticsStrategy);
            options.SetCategoryDefault(ServiceCategory.Resources, resourcesStrategy);
            options.SetCategoryDefault(ServiceCategory.SceneFlow, sceneFlowStrategy);
            options.SetCategoryDefault(ServiceCategory.AI, aiStrategy);

            Debug.Log($"[EditorProfileAsset] Applied strategy settings: Analytics={analyticsStrategy}, Resources={resourcesStrategy}, SceneFlow={sceneFlowStrategy}, AI={aiStrategy}");
        }

        /// <summary>
        /// Applies this Editor profile configuration to PollyResilienceExecutorOptions.
        /// </summary>
        /// <param name="resilienceOptions">The PollyResilienceExecutorOptions to configure.</param>
        public void ApplyToResilienceOptions(PollyResilienceExecutorOptions resilienceOptions)
        {
            if (resilienceOptions == null)
            {
                Debug.LogError($"[EditorProfileAsset] Cannot apply resilience settings from {name}: resilienceOptions is null");
                return;
            }

            Debug.Log($"[EditorProfileAsset] Applying Editor resilience settings from {name}");

            resilienceOptions.DefaultTimeoutSeconds = defaultTimeoutSeconds;
            resilienceOptions.MaxRetryAttempts = maxRetryAttempts;
            resilienceOptions.BaseRetryDelayMilliseconds = baseRetryDelayMilliseconds;
            resilienceOptions.EnableCircuitBreaker = enableCircuitBreaker;
            resilienceOptions.CircuitBreakerFailureThreshold = circuitBreakerFailureThreshold;
            resilienceOptions.CircuitBreakerDurationOfBreakSeconds = circuitBreakerDurationOfBreakSeconds;

            // Apply category-specific timeouts
            resilienceOptions.SetCategoryTimeout(ServiceCategory.Analytics, analyticsTimeoutSeconds);
            resilienceOptions.SetCategoryTimeout(ServiceCategory.Resources, resourcesTimeoutSeconds);
            resilienceOptions.SetCategoryTimeout(ServiceCategory.SceneFlow, sceneFlowTimeoutSeconds);
            resilienceOptions.SetCategoryTimeout(ServiceCategory.AI, aiTimeoutSeconds);

            Debug.Log($"[EditorProfileAsset] Applied resilience settings: Timeout={defaultTimeoutSeconds}s, Retries={maxRetryAttempts}, CircuitBreaker={enableCircuitBreaker}");
        }

        /// <summary>
        /// Applies this Editor profile aspect runtime configuration to the service collection.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        public void ApplyAspectRuntimeToServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
        {
            if (services == null)
            {
                Debug.LogError($"[EditorProfileAsset] Cannot apply aspect runtime settings from {name}: services is null");
                return;
            }

            Debug.Log($"[EditorProfileAsset] Applying Editor aspect runtime settings from {name}: {aspectRuntimeType}");

            switch (aspectRuntimeType)
            {
                case AspectRuntimeType.NoOp:
                    services.AddNoOpAspectRuntime();
                    break;
                case AspectRuntimeType.Unity:
                    services.AddUnityAspectRuntime(enableMetrics, verboseLogging);
                    break;
                case AspectRuntimeType.OpenTelemetry:
                    services.AddOpenTelemetryAspectRuntime(activitySourceName, meterName);
                    break;
                case AspectRuntimeType.Adaptive:
                    services.AddAdaptiveAspectRuntime(activitySourceName, meterName, enableMetrics, verboseLogging);
                    break;
                default:
                    Debug.LogWarning($"[EditorProfileAsset] Unknown aspect runtime type: {aspectRuntimeType}, falling back to Adaptive runtime");
                    services.AddAdaptiveAspectRuntime(activitySourceName, meterName, enableMetrics, verboseLogging);
                    break;
            }

            Debug.Log($"[EditorProfileAsset] Applied aspect runtime: {aspectRuntimeType} (Metrics: {enableMetrics}, Verbose: {verboseLogging})");
        }

        private void Reset()
        {
            // Initialize with Editor mode defaults when the asset is created
            analyticsStrategy = SelectionStrategyType.PickOne;
            resourcesStrategy = SelectionStrategyType.PickOne;
            sceneFlowStrategy = SelectionStrategyType.PickOne;
            aiStrategy = SelectionStrategyType.PickOne;

            defaultTimeoutSeconds = 5.0;
            maxRetryAttempts = 1;
            baseRetryDelayMilliseconds = 100.0;
            enableCircuitBreaker = false;
            circuitBreakerFailureThreshold = 3;
            circuitBreakerDurationOfBreakSeconds = 10.0;

            analyticsTimeoutSeconds = 2.0;
            resourcesTimeoutSeconds = 3.0;
            sceneFlowTimeoutSeconds = 2.0;
            aiTimeoutSeconds = 4.0;

            samplingRate = 1.0f;

            aspectRuntimeType = AspectRuntimeType.Adaptive;
            enableMetrics = true;
            verboseLogging = true;
            activitySourceName = "PintoBean.Editor";
            meterName = "PintoBean.Editor";

            description = "Editor mode profile with fast feedback settings for development:\n" +
                         "- Shorter timeouts for quick failure detection\n" +
                         "- Fewer retry attempts for faster iteration\n" +
                         "- Single provider strategies for predictable testing\n" +
                         "- Adaptive aspect runtime (OpenTelemetry in Editor, Unity in play mode)\n" +
                         "- Higher sampling rate for better diagnostics";
        }
    }
}