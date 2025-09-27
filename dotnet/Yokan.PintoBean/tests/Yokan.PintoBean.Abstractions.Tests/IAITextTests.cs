using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        // Assert
        Assert.NotNull(generateTextMethod);
        Assert.NotNull(generateTextStreamMethod);
        Assert.NotNull(continueConversationMethod);
        Assert.NotNull(continueConversationStreamMethod);

        // Verify method signatures
        Assert.Equal(typeof(Task<AITextResponse>), generateTextMethod.ReturnType);
        Assert.Equal(typeof(IAsyncEnumerable<AITextResponse>), generateTextStreamMethod.ReturnType);
        Assert.Equal(typeof(Task<AITextResponse>), continueConversationMethod.ReturnType);
        Assert.Equal(typeof(IAsyncEnumerable<AITextResponse>), continueConversationStreamMethod.ReturnType);
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
    public void IAIText_ShouldBeInterface()
    {
        // Arrange
        var serviceType = typeof(IAIText);

        // Act & Assert
        Assert.True(serviceType.IsInterface);
        Assert.True(serviceType.IsPublic);
    }

    [Fact]
    public void IAIText_ShouldHaveCorrectNamespace()
    {
        // Arrange
        var serviceType = typeof(IAIText);

        // Act & Assert
        Assert.Equal("Yokan.PintoBean.Abstractions", serviceType.Namespace);
    }

    [Fact]
    public void IAIText_ShouldFollowNamingConvention()
    {
        // Arrange
        var serviceType = typeof(IAIText);

        // Act & Assert
        Assert.Equal("IAIText", serviceType.Name);
        Assert.StartsWith("I", serviceType.Name);
    }
}