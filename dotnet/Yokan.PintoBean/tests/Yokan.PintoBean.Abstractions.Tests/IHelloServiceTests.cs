using Yokan.PintoBean.Abstractions;

namespace Yokan.PintoBean.Abstractions.Tests;

/// <summary>
/// Tests for IHelloService interface contract.
/// </summary>
public class IHelloServiceTests
{
    [Fact]
    public void IHelloService_ShouldHaveCorrectMethods()
    {
        // Arrange
        var serviceType = typeof(IHelloService);

        // Act
        var sayHelloMethod = serviceType.GetMethod(nameof(IHelloService.SayHelloAsync));
        var sayGoodbyeMethod = serviceType.GetMethod(nameof(IHelloService.SayGoodbyeAsync));

        // Assert
        Assert.NotNull(sayHelloMethod);
        Assert.NotNull(sayGoodbyeMethod);
        
        // Verify method signatures
        Assert.Equal(typeof(Task<HelloResponse>), sayHelloMethod.ReturnType);
        Assert.Equal(typeof(Task<HelloResponse>), sayGoodbyeMethod.ReturnType);
        
        // Verify parameters
        var sayHelloParams = sayHelloMethod.GetParameters();
        Assert.Equal(2, sayHelloParams.Length);
        Assert.Equal(typeof(HelloRequest), sayHelloParams[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), sayHelloParams[1].ParameterType);
        
        var sayGoodbyeParams = sayGoodbyeMethod.GetParameters();
        Assert.Equal(2, sayGoodbyeParams.Length);
        Assert.Equal(typeof(HelloRequest), sayGoodbyeParams[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), sayGoodbyeParams[1].ParameterType);
    }

    [Fact]
    public void IHelloService_ShouldBeInterface()
    {
        // Arrange
        var serviceType = typeof(IHelloService);

        // Act & Assert
        Assert.True(serviceType.IsInterface);
        Assert.True(serviceType.IsPublic);
    }
}