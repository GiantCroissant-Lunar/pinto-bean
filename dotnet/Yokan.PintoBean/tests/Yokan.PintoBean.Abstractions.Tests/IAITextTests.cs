using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Yokan.PintoBean.Abstractions;

namespace Yokan.PintoBean.Abstractions.Tests;

/// <summary>
/// Tests for IAIText interface contract.
/// </summary>
public class IAITextTests
{
    [Fact]
    public void IAIText_ShouldHaveCorrectMethods()
    {
        // Arrange
        var serviceType = typeof(IAIText);

        // Act
        var generateTextMethod = serviceType.GetMethod(nameof(IAIText.GenerateTextAsync));
        var generateTextStreamMethod = serviceType.GetMethod(nameof(IAIText.GenerateTextStreamAsync));
        var continueConversationMethod = serviceType.GetMethod(nameof(IAIText.ContinueConversationAsync));
        var continueConversationStreamMethod = serviceType.GetMethod(nameof(IAIText.ContinueConversationStreamAsync));
        var completeTextMethod = serviceType.GetMethod(nameof(IAIText.CompleteTextAsync));

        // Assert
        Assert.NotNull(generateTextMethod);
        Assert.NotNull(generateTextStreamMethod);
        Assert.NotNull(continueConversationMethod);
        Assert.NotNull(continueConversationStreamMethod);
        Assert.NotNull(completeTextMethod);

        // Verify return types
        Assert.Equal(typeof(Task<AITextResponse>), generateTextMethod.ReturnType);
        Assert.Equal(typeof(IAsyncEnumerable<AITextResponse>), generateTextStreamMethod.ReturnType);
        Assert.Equal(typeof(Task<AITextResponse>), continueConversationMethod.ReturnType);
        Assert.Equal(typeof(IAsyncEnumerable<AITextResponse>), continueConversationStreamMethod.ReturnType);
        Assert.Equal(typeof(Task<AITextResponse>), completeTextMethod.ReturnType);
    }

    [Fact]
    public void IAIText_GenerateTextAsync_ShouldHaveCorrectParameters()
    {
        // Arrange
        var serviceType = typeof(IAIText);
        var method = serviceType.GetMethod(nameof(IAIText.GenerateTextAsync));

        // Act
        var parameters = method!.GetParameters();

        // Assert
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(AITextRequest), parameters[0].ParameterType);
        Assert.Equal("request", parameters[0].Name);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.Equal("cancellationToken", parameters[1].Name);
        Assert.True(parameters[1].HasDefaultValue);
    }

    [Fact]
    public void IAIText_GenerateTextStreamAsync_ShouldHaveCorrectParameters()
    {
        // Arrange
        var serviceType = typeof(IAIText);
        var method = serviceType.GetMethod(nameof(IAIText.GenerateTextStreamAsync));

        // Act
        var parameters = method!.GetParameters();

        // Assert
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(AITextRequest), parameters[0].ParameterType);
        Assert.Equal("request", parameters[0].Name);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.Equal("cancellationToken", parameters[1].Name);
        Assert.True(parameters[1].HasDefaultValue);
    }

    [Fact]
    public void IAIText_ContinueConversationAsync_ShouldHaveCorrectParameters()
    {
        // Arrange
        var serviceType = typeof(IAIText);
        var method = serviceType.GetMethod(nameof(IAIText.ContinueConversationAsync));

        // Act
        var parameters = method!.GetParameters();

        // Assert
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(AITextRequest), parameters[0].ParameterType);
        Assert.Equal("request", parameters[0].Name);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.Equal("cancellationToken", parameters[1].Name);
        Assert.True(parameters[1].HasDefaultValue);
    }

    [Fact]
    public void IAIText_ContinueConversationStreamAsync_ShouldHaveCorrectParameters()
    {
        // Arrange
        var serviceType = typeof(IAIText);
        var method = serviceType.GetMethod(nameof(IAIText.ContinueConversationStreamAsync));

        // Act
        var parameters = method!.GetParameters();

        // Assert
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(AITextRequest), parameters[0].ParameterType);
        Assert.Equal("request", parameters[0].Name);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.Equal("cancellationToken", parameters[1].Name);
        Assert.True(parameters[1].HasDefaultValue);
    }

    [Fact]
    public void IAIText_CompleteTextAsync_ShouldHaveCorrectParameters()
    {
        // Arrange
        var serviceType = typeof(IAIText);
        var method = serviceType.GetMethod(nameof(IAIText.CompleteTextAsync));

        // Act
        var parameters = method!.GetParameters();

        // Assert
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(AITextRequest), parameters[0].ParameterType);
        Assert.Equal("request", parameters[0].Name);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.Equal("cancellationToken", parameters[1].Name);
        Assert.True(parameters[1].HasDefaultValue);
    }

    [Fact]
    public void IAIText_ShouldHaveExpectedMethods()
    {
        // Arrange
        var interfaceType = typeof(IAIText);
        
        // Act
        var methods = interfaceType.GetMethods();
        var methodNames = methods.Select(m => m.Name).ToList();
        
        // Assert - Should have all 5 methods
        Assert.Equal(5, methods.Length);
        Assert.Contains("GenerateTextAsync", methodNames);
        Assert.Contains("GenerateTextStreamAsync", methodNames);
        Assert.Contains("ContinueConversationAsync", methodNames);
        Assert.Contains("ContinueConversationStreamAsync", methodNames);
        Assert.Contains("CompleteTextAsync", methodNames);
    }
}
