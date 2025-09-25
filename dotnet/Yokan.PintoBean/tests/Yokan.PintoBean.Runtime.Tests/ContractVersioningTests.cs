using System;
using System.Reflection;
using System.Runtime.Loader;
using Xunit;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Unit tests for the ContractVersioning utility class.
/// </summary>
public class ContractVersioningTests
{
    /// <summary>
    /// Tests that the current contract version is correctly reported.
    /// </summary>
    [Fact]
    public void CurrentContractVersion_ReturnsExpectedVersion()
    {
        // Act
        var version = ContractVersioning.CurrentContractVersion;

        // Assert
        Assert.Equal("0.1.0-dev", version);
    }

    /// <summary>
    /// Tests version compatibility validation with various inputs.
    /// </summary>
    [Theory]
    [InlineData("0.1.0-dev", true)]
    [InlineData("0.1.0-DEV", true)]  // Case insensitive
    [InlineData("0.2.0-dev", false)]
    [InlineData("1.0.0", false)]
    [InlineData("0.1.0", false)]     // Missing -dev suffix
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("invalid-version", false)]
    public void IsCompatible_WithVariousVersions_ReturnsExpectedResult(string? version, bool expected)
    {
        // Act
        var result = ContractVersioning.IsCompatible(version);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Tests error message generation for incompatible versions.
    /// </summary>
    [Fact]
    public void GetCompatibilityErrorMessage_WithIncompatibleVersion_ContainsExpectedContent()
    {
        // Act
        var message = ContractVersioning.GetCompatibilityErrorMessage("test-plugin", "0.2.0-dev");

        // Assert
        Assert.Contains("test-plugin", message);
        Assert.Contains("0.2.0-dev", message);
        Assert.Contains("0.1.0-dev", message);
        Assert.Contains("incompatible", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("current contract version", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rebuild", message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests error message generation for missing contract version.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetCompatibilityErrorMessage_WithMissingVersion_ContainsExpectedContent(string? version)
    {
        // Act
        var message = ContractVersioning.GetCompatibilityErrorMessage("test-plugin", version);

        // Assert
        Assert.Contains("test-plugin", message);
        Assert.Contains("does not declare", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("0.1.0-dev", message);
        Assert.Contains("contract version", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rebuild", message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests type identity validation for Tier-1 assemblies.
    /// </summary>
    [Fact]
    public void IsFromTier1Assembly_WithAbstractionsType_ReturnsTrue()
    {
        // Arrange - Use a type from the Abstractions assembly which should be Tier-1
        var abstractionsType = typeof(Yokan.PintoBean.Abstractions.PintoBeanAbstractions);

        // Act
        var result = ContractVersioning.IsFromTier1Assembly(abstractionsType);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests type identity validation for non-Tier-1 assemblies.
    /// </summary>
    [Fact]
    public void IsFromTier1Assembly_WithRuntimeType_ReturnsFalse()
    {
        // Arrange - Use a type from the Runtime assembly which should not be Tier-1
        var runtimeType = typeof(PluginHost);

        // Act
        var result = ContractVersioning.IsFromTier1Assembly(runtimeType);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests type identity validation with null type.
    /// </summary>
    [Fact]
    public void IsFromTier1Assembly_WithNullType_ReturnsFalse()
    {
        // Act
        var result = ContractVersioning.IsFromTier1Assembly(null!);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests type identity error message generation.
    /// </summary>
    [Fact]
    public void GetTypeIdentityErrorMessage_WithInvalidType_ContainsExpectedContent()
    {
        // Arrange
        var runtimeType = typeof(PluginHost);

        // Act
        var message = ContractVersioning.GetTypeIdentityErrorMessage(runtimeType);

        // Assert
        Assert.Contains(runtimeType.FullName!, message);
        Assert.Contains(runtimeType.Assembly.GetName().Name!, message);
        Assert.Contains("Tier-1 assembly", message);
        Assert.Contains("default context", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("contracts", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("models", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("abstractions", message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests type identity error message with null type.
    /// </summary>
    [Fact]
    public void GetTypeIdentityErrorMessage_WithNullType_HandlesGracefully()
    {
        // Act
        var message = ContractVersioning.GetTypeIdentityErrorMessage(null!);

        // Assert
        Assert.Contains("unknown", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Tier-1 assembly", message);
    }

    /// <summary>
    /// Tests that system types are not considered Tier-1.
    /// </summary>
    [Fact]
    public void IsFromTier1Assembly_WithSystemType_ReturnsFalse()
    {
        // Arrange
        var systemType = typeof(string);

        // Act
        var result = ContractVersioning.IsFromTier1Assembly(systemType);

        // Assert
        Assert.False(result);
    }
}