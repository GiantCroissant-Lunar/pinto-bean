// Unity MonoBehaviour component for bootstrapping strategy configuration from ScriptableObject assets

using UnityEngine;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Unity
{
    /// <summary>
    /// MonoBehaviour component that automatically imports strategy configuration from ScriptableObject assets
    /// when the component starts in a Unity scene. This provides a simple way to configure PintoBean
    /// strategies without requiring custom service registration code.
    /// Supports Game vs Editor profiles based on Application.isEditor && !Application.isPlaying detection.
    /// </summary>
    [AddComponentMenu("PintoBean/Strategy Config Bootstrap")]
    public class StrategyConfigBootstrap : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Whether to force reimport of configuration even if already imported")]
        [SerializeField] private bool forceReimport = false;

        [Tooltip("Whether to log detailed information about the import process")]
        [SerializeField] private bool verboseLogging = true;

        [Tooltip("Optional SelectionStrategyOptions instance to configure. If null, will try to find or create one.")]
        [SerializeField] private SelectionStrategyOptions targetOptions = null;

        [Header("Profile Assets")]
        [Tooltip("Game profile asset to use when in Play mode (Application.isPlaying = true)")]
        [SerializeField] private GameProfileAsset gameProfile = null;

        [Tooltip("Editor profile asset to use when in Editor mode (Application.isEditor = true && Application.isPlaying = false)")]
        [SerializeField] private EditorProfileAsset editorProfile = null;

        [Tooltip("Optional PollyResilienceExecutorOptions instance to configure. If null, will try to find or create one.")]
        [SerializeField] private PollyResilienceExecutorOptions targetResilienceOptions = null;

        [Header("Runtime Info")]
        [SerializeField, ReadOnly] private bool hasImported = false;
        [SerializeField, ReadOnly] private int strategyAssetsFound = 0;
        [SerializeField, ReadOnly] private int shardAssetsFound = 0;
        [SerializeField, ReadOnly] private string selectedProfile = "None";
        [SerializeField, ReadOnly] private string detectedMode = "Unknown";

        /// <summary>
        /// Gets whether this bootstrap component has successfully imported configuration.
        /// </summary>
        public bool HasImported => hasImported;

        /// <summary>
        /// Gets the number of strategy mapping assets found during the last import.
        /// </summary>
        public int StrategyAssetsFound => strategyAssetsFound;

        /// <summary>
        /// Gets the number of shard map assets found during the last import.
        /// </summary>
        public int ShardAssetsFound => shardAssetsFound;

        /// <summary>
        /// Gets the selected profile name for display purposes.
        /// </summary>
        public string SelectedProfile => selectedProfile;

        /// <summary>
        /// Gets the detected Unity mode for display purposes.
        /// </summary>
        public string DetectedMode => detectedMode;

        private void Start()
        {
            DetectUnityMode();
            ImportConfiguration();
        }

        /// <summary>
        /// Detects the current Unity mode and updates display values.
        /// </summary>
        private void DetectUnityMode()
        {
            bool isEditorMode = Application.isEditor && !Application.isPlaying;
            bool isPlayMode = Application.isPlaying;

            if (isEditorMode)
            {
                detectedMode = "Editor";
                selectedProfile = editorProfile != null ? editorProfile.name : "None (Editor)";
            }
            else if (isPlayMode)
            {
                detectedMode = "Play";
                selectedProfile = gameProfile != null ? gameProfile.name : "None (Game)";
            }
            else
            {
                detectedMode = "Unknown";
                selectedProfile = "None";
            }

            if (verboseLogging)
            {
                Debug.Log($"[StrategyConfigBootstrap] Detected Unity mode: {detectedMode}, Selected profile: {selectedProfile}");
            }
        }

        /// <summary>
        /// Manually triggers import of strategy configuration.
        /// </summary>
        [ContextMenu("Import Configuration")]
        public void ImportConfiguration()
        {
            if (verboseLogging)
            {
                Debug.Log($"[StrategyConfigBootstrap] Starting configuration import on {gameObject.name}");
            }

            try
            {
                // Detect mode if not already done
                DetectUnityMode();

                // Get or create target options
                var options = GetOrCreateOptions();
                if (options == null)
                {
                    Debug.LogError($"[StrategyConfigBootstrap] Failed to get or create SelectionStrategyOptions on {gameObject.name}");
                    return;
                }

                // Get or create resilience options
                var resilienceOptions = GetOrCreateResilienceOptions();

                // Apply profile-specific settings first
                ApplyProfileSettings(options, resilienceOptions);

                // Count available assets before import
                CountAvailableAssets();

                // Import configuration from strategy mapping assets
                StrategyConfigImporter.ImportStrategyConfiguration(options, forceReimport);

                hasImported = true;

                if (verboseLogging)
                {
                    Debug.Log($"[StrategyConfigBootstrap] Successfully imported configuration on {gameObject.name}");
                    LogCurrentConfiguration(options);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[StrategyConfigBootstrap] Failed to import configuration on {gameObject.name}: {ex.Message}");
                Debug.LogException(ex, this);
            }
        }

        /// <summary>
        /// Applies profile-specific settings based on the detected Unity mode.
        /// </summary>
        /// <param name="options">The SelectionStrategyOptions to configure.</param>
        /// <param name="resilienceOptions">The PollyResilienceExecutorOptions to configure.</param>
        private void ApplyProfileSettings(SelectionStrategyOptions options, PollyResilienceExecutorOptions resilienceOptions)
        {
            bool isEditorMode = Application.isEditor && !Application.isPlaying;

            if (isEditorMode && editorProfile != null)
            {
                if (verboseLogging)
                {
                    Debug.Log($"[StrategyConfigBootstrap] Applying Editor profile: {editorProfile.name}");
                }
                
                editorProfile.ApplyToSelectionOptions(options);
                
                if (resilienceOptions != null)
                {
                    editorProfile.ApplyToResilienceOptions(resilienceOptions);
                }
            }
            else if (!isEditorMode && gameProfile != null)
            {
                if (verboseLogging)
                {
                    Debug.Log($"[StrategyConfigBootstrap] Applying Game profile: {gameProfile.name}");
                }
                
                gameProfile.ApplyToSelectionOptions(options);
                
                if (resilienceOptions != null)
                {
                    gameProfile.ApplyToResilienceOptions(resilienceOptions);
                }
            }
            else
            {
                if (verboseLogging)
                {
                    string mode = isEditorMode ? "Editor" : "Game";
                    Debug.LogWarning($"[StrategyConfigBootstrap] No {mode} profile asset assigned. Using default settings.");
                }
            }
        }

        /// <summary>
        /// Gets or creates PollyResilienceExecutorOptions instance.
        /// </summary>
        /// <returns>The PollyResilienceExecutorOptions instance, or null if not available.</returns>
        private PollyResilienceExecutorOptions GetOrCreateResilienceOptions()
        {
            // Use target resilience options if specified
            if (targetResilienceOptions != null)
            {
                return targetResilienceOptions;
            }

            // Try to find an existing resilience options instance
            var existingHolder = FindObjectOfType<ResilienceOptionsHolder>();
            if (existingHolder != null)
            {
                return existingHolder.Options;
            }

            // Create a new resilience options instance
            var newOptions = new PollyResilienceExecutorOptions();
            
            // Optionally create a holder component to keep the options around
            var holder = gameObject.AddComponent<ResilienceOptionsHolder>();
            holder.Options = newOptions;
            
            if (verboseLogging)
            {
                Debug.Log($"[StrategyConfigBootstrap] Created new PollyResilienceExecutorOptions instance on {gameObject.name}");
            }

            return newOptions;
        }

        private SelectionStrategyOptions GetOrCreateOptions()
        {
            // Use target options if specified
            if (targetOptions != null)
            {
                return targetOptions;
            }

            // Try to find an existing options instance
            var existingOptions = FindObjectOfType<SelectionStrategyOptionsHolder>();
            if (existingOptions != null)
            {
                return existingOptions.Options;
            }

            // Create a new options instance
            var newOptions = new SelectionStrategyOptions();
            
            // Optionally create a holder component to keep the options around
            var holder = gameObject.AddComponent<SelectionStrategyOptionsHolder>();
            holder.Options = newOptions;
            
            if (verboseLogging)
            {
                Debug.Log($"[StrategyConfigBootstrap] Created new SelectionStrategyOptions instance on {gameObject.name}");
            }

            return newOptions;
        }

        private void CountAvailableAssets()
        {
            var strategyAssets = Resources.LoadAll<StrategyMappingAsset>("");
            var shardAssets = Resources.LoadAll<ShardMapAsset>("");

            strategyAssetsFound = strategyAssets?.Length ?? 0;
            shardAssetsFound = shardAssets?.Length ?? 0;

            if (verboseLogging)
            {
                Debug.Log($"[StrategyConfigBootstrap] Found {strategyAssetsFound} StrategyMappingAsset(s) and {shardAssetsFound} ShardMapAsset(s)");
            }
        }

        private void LogCurrentConfiguration(SelectionStrategyOptions options)
        {
            Debug.Log($"[StrategyConfigBootstrap] Current strategy configuration:");
            Debug.Log($"  Analytics: {options.Analytics}");
            Debug.Log($"  Resources: {options.Resources}");
            Debug.Log($"  SceneFlow: {options.SceneFlow}");
            Debug.Log($"  AI: {options.AI}");

            var shardMappings = StrategyConfigImporter.GetCombinedShardMappings();
            if (shardMappings.Count > 0)
            {
                Debug.Log($"  Shard mappings: {shardMappings.Count} entries");
                foreach (var mapping in shardMappings)
                {
                    Debug.Log($"    {mapping.Key} -> {mapping.Value}");
                }
            }
            else
            {
                Debug.Log($"  Shard mappings: None (will use consistent hashing)");
            }
        }

        private void OnValidate()
        {
            // Update runtime info when values change in the inspector
            if (Application.isPlaying)
            {
                hasImported = StrategyConfigImporter.HasImported;
            }
        }
    }

    /// <summary>
    /// Component that holds a SelectionStrategyOptions instance.
    /// This is used to keep the options around when created by StrategyConfigBootstrap.
    /// </summary>
    [System.Serializable]
    public class SelectionStrategyOptionsHolder : MonoBehaviour
    {
        [System.NonSerialized]
        public SelectionStrategyOptions Options;

        private void Awake()
        {
            // Ensure we don't destroy this object when loading new scenes
            if (Options != null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
    }

    /// <summary>
    /// Component that holds a PollyResilienceExecutorOptions instance.
    /// This is used to keep the resilience options around when created by StrategyConfigBootstrap.
    /// </summary>
    [System.Serializable]
    public class ResilienceOptionsHolder : MonoBehaviour
    {
        [System.NonSerialized]
        public PollyResilienceExecutorOptions Options;

        private void Awake()
        {
            // Ensure we don't destroy this object when loading new scenes
            if (Options != null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
    }

    /// <summary>
    /// Custom attribute to make a field read-only in the inspector.
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute
    {
    }

#if UNITY_EDITOR
    [UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyPropertyDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            UnityEditor.EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
#endif
}