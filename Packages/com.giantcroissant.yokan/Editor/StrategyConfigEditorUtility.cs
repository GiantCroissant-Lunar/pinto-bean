// Unity Editor utility for creating default strategy configuration assets

#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using Yokan.PintoBean.Runtime;
using Yokan.PintoBean.Runtime.Unity;

namespace Yokan.PintoBean.Editor
{
    /// <summary>
    /// Unity Editor utility class that provides menu items for creating default strategy configuration assets.
    /// </summary>
    public static class StrategyConfigEditorUtility
    {
        private const string ConfigFolderPath = "Assets/Config";
        private const string PintoBeanConfigPath = "Assets/Config/PintoBean";

        [MenuItem("PintoBean/Create Default Strategy Config", priority = 1)]
        public static void CreateDefaultStrategyConfig()
        {
            EnsureConfigDirectory();
            
            // Create strategy mapping asset
            CreateDefaultStrategyMappingAsset();
            
            // Create shard map asset for analytics
            CreateDefaultShardMapAsset();
            
            AssetDatabase.Refresh();
            
            Debug.Log("[StrategyConfigEditor] Created default PintoBean strategy configuration assets in " + PintoBeanConfigPath);
            
            // Select the config folder to show the created assets
            var configFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(PintoBeanConfigPath);
            if (configFolder != null)
            {
                Selection.activeObject = configFolder;
                EditorGUIUtility.PingObject(configFolder);
            }
        }

        [MenuItem("PintoBean/Create Strategy Mapping Asset", priority = 2)]
        public static void CreateStrategyMappingAsset()
        {
            EnsureConfigDirectory();
            string assetPath = CreateUniqueAssetPath(PintoBeanConfigPath, "StrategyMapping", "asset");
            
            var asset = ScriptableObject.CreateInstance<StrategyMappingAsset>();
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[StrategyConfigEditor] Created StrategyMappingAsset at {assetPath}");
            
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        [MenuItem("PintoBean/Create Shard Map Asset", priority = 3)]
        public static void CreateShardMapAsset()
        {
            EnsureConfigDirectory();
            string assetPath = CreateUniqueAssetPath(PintoBeanConfigPath, "ShardMap", "asset");
            
            var asset = ScriptableObject.CreateInstance<ShardMapAsset>();
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[StrategyConfigEditor] Created ShardMapAsset at {assetPath}");
            
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        [MenuItem("PintoBean/Open Config Folder", priority = 10)]
        public static void OpenConfigFolder()
        {
            EnsureConfigDirectory();
            
            var configFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(PintoBeanConfigPath);
            if (configFolder != null)
            {
                Selection.activeObject = configFolder;
                EditorGUIUtility.PingObject(configFolder);
            }
            else
            {
                Debug.LogWarning($"[StrategyConfigEditor] Config folder not found at {PintoBeanConfigPath}");
            }
        }

        private static void EnsureConfigDirectory()
        {
            // Ensure the main Config directory exists
            if (!AssetDatabase.IsValidFolder(ConfigFolderPath))
            {
                AssetDatabase.CreateFolder("Assets", "Config");
                Debug.Log($"[StrategyConfigEditor] Created Config directory at {ConfigFolderPath}");
            }
            
            // Ensure the PintoBean subdirectory exists
            if (!AssetDatabase.IsValidFolder(PintoBeanConfigPath))
            {
                AssetDatabase.CreateFolder(ConfigFolderPath, "PintoBean");
                Debug.Log($"[StrategyConfigEditor] Created PintoBean config directory at {PintoBeanConfigPath}");
            }
        }

        private static void CreateDefaultStrategyMappingAsset()
        {
            string assetPath = CreateUniqueAssetPath(PintoBeanConfigPath, "DefaultStrategyMapping", "asset");
            
            var asset = ScriptableObject.CreateInstance<StrategyMappingAsset>();
            
            // The asset will be initialized with default values via Reset()
            // But we can add some example contract mappings for demonstration
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[StrategyConfigEditor] Created default StrategyMappingAsset at {assetPath}");
        }

        private static void CreateDefaultShardMapAsset()
        {
            string assetPath = CreateUniqueAssetPath(PintoBeanConfigPath, "DefaultAnalyticsShardMap", "asset");
            
            var asset = ScriptableObject.CreateInstance<ShardMapAsset>();
            
            // The asset will be initialized with example values via Reset()
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[StrategyConfigEditor] Created default ShardMapAsset at {assetPath}");
        }

        private static string CreateUniqueAssetPath(string directory, string baseName, string extension)
        {
            string assetPath = Path.Combine(directory, $"{baseName}.{extension}");
            
            // Make sure the path is unique
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath) != null)
            {
                int counter = 1;
                string uniquePath;
                do
                {
                    uniquePath = Path.Combine(directory, $"{baseName}_{counter}.{extension}");
                    counter++;
                }
                while (AssetDatabase.LoadAssetAtPath<ScriptableObject>(uniquePath) != null);
                
                assetPath = uniquePath;
            }
            
            return assetPath;
        }
    }
}
#endif