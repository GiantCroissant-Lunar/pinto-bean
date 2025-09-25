using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yokan.PintoBean.Runtime;

// Alias to avoid confusion with the NoOpResilienceExecutor in IntegrationTests.cs
using RuntimeNoOpExecutor = Yokan.PintoBean.Runtime.NoOpResilienceExecutor;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Unit tests for resilience executor functionality.
/// </summary>
public class ResilienceExecutorTests
{
    [Fact]
    public void RuntimeNoOpExecutor_Execute_InvokesDelegate()
    {
        // Arrange
        var executor = RuntimeNoOpExecutor.Instance;
        var expected = 42;
        var wasCalled = false;

        // Act
        var result = executor.Execute(() =>
        {
            wasCalled = true;
            return expected;
        });

        // Assert
        Assert.True(wasCalled);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void RuntimeNoOpExecutor_Execute_PropagatesException()
    {
        // Arrange
        var executor = RuntimeNoOpExecutor.Instance;
        var expectedException = new InvalidOperationException("Test exception");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            executor.Execute<int>(() => throw expectedException));

        Assert.Same(expectedException, exception);
    }

    [Fact]
    public void RuntimeNoOpExecutor_Execute_ThrowsArgumentNullException_WhenFuncIsNull()
    {
        // Arrange
        var executor = RuntimeNoOpExecutor.Instance;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => executor.Execute<int>(null!));
    }

    [Fact]
    public async Task RuntimeNoOpExecutor_ExecuteAsync_WithResult_InvokesDelegate()
    {
        // Arrange
        var executor = RuntimeNoOpExecutor.Instance;
        var expected = 42;
        var wasCalled = false;

        // Act
        var result = await executor.ExecuteAsync(async ct =>
        {
            wasCalled = true;
            await Task.Delay(1, ct);
            return expected;
        });

        // Assert
        Assert.True(wasCalled);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task RuntimeNoOpExecutor_ExecuteAsync_WithResult_PropagatesException()
    {
        // Arrange
        var executor = RuntimeNoOpExecutor.Instance;
        var expectedException = new InvalidOperationException("Test exception");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await executor.ExecuteAsync<int>(async ct =>
            {
                await Task.Delay(1, ct);
                throw expectedException;
            }));

        Assert.Same(expectedException, exception);
    }

    [Fact]
    public async Task RuntimeNoOpExecutor_ExecuteAsync_WithResult_PassesCancellationToken()
    {
        // Arrange
        var executor = RuntimeNoOpExecutor.Instance;
        using var cts = new CancellationTokenSource();
        var receivedToken = CancellationToken.None;

        // Act
        await executor.ExecuteAsync(async ct =>
        {
            receivedToken = ct;
            await Task.Delay(1, ct);
            return 42;
        }, cts.Token);

        // Assert
        Assert.Equal(cts.Token, receivedToken);
    }

    [Fact]
    public async Task RuntimeNoOpExecutor_ExecuteAsync_WithResult_ThrowsArgumentNullException_WhenFuncIsNull()
    {
        // Arrange
        var executor = RuntimeNoOpExecutor.Instance;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => executor.ExecuteAsync<int>(null!));
    }

    [Fact]
    public async Task RuntimeNoOpExecutor_ExecuteAsync_WithoutResult_InvokesDelegate()
    {
        // Arrange
        var executor = RuntimeNoOpExecutor.Instance;
        var wasCalled = false;

        // Act
        await executor.ExecuteAsync(async ct =>
        {
            wasCalled = true;
            await Task.Delay(1, ct);
        });

        // Assert
        Assert.True(wasCalled);
    }

    [Fact]
    public async Task RuntimeNoOpExecutor_ExecuteAsync_WithoutResult_PropagatesException()
    {
        // Arrange
        var executor = RuntimeNoOpExecutor.Instance;
        var expectedException = new InvalidOperationException("Test exception");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await executor.ExecuteAsync(async ct =>
            {
                await Task.Delay(1, ct);
                throw expectedException;
            }));

        Assert.Same(expectedException, exception);
    }

    [Fact]
    public async Task RuntimeNoOpExecutor_ExecuteAsync_WithoutResult_PassesCancellationToken()
    {
        // Arrange
        var executor = RuntimeNoOpExecutor.Instance;
        using var cts = new CancellationTokenSource();
        var receivedToken = CancellationToken.None;

        // Act
        await executor.ExecuteAsync(async ct =>
        {
            receivedToken = ct;
            await Task.Delay(1, ct);
        }, cts.Token);

        // Assert
        Assert.Equal(cts.Token, receivedToken);
    }

    [Fact]
    public async Task RuntimeNoOpExecutor_ExecuteAsync_WithoutResult_ThrowsArgumentNullException_WhenFuncIsNull()
    {
        // Arrange
        var executor = RuntimeNoOpExecutor.Instance;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => executor.ExecuteAsync(null!));
    }

    [Fact]
    public void RuntimeNoOpExecutor_Instance_IsSingleton()
    {
        // Arrange & Act
        var instance1 = RuntimeNoOpExecutor.Instance;
        var instance2 = RuntimeNoOpExecutor.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void AddResilienceExecutor_RegistersDefaultImplementation()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddResilienceExecutor();
        var serviceProvider = services.BuildServiceProvider();
        var executor = serviceProvider.GetService<IResilienceExecutor>();

        // Assert
        Assert.NotNull(executor);
        Assert.IsType<RuntimeNoOpExecutor>(executor);
        Assert.Same(RuntimeNoOpExecutor.Instance, executor);
    }

    [Fact]
    public void AddResilienceExecutor_WithGenericType_RegistersCustomImplementation()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddResilienceExecutor<TestResilienceExecutor>();
        var serviceProvider = services.BuildServiceProvider();
        var executor = serviceProvider.GetService<IResilienceExecutor>();

        // Assert
        Assert.NotNull(executor);
        Assert.IsType<TestResilienceExecutor>(executor);
    }

    [Fact]
    public void AddResilienceExecutor_WithFactory_RegistersFactoryResult()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedExecutor = new TestResilienceExecutor();

        // Act
        services.AddResilienceExecutor(sp => expectedExecutor);
        var serviceProvider = services.BuildServiceProvider();
        var executor = serviceProvider.GetService<IResilienceExecutor>();

        // Assert
        Assert.NotNull(executor);
        Assert.Same(expectedExecutor, executor);
    }

    [Fact]
    public void AddResilienceExecutor_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ServiceCollectionExtensions.AddResilienceExecutor(null!));
    }

    [Fact]
    public void AddResilienceExecutor_WithGenericType_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ServiceCollectionExtensions.AddResilienceExecutor<TestResilienceExecutor>(null!));
    }

    [Fact]
    public void AddResilienceExecutor_WithFactory_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ServiceCollectionExtensions.AddResilienceExecutor(null!, sp => RuntimeNoOpExecutor.Instance));
    }

    [Fact]
    public void AddResilienceExecutor_WithFactory_ThrowsArgumentNullException_WhenFactoryIsNull()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            services.AddResilienceExecutor(null!));
    }

    [Fact]
    public void AddResilienceExecutor_OnlyRegistersOnce_WhenCalledMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddResilienceExecutor();
        services.AddResilienceExecutor<TestResilienceExecutor>(); // This should not replace the first registration

        var serviceProvider = services.BuildServiceProvider();
        var executor = serviceProvider.GetService<IResilienceExecutor>();

        // Assert - Should still be the first registration (RuntimeNoOpExecutor)
        Assert.NotNull(executor);
        Assert.IsType<RuntimeNoOpExecutor>(executor);
    }

    /// <summary>
    /// Test implementation of IResilienceExecutor for testing purposes.
    /// </summary>
    private class TestResilienceExecutor : IResilienceExecutor
    {
        public TResult Execute<TResult>(Func<TResult> func) => func();

        public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> func, CancellationToken cancellationToken = default) 
            => func(cancellationToken);

        public Task ExecuteAsync(Func<CancellationToken, Task> func, CancellationToken cancellationToken = default) 
            => func(cancellationToken);
    }
}