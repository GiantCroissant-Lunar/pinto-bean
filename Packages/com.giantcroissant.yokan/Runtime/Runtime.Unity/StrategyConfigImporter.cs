// Unity strategy configuration importer that loads ScriptableObject assets at startup

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Unity
{
    /// <summary>
    /// Utility class that imports strategy configuration from ScriptableObject assets
    /// and applies them to SelectionStrategyOptions at startup.
    /// </summary>
    public static class StrategyConfigImporter
    {
        private static bool _hasImported = false;
        private static readonly Dictionary<string, string> _combinedShardMappings = new Dictionary<string, string>();

        /// <summary>
        /// Imports all StrategyMappingAsset and ShardMapAsset resources and applies them to the provided options.
        /// This method is typically called during Unity application startup.
        /// </summary>
        /// <param name="options">The SelectionStrategyOptions to configure.</param>
        /// <param name="forceReimport">Whether to force reimport even if already imported.</param>
        public static void ImportStrategyConfiguration(SelectionStrategyOptions options, bool forceReimport = false)
        {
            if (options == null)
            {
                Debug.LogError("[StrategyConfigImporter] Cannot import configuration: options is null");
                return;
            }

            if (_hasImported && !forceReimport)
            {
                Debug.Log("[StrategyConfigImporter] Strategy configuration already imported, skipping");
                return;
            }

            Debug.Log("[StrategyConfigImporter] Starting strategy configuration import...");

            try
            {
                // Import strategy mappings
                ImportStrategyMappings(options);

                // Import shard mappings
                ImportShardMappings();

                _hasImported = true;
                Debug.Log("[StrategyConfigImporter] Strategy configuration import completed successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[StrategyConfigImporter] Failed to import strategy configuration: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Gets the combined shard mappings from all loaded ShardMapAssets.
        /// </summary>
        /// <returns>A read-only dictionary of shard key to provider ID mappings.</returns>
        public static IReadOnlyDictionary<string, string> GetCombinedShardMappings()
        {
            return _combinedShardMappings;
        }

        /// <summary>
        /// Checks if strategy configuration has been imported.
        /// </summary>
        /// <returns>True if configuration has been imported, false otherwise.</returns>
        public static bool HasImported => _hasImported;

        /// <summary>
        /// Forces a re-import of strategy configuration on the next call to ImportStrategyConfiguration.
        /// </summary>
        public static void ClearImportedFlag()
        {
            _hasImported = false;
            Debug.Log("[StrategyConfigImporter] Cleared imported flag, next import will reload configuration");
        }

        private static void ImportStrategyMappings(SelectionStrategyOptions options)
        {
            // Load all StrategyMappingAsset resources
            var strategyAssets = Resources.LoadAll<StrategyMappingAsset>("");
            
            if (strategyAssets == null || strategyAssets.Length == 0)
            {
                Debug.LogWarning("[StrategyConfigImporter] No StrategyMappingAsset resources found. " +
                               "Create strategy mapping assets in a Resources folder or use sensible defaults.");
                return;
            }

            Debug.Log($"[StrategyConfigImporter] Found {strategyAssets.Length} StrategyMappingAsset(s)");

            foreach (var asset in strategyAssets)
            {
                if (asset != null)
                {
                    asset.ApplyToOptions(options, logWarnings: true);
                }
                else
                {
                    Debug.LogWarning("[StrategyConfigImporter] Found null StrategyMappingAsset reference");
                }
            }
        }

        private static void ImportShardMappings()
        {
            // Clear previous mappings
            _combinedShardMappings.Clear();

            // Load all ShardMapAsset resources
            var shardAssets = Resources.LoadAll<ShardMapAsset>("");
            
            if (shardAssets == null || shardAssets.Length == 0)
            {
                Debug.Log("[StrategyConfigImporter] No ShardMapAsset resources found. " +
                         "Sharded strategies will use consistent hashing only.");
                return;
            }

            Debug.Log($"[StrategyConfigImporter] Found {shardAssets.Length} ShardMapAsset(s)");

            foreach (var asset in shardAssets)
            {
                if (asset != null)
                {
                    var assetMappings = asset.ToDictionary(logWarnings: true);
                    foreach (var kvp in assetMappings)
                    {
                        if (_combinedShardMappings.ContainsKey(kvp.Key))
                        {
                            Debug.LogWarning($"[StrategyConfigImporter] Duplicate shard key '{kvp.Key}' found across multiple ShardMapAssets. " +
                                           $"Overriding '{_combinedShardMappings[kvp.Key]}' with '{kvp.Value}' from {asset.name}");
                        }
                        _combinedShardMappings[kvp.Key] = kvp.Value;
                    }
                }
                else
                {
                    Debug.LogWarning("[StrategyConfigImporter] Found null ShardMapAsset reference");
                }
            }

            Debug.Log($"[StrategyConfigImporter] Combined shard mappings: {_combinedShardMappings.Count} entries");
        }

        /// <summary>
        /// Creates a custom Sharded strategy with the combined shard mappings for Analytics services.
        /// This is a convenience method for setting up Analytics with explicit shard mapping.
        /// </summary>
        /// <typeparam name="TService">The Analytics service type.</typeparam>
        /// <param name="registry">Optional service registry for cache invalidation.</param>
        /// <returns>A Sharded selection strategy with explicit mapping for Analytics.</returns>
        public static ISelectionStrategy<TService> CreateAnalyticsShardedStrategy<TService>(IServiceRegistry registry = null)
            where TService : class
        {
            var shardMappings = GetCombinedShardMappings();
            if (shardMappings.Count > 0)
            {
                Debug.Log($"[StrategyConfigImporter] Creating Analytics Sharded strategy with {shardMappings.Count} explicit mappings");
                return DefaultSelectionStrategies.CreateAnalyticsShardedWithMap<TService>(shardMappings, registry);
            }
            else
            {
                Debug.Log("[StrategyConfigImporter] Creating Analytics Sharded strategy with consistent hashing only");
                return DefaultSelectionStrategies.CreateAnalyticsSharded<TService>(registry);
            }
        }

        /// <summary>
        /// Creates a custom Sharded strategy with the combined shard mappings and custom key extraction.
        /// </summary>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <param name="keyExtractor">Function to extract shard key from selection metadata.</param>
        /// <param name="registry">Optional service registry for cache invalidation.</param>
        /// <returns>A Sharded selection strategy with explicit mapping.</returns>
        public static ISelectionStrategy<TService> CreateShardedStrategy<TService>(
            System.Func<System.Collections.Generic.IDictionary<string, object>, string> keyExtractor,
            IServiceRegistry registry = null)
            where TService : class
        {
            var shardMappings = GetCombinedShardMappings();
            if (shardMappings.Count > 0)
            {
                Debug.Log($"[StrategyConfigImporter] Creating custom Sharded strategy with {shardMappings.Count} explicit mappings");
                return DefaultSelectionStrategies.CreateShardedWithMap<TService>(keyExtractor, shardMappings, registry);
            }
            else
            {
                Debug.Log("[StrategyConfigImporter] Creating custom Sharded strategy with consistent hashing only");
                return DefaultSelectionStrategies.CreateSharded<TService>(keyExtractor, registry);
            }
        }
    }
}