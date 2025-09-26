// Unity ScriptableObject asset for configuring selection strategy mappings per service contract

using System;
using System.Collections.Generic;
using Yokan.PintoBean.Runtime;

#if UNITY_2018_1_OR_NEWER
using UnityEngine;
#endif

namespace Yokan.PintoBean.Runtime.Unity
{
    /// <summary>
    /// ScriptableObject asset that defines selection strategy mappings for service contracts.
    /// Used to configure strategy defaults that get loaded into SelectionOptions at boot time.
    /// </summary>
#if UNITY_2018_1_OR_NEWER
    [CreateAssetMenu(menuName = "PintoBean/Strategy Mapping", fileName = "StrategyMapping")]
    public class StrategyMappingAsset : ScriptableObject
#else
    public class StrategyMappingAsset
#endif
    {
        /// <summary>
        /// Represents a mapping between a service contract type and a selection strategy.
        /// </summary>
        [System.Serializable]
        public class ContractStrategyMapping
        {
            [System.ComponentModel.Description("The fully qualified name of the service contract interface (e.g., 'MyProject.IAnalyticsService')")]
#if UNITY_2018_1_OR_NEWER
            [SerializeField]
#endif
            private string contractTypeName = string.Empty;  
            
            [System.ComponentModel.Description("The selection strategy to use for this contract")]
#if UNITY_2018_1_OR_NEWER
            [SerializeField]
#endif
            private SelectionStrategyType strategy;
            
            [System.ComponentModel.Description("Service category for this contract (used for fallback if contract-specific strategy is not found)")]
#if UNITY_2018_1_OR_NEWER
            [SerializeField]
#endif
            private ServiceCategory category;

            /// <summary>
            /// Gets the fully qualified name of the service contract interface.
            /// </summary>
            public string ContractTypeName => contractTypeName;
            /// <summary>
            /// Gets the selection strategy to use for this contract.
            /// </summary>
            public SelectionStrategyType Strategy => strategy;
            /// <summary>
            /// Gets the service category for this contract.
            /// </summary>
            public ServiceCategory Category => category;

            /// <summary>
            /// Initializes a new instance of the <see cref="ContractStrategyMapping"/> class.
            /// Parameterless constructor for serialization.
            /// </summary>
            public ContractStrategyMapping() { }

            /// <summary>
            /// Initializes a new instance of the <see cref="ContractStrategyMapping"/> class.
            /// Constructor for programmatic creation.
            /// </summary>
            /// <param name="contractTypeName">The fully qualified name of the service contract interface.</param>
            /// <param name="strategy">The selection strategy to use for this contract.</param>
            /// <param name="category">The service category for this contract.</param>
            public ContractStrategyMapping(string contractTypeName, SelectionStrategyType strategy, ServiceCategory category)
            {
                this.contractTypeName = contractTypeName;
                this.strategy = strategy;
                this.category = category;
            }
        }

        /// <summary>
        /// Represents a mapping between a service category and a default selection strategy.
        /// </summary>
        [System.Serializable]
        public class CategoryStrategyMapping
        {
            [System.ComponentModel.Description("The service category")]
#if UNITY_2018_1_OR_NEWER
            [SerializeField]
#endif
            private ServiceCategory category;
            
            [System.ComponentModel.Description("The default selection strategy to use for this category")]
#if UNITY_2018_1_OR_NEWER
            [SerializeField]
#endif
            private SelectionStrategyType strategy;

            /// <summary>
            /// Gets the service category.
            /// </summary>
            public ServiceCategory Category => category;
            /// <summary>
            /// Gets the default selection strategy to use for this category.
            /// </summary>
            public SelectionStrategyType Strategy => strategy;

            /// <summary>
            /// Initializes a new instance of the <see cref="CategoryStrategyMapping"/> class.
            /// Parameterless constructor for serialization.
            /// </summary>
            public CategoryStrategyMapping() { }

            /// <summary>
            /// Initializes a new instance of the <see cref="CategoryStrategyMapping"/> class.
            /// Constructor for programmatic creation.
            /// </summary>
            /// <param name="category">The service category.</param>
            /// <param name="strategy">The default selection strategy to use for this category.</param>
            public CategoryStrategyMapping(ServiceCategory category, SelectionStrategyType strategy)
            {
                this.category = category;
                this.strategy = strategy;
            }
        }

#if UNITY_2018_1_OR_NEWER
        [Header("Contract-specific Strategy Overrides")]
        [Tooltip("Strategy overrides for specific service contract types")]
        [SerializeField]
#endif
        private ContractStrategyMapping[] contractMappings = new ContractStrategyMapping[0];

#if UNITY_2018_1_OR_NEWER
        [Header("Category Defaults")]
        [Tooltip("Default strategy mappings by service category")]
        [SerializeField]
#endif
        private CategoryStrategyMapping[] categoryMappings = new CategoryStrategyMapping[]
        {
            new CategoryStrategyMapping(ServiceCategory.Analytics, SelectionStrategyType.FanOut),
            new CategoryStrategyMapping(ServiceCategory.Resources, SelectionStrategyType.PickOne),
            new CategoryStrategyMapping(ServiceCategory.SceneFlow, SelectionStrategyType.PickOne),
            new CategoryStrategyMapping(ServiceCategory.AI, SelectionStrategyType.PickOne)
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
                LogError($"[StrategyMappingAsset] Cannot apply mappings from {GetName()}: options is null");
                return;
            }

            Log($"[StrategyMappingAsset] Applying strategy mappings from {GetName()}");

            // Apply category defaults first
            foreach (var categoryMapping in categoryMappings)
            {
                options.SetCategoryDefault(categoryMapping.Category, categoryMapping.Strategy);
                Log($"[StrategyMappingAsset] Set category {categoryMapping.Category} default to {categoryMapping.Strategy}");
            }

            // Apply contract-specific overrides
            foreach (var contractMapping in contractMappings)
            {
                if (string.IsNullOrWhiteSpace(contractMapping.ContractTypeName))
                {
                    if (logWarnings)
                    {
                        LogWarning($"[StrategyMappingAsset] Skipping contract mapping with empty type name in {GetName()}");
                    }
                    continue;
                }

                // Try to resolve the type
                Type? contractType = ResolveContractType(contractMapping.ContractTypeName);
                if (contractType != null)
                {
                    options.UseStrategyFor(contractType, contractMapping.Strategy);
                    Log($"[StrategyMappingAsset] Set contract {contractMapping.ContractTypeName} strategy to {contractMapping.Strategy}");
                }
                else if (logWarnings)
                {
                    LogWarning($"[StrategyMappingAsset] Could not resolve contract type '{contractMapping.ContractTypeName}' in {GetName()}. " +
                               "Make sure the type name is fully qualified and the assembly is loaded.");
                }
            }
        }

        /// <summary>
        /// Attempts to resolve a contract type by name from all loaded assemblies.
        /// </summary>
        /// <param name="typeName">The fully qualified type name.</param>
        /// <returns>The resolved Type, or null if not found.</returns>
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "This is for dynamic configuration loading where types are resolved at runtime")]
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2057:Unrecognized value passed to 'typeName' parameter", Justification = "This is for dynamic configuration loading where type names come from user configuration")]
        private Type? ResolveContractType(string typeName)
        {
            // First try Type.GetType which works for types in the current assembly or mscorlib
            Type? type = Type.GetType(typeName);
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
                    LogWarning($"[StrategyMappingAsset] Exception while searching for type {typeName} in assembly {assembly.FullName}: {ex.Message}");
                }
            }

            return null;
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
            Console.WriteLine(message);
#endif
        }

        private void LogWarning(string message)
        {
#if UNITY_2018_1_OR_NEWER
            Debug.LogWarning(message);
#else
            Console.WriteLine($"WARNING: {message}");
#endif
        }

        private void LogError(string message)
        {
#if UNITY_2018_1_OR_NEWER
            Debug.LogError(message);
#else
            Console.WriteLine($"ERROR: {message}");
#endif
        }

#if UNITY_2018_1_OR_NEWER
        private void Reset()
        {
            // Initialize with sensible defaults when the asset is created
            if (categoryMappings == null || categoryMappings.Length == 0)
            {
                categoryMappings = new CategoryStrategyMapping[]
                {
                    new CategoryStrategyMapping(ServiceCategory.Analytics, SelectionStrategyType.FanOut),
                    new CategoryStrategyMapping(ServiceCategory.Resources, SelectionStrategyType.PickOne),
                    new CategoryStrategyMapping(ServiceCategory.SceneFlow, SelectionStrategyType.PickOne),
                    new CategoryStrategyMapping(ServiceCategory.AI, SelectionStrategyType.PickOne)
                };
            }
        }
#endif
    }
}