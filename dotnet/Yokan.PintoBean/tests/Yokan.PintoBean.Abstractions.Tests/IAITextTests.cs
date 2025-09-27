// Basic test for IAIText interface
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Yokan.PintoBean.Abstractions;

namespace Yokan.PintoBean.Abstractions.Tests;

/// <summary>
/// Tests for the IAIText interface contract validation.
/// </summary>
public class IAITextTests
{
    [Fact]
    public void IAIText_ShouldHaveExpectedMethods()
    {
        // Arrange
        var interfaceType = typeof(IAIText);
        
        // Act
        var methods = interfaceType.GetMethods();
        
        // Assert
        Assert.Equal(2, methods.Length);
        Assert.Contains("GenerateTextAsync", methods.Select(m => m.Name));
        Assert.Contains("CompleteTextAsync", methods.Select(m => m.Name));
    }

    [Fact]
    public void IAIText_GenerateTextAsync_ShouldHaveCorrectSignature()
    {
        // Arrange
        var interfaceType = typeof(IAIText);
        
        // Act
        var method = interfaceType.GetMethod("GenerateTextAsync");
        
        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<AITextResponse>), method.ReturnType);
        
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(AITextRequest), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.True(parameters[1].HasDefaultValue);
    }

    [Fact]
    public void IAIText_CompleteTextAsync_ShouldHaveCorrectSignature()
    {
        // Arrange
        var interfaceType = typeof(IAIText);
        
        // Act
        var method = interfaceType.GetMethod("CompleteTextAsync");
        
        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<AITextResponse>), method.ReturnType);
        
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(AITextRequest), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.True(parameters[1].HasDefaultValue);
    }
}