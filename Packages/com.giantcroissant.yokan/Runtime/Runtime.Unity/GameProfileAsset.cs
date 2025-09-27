// Unity ScriptableObject asset for Game mode profile configuration
// Game mode: higher timeouts, more retries, robust resilience settings

using UnityEngine;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Unity
{
    /// <summary>
    /// ScriptableObject asset that defines Game mode profile configuration.
    /// Game mode typically uses higher timeouts, more retries, and robust resilience settings
    /// for production gameplay scenarios.
    /// </summary>
    [CreateAssetMenu(menuName = "PintoBean/Game Profile", fileName = "GameProfile")]
    public class GameProfileAsset : ScriptableObject
    {
        [Header("Selection Strategy Settings")]
        [Tooltip("Analytics strategy for Game mode (typically FanOut for multiple backends)")]
        [SerializeField] private SelectionStrategyType analyticsStrategy = SelectionStrategyType.FanOut;
        
        [Tooltip("Resources strategy for Game mode (typically PickOne with fallback)")]
        [SerializeField] private SelectionStrategyType resourcesStrategy = SelectionStrategyType.PickOne;
        
        [Tooltip("SceneFlow strategy for Game mode (typically PickOne for deterministic flow)")]
        [SerializeField] private SelectionStrategyType sceneFlowStrategy = SelectionStrategyType.PickOne;
        
        [Tooltip("AI strategy for Game mode (typically PickOne with routing)")]
        [SerializeField] private SelectionStrategyType aiStrategy = SelectionStrategyType.PickOne;

        [Header("Resilience Settings")]
        [Tooltip("Default timeout for operations in Game mode (seconds)")]
        [SerializeField] private double defaultTimeoutSeconds = 30.0;
        
        [Tooltip("Maximum retry attempts in Game mode")]
        [SerializeField] private int maxRetryAttempts = 3;
        
        [Tooltip("Base retry delay in Game mode (milliseconds)")]
        [SerializeField] private double baseRetryDelayMilliseconds = 1000.0;
        
        [Tooltip("Enable circuit breaker in Game mode")]
        [SerializeField] private bool enableCircuitBreaker = false;
        
        [Tooltip("Circuit breaker failure threshold in Game mode")]
        [SerializeField] private int circuitBreakerFailureThreshold = 5;
        
        [Tooltip("Circuit breaker break duration in Game mode (seconds)")]
        [SerializeField] private double circuitBreakerDurationOfBreakSeconds = 30.0;

        [Header("Category-Specific Timeouts")]
        [Tooltip("Analytics operations timeout (seconds)")]
        [SerializeField] private double analyticsTimeoutSeconds = 10.0;
        
        [Tooltip("Resources operations timeout (seconds)")]
        [SerializeField] private double resourcesTimeoutSeconds = 15.0;
        
        [Tooltip("SceneFlow operations timeout (seconds)")]
        [SerializeField] private double sceneFlowTimeoutSeconds = 5.0;
        
        [Tooltip("AI operations timeout (seconds)")]
        [SerializeField] private double aiTimeoutSeconds = 20.0;

        [Header("Provider Preferences")]
        [Tooltip("Sampling rate for performance metrics (0.0 - 1.0)")]
        [SerializeField, Range(0.0f, 1.0f)] private float samplingRate = 0.1f;

        [Header("Aspect Runtime Settings")]
        [Tooltip("Type of aspect runtime to use for telemetry and logging in Game mode")]
        [SerializeField] private AspectRuntimeType aspectRuntimeType = AspectRuntimeType.Unity;
        
        [Tooltip("Enable metrics recording in aspect runtime")]
        [SerializeField] private bool enableMetrics = true;
        
        [Tooltip("Enable verbose logging in aspect runtime")]
        [SerializeField] private bool verboseLogging = false;
        
        [Tooltip("ActivitySource name for OpenTelemetry tracing (used when AspectRuntimeType is OpenTelemetry or Adaptive)")]
        [SerializeField] private string activitySourceName = "PintoBean.Game";
        
        [Tooltip("Meter name for OpenTelemetry metrics (used when AspectRuntimeType is OpenTelemetry or Adaptive)")]
        [SerializeField] private string meterName = "PintoBean.Game";

        [Header("Description")]
        [Tooltip("Description of this Game profile for documentation")]
        [SerializeField, TextArea(2, 4)] private string description = "Game mode profile with robust settings for production gameplay.";

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
        /// Applies this Game profile configuration to SelectionStrategyOptions.
        /// </summary>
        /// <param name="options">The SelectionStrategyOptions to configure.</param>
        public void ApplyToSelectionOptions(SelectionStrategyOptions options)
        {
            if (options == null)
            {
                Debug.LogError($"[GameProfileAsset] Cannot apply profile from {name}: options is null");
                return;
            }

            Debug.Log($"[GameProfileAsset] Applying Game profile settings from {name}");

            options.SetCategoryDefault(ServiceCategory.Analytics, analyticsStrategy);
            options.SetCategoryDefault(ServiceCategory.Resources, resourcesStrategy);
            options.SetCategoryDefault(ServiceCategory.SceneFlow, sceneFlowStrategy);
            options.SetCategoryDefault(ServiceCategory.AI, aiStrategy);

            Debug.Log($"[GameProfileAsset] Applied strategy settings: Analytics={analyticsStrategy}, Resources={resourcesStrategy}, SceneFlow={sceneFlowStrategy}, AI={aiStrategy}");
        }

        /// <summary>
        /// Applies this Game profile configuration to PollyResilienceExecutorOptions.
        /// </summary>
        /// <param name="resilienceOptions">The PollyResilienceExecutorOptions to configure.</param>
        public void ApplyToResilienceOptions(PollyResilienceExecutorOptions resilienceOptions)
        {
            if (resilienceOptions == null)
            {
                Debug.LogError($"[GameProfileAsset] Cannot apply resilience settings from {name}: resilienceOptions is null");
                return;
            }

            Debug.Log($"[GameProfileAsset] Applying Game resilience settings from {name}");

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

            Debug.Log($"[GameProfileAsset] Applied resilience settings: Timeout={defaultTimeoutSeconds}s, Retries={maxRetryAttempts}, CircuitBreaker={enableCircuitBreaker}");
        }

        /// <summary>
        /// Applies this Game profile aspect runtime configuration to the service collection.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        public void ApplyAspectRuntimeToServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
        {
            if (services == null)
            {
                Debug.LogError($"[GameProfileAsset] Cannot apply aspect runtime settings from {name}: services is null");
                return;
            }

            Debug.Log($"[GameProfileAsset] Applying Game aspect runtime settings from {name}: {aspectRuntimeType}");

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
                    Debug.LogWarning($"[GameProfileAsset] Unknown aspect runtime type: {aspectRuntimeType}, falling back to Unity runtime");
                    services.AddUnityAspectRuntime(enableMetrics, verboseLogging);
                    break;
            }

            Debug.Log($"[GameProfileAsset] Applied aspect runtime: {aspectRuntimeType} (Metrics: {enableMetrics}, Verbose: {verboseLogging})");
        }

        private void Reset()
        {
            // Initialize with Game mode defaults when the asset is created
            analyticsStrategy = SelectionStrategyType.FanOut;
            resourcesStrategy = SelectionStrategyType.PickOne;
            sceneFlowStrategy = SelectionStrategyType.PickOne;
            aiStrategy = SelectionStrategyType.PickOne;

            defaultTimeoutSeconds = 30.0;
            maxRetryAttempts = 3;
            baseRetryDelayMilliseconds = 1000.0;
            enableCircuitBreaker = false;
            circuitBreakerFailureThreshold = 5;
            circuitBreakerDurationOfBreakSeconds = 30.0;

            analyticsTimeoutSeconds = 10.0;
            resourcesTimeoutSeconds = 15.0;
            sceneFlowTimeoutSeconds = 5.0;
            aiTimeoutSeconds = 20.0;

            samplingRate = 0.1f;

            aspectRuntimeType = AspectRuntimeType.Unity;
            enableMetrics = true;
            verboseLogging = false;
            activitySourceName = "PintoBean.Game";
            meterName = "PintoBean.Game";

            description = "Game mode profile with robust settings for production gameplay:\n" +
                         "- Higher timeouts for network operations\n" +
                         "- Multiple retry attempts for resilience\n" +
                         "- Production-ready strategy configurations\n" +
                         "- Unity aspect runtime for play mode logging\n" +
                         "- Optimized for runtime performance";
        }
    }
}