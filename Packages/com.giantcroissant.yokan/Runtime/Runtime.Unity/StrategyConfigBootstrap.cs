// Unity MonoBehaviour component for bootstrapping strategy configuration from ScriptableObject assets

using UnityEngine;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Unity
{
    /// <summary>
    /// MonoBehaviour component that automatically imports strategy configuration from ScriptableObject assets
    /// when the component starts in a Unity scene. This provides a simple way to configure PintoBean
    /// strategies without requiring custom service registration code.
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

        [Header("Runtime Info")]
        [SerializeField, ReadOnly] private bool hasImported = false;
        [SerializeField, ReadOnly] private int strategyAssetsFound = 0;
        [SerializeField, ReadOnly] private int shardAssetsFound = 0;

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

        private void Start()
        {
            ImportConfiguration();
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
                // Get or create target options
                var options = GetOrCreateOptions();
                if (options == null)
                {
                    Debug.LogError($"[StrategyConfigBootstrap] Failed to get or create SelectionStrategyOptions on {gameObject.name}");
                    return;
                }

                // Count available assets before import
                CountAvailableAssets();

                // Import configuration
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
        /// Clears the imported flag and allows re-import on next call.
        /// </summary>
        [ContextMenu("Clear Import Flag")]
        public void ClearImportFlag()
        {
            StrategyConfigImporter.ClearImportedFlag();
            hasImported = false;
            Debug.Log($"[StrategyConfigBootstrap] Cleared import flag on {gameObject.name}");
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