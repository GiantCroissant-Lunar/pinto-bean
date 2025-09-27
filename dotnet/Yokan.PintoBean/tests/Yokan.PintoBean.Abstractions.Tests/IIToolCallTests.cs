using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Yokan.PintoBean.Abstractions;

namespace Yokan.PintoBean.Abstractions.Tests;

/// <summary>
/// Tests for IIToolCall interface contract.
/// </summary>
public class IIToolCallTests
{
    [Fact]
    public void IIToolCall_ShouldHaveCorrectMethods()
    {
        // Arrange
        var serviceType = typeof(IIToolCall);

        // Act
        var planToolCallsMethod = serviceType.GetMethod(nameof(IIToolCall.PlanToolCallsAsync));
        var executeToolCallMethod = serviceType.GetMethod(nameof(IIToolCall.ExecuteToolCallAsync));
        var executeToolCallsBatchMethod = serviceType.GetMethod(nameof(IIToolCall.ExecuteToolCallsBatchAsync));
        var synthesizeResponseMethod = serviceType.GetMethod(nameof(IIToolCall.SynthesizeResponseAsync));
        var validateToolDefinitionMethod = serviceType.GetMethod(nameof(IIToolCall.ValidateToolDefinitionAsync));
        var registerToolMethod = serviceType.GetMethod(nameof(IIToolCall.RegisterToolAsync));
        var getRegisteredToolsMethod = serviceType.GetMethod(nameof(IIToolCall.GetRegisteredToolsAsync));

        // Assert
        Assert.NotNull(planToolCallsMethod);
        Assert.NotNull(executeToolCallMethod);
        Assert.NotNull(executeToolCallsBatchMethod);
        Assert.NotNull(synthesizeResponseMethod);
        Assert.NotNull(validateToolDefinitionMethod);
        Assert.NotNull(registerToolMethod);
        Assert.NotNull(getRegisteredToolsMethod);

        // Verify method signatures
        Assert.Equal(typeof(Task<ToolCallResponse>), planToolCallsMethod.ReturnType);
        Assert.Equal(typeof(Task<ToolCallResult>), executeToolCallMethod.ReturnType);
        Assert.Equal(typeof(Task<IEnumerable<ToolCallResult>>), executeToolCallsBatchMethod.ReturnType);
        Assert.Equal(typeof(Task<ToolCallResponse>), synthesizeResponseMethod.ReturnType);
        Assert.Equal(typeof(Task<bool>), validateToolDefinitionMethod.ReturnType);
        Assert.Equal(typeof(Task<bool>), registerToolMethod.ReturnType);
        Assert.Equal(typeof(Task<IEnumerable<ToolDefinition>>), getRegisteredToolsMethod.ReturnType);
    }

    [Fact]
    public void IIToolCall_PlanToolCallsAsync_ShouldHaveCorrectParameters()
    {
        // Arrange
        var serviceType = typeof(IIToolCall);
        var method = serviceType.GetMethod(nameof(IIToolCall.PlanToolCallsAsync));

        // Act
        var parameters = method!.GetParameters();

        // Assert
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(ToolCallRequest), parameters[0].ParameterType);
        Assert.Equal("request", parameters[0].Name);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.Equal("cancellationToken", parameters[1].Name);
        Assert.True(parameters[1].HasDefaultValue);
    }

    [Fact]
    public void IIToolCall_ExecuteToolCallAsync_ShouldHaveCorrectParameters()
    {
        // Arrange
        var serviceType = typeof(IIToolCall);
        var method = serviceType.GetMethod(nameof(IIToolCall.ExecuteToolCallAsync));

        // Act
        var parameters = method!.GetParameters();

        // Assert
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(ToolCall), parameters[0].ParameterType);
        Assert.Equal("toolCall", parameters[0].Name);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.Equal("cancellationToken", parameters[1].Name);
        Assert.True(parameters[1].HasDefaultValue);
    }

    [Fact]
    public void IIToolCall_ExecuteToolCallsBatchAsync_ShouldHaveCorrectParameters()
    {
        // Arrange
        var serviceType = typeof(IIToolCall);
        var method = serviceType.GetMethod(nameof(IIToolCall.ExecuteToolCallsBatchAsync));

        // Act
        var parameters = method!.GetParameters();

        // Assert
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(IEnumerable<ToolCall>), parameters[0].ParameterType);
        Assert.Equal("toolCalls", parameters[0].Name);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.Equal("cancellationToken", parameters[1].Name);
        Assert.True(parameters[1].HasDefaultValue);
    }

    [Fact]
    public void IIToolCall_SynthesizeResponseAsync_ShouldHaveCorrectParameters()
    {
        // Arrange
        var serviceType = typeof(IIToolCall);
        var method = serviceType.GetMethod(nameof(IIToolCall.SynthesizeResponseAsync));

        // Act
        var parameters = method!.GetParameters();

        // Assert
        Assert.Equal(3, parameters.Length);
        Assert.Equal(typeof(ToolCallRequest), parameters[0].ParameterType);
        Assert.Equal("originalRequest", parameters[0].Name);
        Assert.Equal(typeof(IEnumerable<ToolCallResult>), parameters[1].ParameterType);
        Assert.Equal("toolResults", parameters[1].Name);
        Assert.Equal(typeof(CancellationToken), parameters[2].ParameterType);
        Assert.Equal("cancellationToken", parameters[2].Name);
        Assert.True(parameters[2].HasDefaultValue);
    }

    [Fact]
    public void IIToolCall_ValidateToolDefinitionAsync_ShouldHaveCorrectParameters()
    {
        // Arrange
        var serviceType = typeof(IIToolCall);
        var method = serviceType.GetMethod(nameof(IIToolCall.ValidateToolDefinitionAsync));

        // Act
        var parameters = method!.GetParameters();

        // Assert
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(ToolDefinition), parameters[0].ParameterType);
        Assert.Equal("tool", parameters[0].Name);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.Equal("cancellationToken", parameters[1].Name);
        Assert.True(parameters[1].HasDefaultValue);
    }

    [Fact]
    public void IIToolCall_RegisterToolAsync_ShouldHaveCorrectParameters()
    {
        // Arrange
        var serviceType = typeof(IIToolCall);
        var method = serviceType.GetMethod(nameof(IIToolCall.RegisterToolAsync));

        // Act
        var parameters = method!.GetParameters();

        // Assert
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(ToolDefinition), parameters[0].ParameterType);
        Assert.Equal("tool", parameters[0].Name);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.Equal("cancellationToken", parameters[1].Name);
        Assert.True(parameters[1].HasDefaultValue);
    }

    [Fact]
    public void IIToolCall_GetRegisteredToolsAsync_ShouldHaveCorrectParameters()
    {
        // Arrange
        var serviceType = typeof(IIToolCall);
        var method = serviceType.GetMethod(nameof(IIToolCall.GetRegisteredToolsAsync));

        // Act
        var parameters = method!.GetParameters();

        // Assert
        Assert.Single(parameters);
        Assert.Equal(typeof(CancellationToken), parameters[0].ParameterType);
        Assert.Equal("cancellationToken", parameters[0].Name);
        Assert.True(parameters[0].HasDefaultValue);
    }

    [Fact]
    public void IIToolCall_ShouldBeInterface()
    {
        // Arrange
        var serviceType = typeof(IIToolCall);

        // Act & Assert
        Assert.True(serviceType.IsInterface);
        Assert.True(serviceType.IsPublic);
    }

    [Fact]
    public void IIToolCall_ShouldHaveCorrectNamespace()
    {
        // Arrange
        var serviceType = typeof(IIToolCall);

        // Act & Assert
        Assert.Equal("Yokan.PintoBean.Abstractions", serviceType.Namespace);
    }

    [Fact]
    public void IIToolCall_ShouldFollowNamingConvention()
    {
        // Arrange
        var serviceType = typeof(IIToolCall);

        // Act & Assert
        Assert.Equal("IIToolCall", serviceType.Name);
        Assert.StartsWith("I", serviceType.Name);
    }
}