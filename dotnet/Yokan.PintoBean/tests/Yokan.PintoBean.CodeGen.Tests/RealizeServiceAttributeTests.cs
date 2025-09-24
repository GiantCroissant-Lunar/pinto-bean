using System;

namespace Yokan.PintoBean.CodeGen.Tests;

/// <summary>
/// Tests for the RealizeServiceAttribute class.
/// </summary>
public class RealizeServiceAttributeTests
{
    /// <summary>
    /// Tests that the constructor sets the Contracts property correctly when given multiple contracts.
    /// </summary>
    [Fact]
    public void Constructor_WithValidContracts_SetsContractsProperty()
    {
        // Arrange
        var contracts = new[] { typeof(ITestService), typeof(ISecondService) };

        // Act
        var attribute = new RealizeServiceAttribute(contracts);

        // Assert
        Assert.Equal(contracts, attribute.Contracts);
    }

    /// <summary>
    /// Tests that the constructor sets the Contracts property correctly when given a single contract.
    /// </summary>
    [Fact]
    public void Constructor_WithSingleContract_SetsContractsProperty()
    {
        // Arrange
        var contract = typeof(ITestService);

        // Act
        var attribute = new RealizeServiceAttribute(contract);

        // Assert
        Assert.Single(attribute.Contracts);
        Assert.Equal(contract, attribute.Contracts[0]);
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentNullException when given null contracts.
    /// </summary>
    [Fact]
    public void Constructor_WithNullContracts_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new RealizeServiceAttribute(null!));
        Assert.Equal("contracts", exception.ParamName);
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentException when given empty contracts.
    /// </summary>
    [Fact]
    public void Constructor_WithEmptyContracts_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new RealizeServiceAttribute());
        Assert.Equal("contracts", exception.ParamName);
        Assert.Contains("At least one contract must be specified", exception.Message);
    }

    /// <summary>
    /// Tests that the AttributeUsage is configured correctly.
    /// </summary>
    [Fact]
    public void AttributeUsage_IsConfiguredCorrectly()
    {
        // Arrange
        var attributeType = typeof(RealizeServiceAttribute);

        // Act
        var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(attributeType, typeof(AttributeUsageAttribute))!;

        // Assert
        Assert.NotNull(usage);
        Assert.Equal(AttributeTargets.Class, usage.ValidOn);
        Assert.False(usage.AllowMultiple);
        Assert.False(usage.Inherited);
    }

    /// <summary>
    /// Tests that the Contracts property is read-only.
    /// </summary>
    [Fact]
    public void Contracts_Property_IsReadOnly()
    {
        // Arrange
        var contracts = new[] { typeof(ITestService) };
        var attribute = new RealizeServiceAttribute(contracts);

        // Act & Assert - Property should be read-only (no setter)
        Assert.Equal(contracts, attribute.Contracts);

        // Verify the property is indeed read-only
        var property = typeof(RealizeServiceAttribute).GetProperty(nameof(RealizeServiceAttribute.Contracts));
        Assert.NotNull(property);
        Assert.Null(property!.GetSetMethod());
    }
}

/// <summary>
/// Test service interface for testing purposes.
/// </summary>
public interface ITestService
{
    /// <summary>
    /// Test method for attribute testing.
    /// </summary>
    void DoSomething();
}

/// <summary>
/// Second test service interface for testing purposes.
/// </summary>
public interface ISecondService
{
    /// <summary>
    /// Test calculation method for attribute testing.
    /// </summary>
    /// <param name="value">Input value.</param>
    /// <returns>Calculated result.</returns>
    int Calculate(int value);
}
