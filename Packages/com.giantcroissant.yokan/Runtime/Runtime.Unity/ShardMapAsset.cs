// Unity ScriptableObject asset for configuring explicit shard mappings for Sharded selection strategies

using System.Collections.Generic;
using UnityEngine;

namespace Yokan.PintoBean.Runtime.Unity
{
    /// <summary>
    /// ScriptableObject asset that defines explicit shard key to provider ID mappings.
    /// Used for Sharded selection strategies to override consistent hashing with explicit routing.
    /// </summary>
    [CreateAssetMenu(menuName = "PintoBean/Shard Map", fileName = "ShardMap")]
    public class ShardMapAsset : ScriptableObject
    {
        [System.Serializable]
        public class ShardMapping
        {
            [Tooltip("The shard key (e.g., 'player', 'system', 'inventory')")]
            [SerializeField] private string shardKey;
            
            [Tooltip("The provider ID to route this shard key to")]
            [SerializeField] private string providerId;

            public string ShardKey => shardKey;
            public string ProviderId => providerId;
        }

        [Header("Shard Key Mappings")]
        [Tooltip("Explicit mappings from shard keys to provider IDs. Keys not listed here will use consistent hashing.")]
        [SerializeField] private ShardMapping[] shardMappings = new ShardMapping[0];

        [Header("Configuration")]
        [Tooltip("Description of this shard map for documentation purposes")]
        [SerializeField, TextArea(2, 4)] private string description = "Defines explicit shard key to provider mappings for Sharded selection strategies.";

        /// <summary>
        /// Gets all shard key mappings defined in this asset.
        /// </summary>
        public virtual IReadOnlyList<ShardMapping> ShardMappings => shardMappings;

        /// <summary>
        /// Gets the description of this shard map.
        /// </summary>
        public virtual string Description => description;

        /// <summary>
        /// Converts this asset's mappings to a dictionary suitable for use with Sharded selection strategies.
        /// </summary>
        /// <param name="logWarnings">Whether to log warnings for invalid or duplicate mappings.</param>
        /// <returns>A dictionary mapping shard keys to provider IDs.</returns>
        public Dictionary<string, string> ToDictionary(bool logWarnings = true)
        {
            var result = new Dictionary<string, string>();

            Debug.Log($"[ShardMapAsset] Converting shard mappings from {name} ({shardMappings.Length} entries)");

            foreach (var mapping in shardMappings)
            {
                if (string.IsNullOrWhiteSpace(mapping.ShardKey))
                {
                    if (logWarnings)
                    {
                        Debug.LogWarning($"[ShardMapAsset] Skipping mapping with empty shard key in {name}");
                    }
                    continue;
                }

                if (string.IsNullOrWhiteSpace(mapping.ProviderId))
                {
                    if (logWarnings)
                    {
                        Debug.LogWarning($"[ShardMapAsset] Skipping mapping for shard key '{mapping.ShardKey}' with empty provider ID in {name}");
                    }
                    continue;
                }

                if (result.ContainsKey(mapping.ShardKey))
                {
                    if (logWarnings)
                    {
                        Debug.LogWarning($"[ShardMapAsset] Duplicate shard key '{mapping.ShardKey}' in {name}. " +
                                       $"Overriding '{result[mapping.ShardKey]}' with '{mapping.ProviderId}'");
                    }
                }

                result[mapping.ShardKey] = mapping.ProviderId;
                Debug.Log($"[ShardMapAsset] Mapped shard key '{mapping.ShardKey}' -> '{mapping.ProviderId}'");
            }

            Debug.Log($"[ShardMapAsset] Successfully converted {result.Count} shard mappings from {name}");
            return result;
        }

        /// <summary>
        /// Gets a read-only dictionary of the shard mappings.
        /// </summary>
        /// <param name="logWarnings">Whether to log warnings for invalid mappings.</param>
        /// <returns>A read-only dictionary of shard key to provider ID mappings.</returns>
        public IReadOnlyDictionary<string, string> GetShardMappings(bool logWarnings = true)
        {
            return ToDictionary(logWarnings);
        }

        private void Reset()
        {
            // Initialize with example mappings when the asset is created
            if (shardMappings == null || shardMappings.Length == 0)
            {
                shardMappings = new ShardMapping[]
                {
                    new ShardMapping { ShardKey = "player", ProviderId = "PrimaryAnalyticsProvider" },
                    new ShardMapping { ShardKey = "system", ProviderId = "SystemAnalyticsProvider" },
                    new ShardMapping { ShardKey = "debug", ProviderId = "DebugAnalyticsProvider" }
                };
                
                description = "Example shard mappings:\n" +
                             "- 'player' events -> PrimaryAnalyticsProvider\n" +
                             "- 'system' events -> SystemAnalyticsProvider\n" +
                             "- 'debug' events -> DebugAnalyticsProvider\n\n" +
                             "Other shard keys will use consistent hashing.";
            }
        }

        private void OnValidate()
        {
            // Validate mappings in the editor
            if (shardMappings != null)
            {
                var seenKeys = new HashSet<string>();
                foreach (var mapping in shardMappings)
                {
                    if (!string.IsNullOrWhiteSpace(mapping.ShardKey))
                    {
                        if (seenKeys.Contains(mapping.ShardKey))
                        {
                            Debug.LogWarning($"[ShardMapAsset] Duplicate shard key '{mapping.ShardKey}' detected in {name}");
                        }
                        seenKeys.Add(mapping.ShardKey);
                    }
                }
            }
        }
    }
}