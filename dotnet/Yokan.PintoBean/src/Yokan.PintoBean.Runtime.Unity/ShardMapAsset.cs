// Unity ScriptableObject asset for configuring explicit shard mappings for Sharded selection strategies

using System.Collections.Generic;

#if UNITY_2018_1_OR_NEWER
using UnityEngine;
#endif

namespace Yokan.PintoBean.Runtime.Unity
{
    /// <summary>
    /// ScriptableObject asset that defines explicit shard key to provider ID mappings.
    /// Used for Sharded selection strategies to override consistent hashing with explicit routing.
    /// </summary>
#if UNITY_2018_1_OR_NEWER
    [CreateAssetMenu(menuName = "PintoBean/Shard Map", fileName = "ShardMap")]
    public class ShardMapAsset : ScriptableObject
#else
    public class ShardMapAsset
#endif
    {
        /// <summary>
        /// Represents a mapping between a shard key and a provider ID.
        /// </summary>
        [System.Serializable]
        public class ShardMapping
        {
            [System.ComponentModel.Description("The shard key (e.g., 'player', 'system', 'inventory')")]
#if UNITY_2018_1_OR_NEWER
            [SerializeField]
#endif
            private string shardKey = string.Empty;
            
            [System.ComponentModel.Description("The provider ID to route this shard key to")]
#if UNITY_2018_1_OR_NEWER
            [SerializeField]
#endif
            private string providerId = string.Empty;

            /// <summary>
            /// Gets the shard key.
            /// </summary>
            public string ShardKey => shardKey;
            /// <summary>
            /// Gets the provider ID to route this shard key to.
            /// </summary>
            public string ProviderId => providerId;

            /// <summary>
            /// Initializes a new instance of the <see cref="ShardMapping"/> class.
            /// Parameterless constructor for serialization.
            /// </summary>
            public ShardMapping() { }

            /// <summary>
            /// Initializes a new instance of the <see cref="ShardMapping"/> class.
            /// Constructor for programmatic creation.
            /// </summary>
            /// <param name="shardKey">The shard key.</param>
            /// <param name="providerId">The provider ID to route this shard key to.</param>
            public ShardMapping(string shardKey, string providerId)
            {
                this.shardKey = shardKey;
                this.providerId = providerId;
            }
        }

#if UNITY_2018_1_OR_NEWER
        [Header("Shard Key Mappings")]
        [Tooltip("Explicit mappings from shard keys to provider IDs. Keys not listed here will use consistent hashing.")]
        [SerializeField]
#endif
        private ShardMapping[] shardMappings = new ShardMapping[0];

#if UNITY_2018_1_OR_NEWER
        [Header("Configuration")]
        [Tooltip("Description of this shard map for documentation purposes")]
        [SerializeField, TextArea(2, 4)]
#endif
        private string description = "Defines explicit shard key to provider mappings for Sharded selection strategies.";

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

            Log($"[ShardMapAsset] Converting shard mappings from {GetName()} ({shardMappings.Length} entries)");

            foreach (var mapping in shardMappings)
            {
                if (string.IsNullOrWhiteSpace(mapping.ShardKey))
                {
                    if (logWarnings)
                    {
                        LogWarning($"[ShardMapAsset] Skipping mapping with empty shard key in {GetName()}");
                    }
                    continue;
                }

                if (string.IsNullOrWhiteSpace(mapping.ProviderId))
                {
                    if (logWarnings)
                    {
                        LogWarning($"[ShardMapAsset] Skipping mapping for shard key '{mapping.ShardKey}' with empty provider ID in {GetName()}");
                    }
                    continue;
                }

                if (result.ContainsKey(mapping.ShardKey))
                {
                    if (logWarnings)
                    {
                        LogWarning($"[ShardMapAsset] Duplicate shard key '{mapping.ShardKey}' in {GetName()}. " +
                                   $"Overriding '{result[mapping.ShardKey]}' with '{mapping.ProviderId}'");
                    }
                }

                result[mapping.ShardKey] = mapping.ProviderId;
                Log($"[ShardMapAsset] Mapped shard key '{mapping.ShardKey}' -> '{mapping.ProviderId}'");
            }

            Log($"[ShardMapAsset] Successfully converted {result.Count} shard mappings from {GetName()}");
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

        private string GetName()
        {
#if UNITY_2018_1_OR_NEWER
            return name;
#else
            return this.GetType().Name;
#endif
        }

        private void Log(string message)
        {
#if UNITY_2018_1_OR_NEWER
            Debug.Log(message);
#else
            System.Console.WriteLine(message);
#endif
        }

        private void LogWarning(string message)
        {
#if UNITY_2018_1_OR_NEWER
            Debug.LogWarning(message);
#else
            System.Console.WriteLine($"WARNING: {message}");
#endif
        }

#if UNITY_2018_1_OR_NEWER
        private void Reset()
        {
            // Initialize with example mappings when the asset is created
            if (shardMappings == null || shardMappings.Length == 0)
            {
                shardMappings = new ShardMapping[]
                {
                    new ShardMapping("player", "PrimaryAnalyticsProvider"),
                    new ShardMapping("system", "SystemAnalyticsProvider"),
                    new ShardMapping("debug", "DebugAnalyticsProvider")
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
                            LogWarning($"[ShardMapAsset] Duplicate shard key '{mapping.ShardKey}' detected in {GetName()}");
                        }
                        seenKeys.Add(mapping.ShardKey);
                    }
                }
            }
        }
#endif
    }
}