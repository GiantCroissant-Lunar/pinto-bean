// Unity ScriptableObject asset for configuring selection strategy mappings per service contract

using System;
using System.Collections.Generic;
using UnityEngine;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Unity
{
    /// <summary>
    /// ScriptableObject asset that defines selection strategy mappings for service contracts.
    /// Used to configure strategy defaults that get loaded into SelectionOptions at boot time.
    /// </summary>
    [CreateAssetMenu(menuName = "PintoBean/Strategy Mapping", fileName = "StrategyMapping")]
    public class StrategyMappingAsset : ScriptableObject
    {
        [System.Serializable]
        public class ContractStrategyMapping
        {
            [Tooltip("The fully qualified name of the service contract interface (e.g., 'MyProject.IAnalyticsService')")]
            [SerializeField] private string contractTypeName;
            
            [Tooltip("The selection strategy to use for this contract")]
            [SerializeField] private SelectionStrategyType strategy;
            
            [Tooltip("Service category for this contract (used for fallback if contract-specific strategy is not found)")]
            [SerializeField] private ServiceCategory category;

            public string ContractTypeName => contractTypeName;
            public SelectionStrategyType Strategy => strategy;
            public ServiceCategory Category => category;
        }

        [System.Serializable]
        public class CategoryStrategyMapping
        {
            [Tooltip("The service category")]
            [SerializeField] private ServiceCategory category;
            
            [Tooltip("The default selection strategy to use for this category")]
            [SerializeField] private SelectionStrategyType strategy;

            public ServiceCategory Category => category;
            public SelectionStrategyType Strategy => strategy;
        }

        [Header("Contract-specific Strategy Overrides")]
        [Tooltip("Strategy overrides for specific service contract types")]
        [SerializeField] private ContractStrategyMapping[] contractMappings = new ContractStrategyMapping[0];

        [Header("Category Defaults")]
        [Tooltip("Default strategy mappings by service category")]
        [SerializeField] private CategoryStrategyMapping[] categoryMappings = new CategoryStrategyMapping[]
        {
            new CategoryStrategyMapping { Category = ServiceCategory.Analytics, Strategy = SelectionStrategyType.FanOut },
            new CategoryStrategyMapping { Category = ServiceCategory.Resources, Strategy = SelectionStrategyType.PickOne },
            new CategoryStrategyMapping { Category = ServiceCategory.SceneFlow, Strategy = SelectionStrategyType.PickOne },
            new CategoryStrategyMapping { Category = ServiceCategory.AI, Strategy = SelectionStrategyType.PickOne }
        };

        /// <summary>
        /// Gets all contract-specific strategy mappings defined in this asset.
        /// </summary>
        public virtual IReadOnlyList<ContractStrategyMapping> ContractMappings => contractMappings;

        /// <summary>
        /// Gets all category default strategy mappings defined in this asset.
        /// </summary>
        public virtual IReadOnlyList<CategoryStrategyMapping> CategoryMappings => categoryMappings;

        /// <summary>
        /// Applies the strategy mappings from this asset to the provided SelectionStrategyOptions.
        /// </summary>
        /// <param name="options">The SelectionStrategyOptions to configure.</param>
        /// <param name="logWarnings">Whether to log warnings for invalid contract type names.</param>
        public void ApplyToOptions(SelectionStrategyOptions options, bool logWarnings = true)
        {
            if (options == null)
            {
                Debug.LogError($"[StrategyMappingAsset] Cannot apply mappings from {name}: options is null");
                return;
            }

            Debug.Log($"[StrategyMappingAsset] Applying strategy mappings from {name}");

            // Apply category defaults first
            foreach (var categoryMapping in categoryMappings)
            {
                options.SetCategoryDefault(categoryMapping.Category, categoryMapping.Strategy);
                Debug.Log($"[StrategyMappingAsset] Set category {categoryMapping.Category} default to {categoryMapping.Strategy}");
            }

            // Apply contract-specific overrides
            foreach (var contractMapping in contractMappings)
            {
                if (string.IsNullOrWhiteSpace(contractMapping.ContractTypeName))
                {
                    if (logWarnings)
                    {
                        Debug.LogWarning($"[StrategyMappingAsset] Skipping contract mapping with empty type name in {name}");
                    }
                    continue;
                }

                // Try to resolve the type
                Type contractType = ResolveContractType(contractMapping.ContractTypeName);
                if (contractType != null)
                {
                    options.UseStrategyFor(contractType, contractMapping.Strategy);
                    Debug.Log($"[StrategyMappingAsset] Set contract {contractMapping.ContractTypeName} strategy to {contractMapping.Strategy}");
                }
                else if (logWarnings)
                {
                    Debug.LogWarning($"[StrategyMappingAsset] Could not resolve contract type '{contractMapping.ContractTypeName}' in {name}. " +
                                   "Make sure the type name is fully qualified and the assembly is loaded.");
                }
            }
        }

        /// <summary>
        /// Attempts to resolve a contract type by name from all loaded assemblies.
        /// </summary>
        /// <param name="typeName">The fully qualified type name.</param>
        /// <returns>The resolved Type, or null if not found.</returns>
        private Type ResolveContractType(string typeName)
        {
            // First try Type.GetType which works for types in the current assembly or mscorlib
            Type type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            // Search through all loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    type = assembly.GetType(typeName);
                    if (type != null)
                    {
                        return type;
                    }
                }
                catch (Exception ex)
                {
                    // Continue searching, but log the exception for debugging
                    Debug.LogWarning($"[StrategyMappingAsset] Exception while searching for type {typeName} in assembly {assembly.FullName}: {ex.Message}");
                }
            }

            return null;
        }

        private void Reset()
        {
            // Initialize with sensible defaults when the asset is created
            if (categoryMappings == null || categoryMappings.Length == 0)
            {
                categoryMappings = new CategoryStrategyMapping[]
                {
                    new CategoryStrategyMapping { Category = ServiceCategory.Analytics, Strategy = SelectionStrategyType.FanOut },
                    new CategoryStrategyMapping { Category = ServiceCategory.Resources, Strategy = SelectionStrategyType.PickOne },
                    new CategoryStrategyMapping { Category = ServiceCategory.SceneFlow, Strategy = SelectionStrategyType.PickOne },
                    new CategoryStrategyMapping { Category = ServiceCategory.AI, Strategy = SelectionStrategyType.PickOne }
                };
            }
        }
    }
}