using System;

namespace Yokan.PintoBean.CodeGen.Tests;

/// <summary>
/// Tests for the GenerateRegistryAttribute class.
/// </summary>
public class GenerateRegistryAttributeTests
{
    /// <summary>
    /// Tests that the constructor sets the Contract property correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithValidContract_SetsContractProperty()
    {
        // Arrange
        var contract = typeof(ITestService);

        // Act
        var attribute = new GenerateRegistryAttribute(contract);

        // Assert
        Assert.Equal(contract, attribute.Contract);
    }

    /// <summary>
    /// Tests that the constructor works with interface contracts.
    /// </summary>
    [Fact]
    public void Constructor_WithInterfaceContract_SetsContractProperty()
    {
        // Arrange
        var contract = typeof(ISecondService);

        // Act
        var attribute = new GenerateRegistryAttribute(contract);

        // Assert
        Assert.Equal(contract, attribute.Contract);
    }

    /// <summary>
    /// Tests that the constructor works with class contracts.
    /// </summary>
    [Fact]
    public void Constructor_WithClassContract_SetsContractProperty()
    {
        // Arrange
        var contract = typeof(TestClass);

        // Act
        var attribute = new GenerateRegistryAttribute(contract);

        // Assert
        Assert.Equal(contract, attribute.Contract);
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentNullException when given null contract.
    /// </summary>
    [Fact]
    public void Constructor_WithNullContract_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new GenerateRegistryAttribute(null!));
        Assert.Equal("contract", exception.ParamName);
    }

    /// <summary>
    /// Tests that the AttributeUsage is configured correctly.
    /// </summary>
    [Fact]
    public void AttributeUsage_IsConfiguredCorrectly()
    {
        // Arrange
        var attributeType = typeof(GenerateRegistryAttribute);
        
        // Act
        var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(attributeType, typeof(AttributeUsageAttribute))!;
        
        // Assert
        Assert.NotNull(usage);
        Assert.Equal(AttributeTargets.Interface | AttributeTargets.Class, usage.ValidOn);
        Assert.False(usage.AllowMultiple);
        Assert.False(usage.Inherited);
    }

    /// <summary>
    /// Tests that the Contract property is read-only.
    /// </summary>
    [Fact]
    public void Contract_Property_IsReadOnly()
    {
        // Arrange
        var contract = typeof(ITestService);
        var attribute = new GenerateRegistryAttribute(contract);

        // Act & Assert - Property should be read-only (no setter)
        Assert.Equal(contract, attribute.Contract);
        
        // Verify the property is indeed read-only
        var property = typeof(GenerateRegistryAttribute).GetProperty(nameof(GenerateRegistryAttribute.Contract));
        Assert.NotNull(property);
        Assert.Null(property!.GetSetMethod());
    }

    /// <summary>
    /// Tests that the constructor works with generic contracts.
    /// </summary>
    [Fact]
    public void Constructor_WithGenericContract_SetsContractProperty()
    {
        // Arrange
        var contract = typeof(IGenericService<string>);

        // Act
        var attribute = new GenerateRegistryAttribute(contract);

        // Assert
        Assert.Equal(contract, attribute.Contract);
    }
}

/// <summary>
/// Test class type for testing purposes.
/// </summary>
public class TestClass
{
    /// <summary>
    /// Test method for attribute testing.
    /// </summary>
    public void DoSomething() { }
}

/// <summary>
/// Generic test service interface for testing purposes.
/// </summary>
/// <typeparam name="T">Generic type parameter.</typeparam>
public interface IGenericService<T>
{
    /// <summary>
    /// Test generic processing method for attribute testing.
    /// </summary>
    /// <param name="value">Input value.</param>
    /// <returns>Processed value.</returns>
    T Process(T value);
}