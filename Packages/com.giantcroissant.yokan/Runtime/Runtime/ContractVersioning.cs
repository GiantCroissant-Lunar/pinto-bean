using System;
using System.Reflection;
using System.Runtime.Loader;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Provides contract versioning utilities for plugin compatibility validation.
/// </summary>
public static class ContractVersioning
{
    /// <summary>
    /// Gets the current contract version from the Abstractions assembly.
    /// </summary>
    public static string CurrentContractVersion => "0.1.0-dev";

    /// <summary>
    /// Validates if a plugin's contract version is compatible with the current contract version.
    /// </summary>
    /// <param name="pluginContractVersion">The contract version declared by the plugin.</param>
    /// <returns>True if compatible, false otherwise.</returns>
    public static bool IsCompatible(string? pluginContractVersion)
    {
        if (string.IsNullOrWhiteSpace(pluginContractVersion))
        {
            // If no contract version is declared, assume incompatible for safety
            return false;
        }

        // For now, require exact version match since we're in early development (0.1.0-dev)
        // In the future, this could implement semantic versioning compatibility rules
        return string.Equals(pluginContractVersion, CurrentContractVersion, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets a detailed compatibility error message for an incompatible plugin.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <param name="pluginContractVersion">The contract version declared by the plugin.</param>
    /// <returns>A detailed error message.</returns>
    public static string GetCompatibilityErrorMessage(string pluginId, string? pluginContractVersion)
    {
        if (string.IsNullOrWhiteSpace(pluginContractVersion))
        {
            return $"Plugin '{pluginId}' does not declare a contract version. " +
                   $"Expected contract version: {CurrentContractVersion}. " +
                   "Please rebuild the plugin against the current contract version.";
        }

        return $"Plugin '{pluginId}' was compiled against contract version '{pluginContractVersion}' " +
               $"which is incompatible with the current contract version '{CurrentContractVersion}'. " +
               "Please rebuild the plugin against the current contract version.";
    }

    /// <summary>
    /// Validates that a contract type comes from a Tier-1 assembly loaded in the default context.
    /// </summary>
    /// <param name="contractType">The contract type to validate.</param>
    /// <returns>True if the type is from a valid Tier-1 assembly, false otherwise.</returns>
    public static bool IsFromTier1Assembly(Type contractType)
    {
        if (contractType?.Assembly == null)
            return false;

        var assembly = contractType.Assembly;
        var assemblyName = assembly.GetName().Name?.ToLowerInvariant() ?? "";

        // Check if it's a Tier-1 assembly (contracts, models, abstractions)
        bool isTier1 = assemblyName.Contains("contracts") ||
                       assemblyName.Contains("models") ||
                       assemblyName.Contains("abstractions");

        if (!isTier1)
            return false;

        // Check if it's loaded in the default context
        // Types from the default context should have their assemblies loaded in the default AssemblyLoadContext
        var loadContext = AssemblyLoadContext.GetLoadContext(assembly);
        return loadContext == AssemblyLoadContext.Default;
    }

    /// <summary>
    /// Gets an error message for invalid contract type identity.
    /// </summary>
    /// <param name="contractType">The contract type that failed validation.</param>
    /// <returns>A detailed error message.</returns>
    public static string GetTypeIdentityErrorMessage(Type contractType)
    {
        var assemblyName = contractType?.Assembly?.GetName().Name ?? "unknown";
        var typeName = contractType?.FullName ?? "unknown";

        return $"Contract type '{typeName}' from assembly '{assemblyName}' does not come from a Tier-1 assembly loaded in the default context. " +
               "Contract types must be defined in assemblies with names containing 'contracts', 'models', or 'abstractions' and loaded in the default AssemblyLoadContext.";
    }
}