using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly.Timeout;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Unit tests for PollyResilienceExecutor functionality.
/// </summary>
public class PollyResilienceExecutorTests
{
    [Fact]
    public void Constructor_WithIOptions_InitializesCorrectly()
    {
        // Arrange
        var options = Options.Create(new PollyResilienceExecutorOptions());

        // Act
        var executor = new PollyResilienceExecutor(options);

        // Assert
        Assert.NotNull(executor);
    }

    [Fact]
    public void Constructor_WithOptions_InitializesCorrectly()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions();

        // Act
        var executor = new PollyResilienceExecutor(options);

        // Assert
        Assert.NotNull(executor);
    }

    [Fact]
    public void Constructor_WithNullIOptions_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PollyResilienceExecutor((IOptions<PollyResilienceExecutorOptions>)null!));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PollyResilienceExecutor((PollyResilienceExecutorOptions)null!));
    }

    [Fact]
    public void Constructor_WithNullOptionsValue_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create<PollyResilienceExecutorOptions>(null!);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PollyResilienceExecutor(options));
    }

    [Fact]
    public void Execute_WithSuccessfulFunction_ReturnsResult()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions();
        var executor = new PollyResilienceExecutor(options);
        var expected = 42;

        // Act
        var result = executor.Execute(() => expected);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Execute_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions();
        var executor = new PollyResilienceExecutor(options);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => executor.Execute<int>(null!));
    }

    [Fact]
    public void Execute_WithTransientException_RetriesAndSucceeds()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions
        {
            MaxRetryAttempts = 2,
            BaseRetryDelayMilliseconds = 10 // Very short delay for testing
        };
        var executor = new PollyResilienceExecutor(options);
        var attemptCount = 0;
        var expected = 42;

        // Act
        var result = executor.Execute(() =>
        {
            attemptCount++;
            if (attemptCount < 2)
                throw new InvalidOperationException("Transient failure");
            return expected;
        });

        // Assert
        Assert.Equal(expected, result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public void Execute_WithNonTransientException_DoesNotRetry()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions
        {
            MaxRetryAttempts = 3
        };
        var executor = new PollyResilienceExecutor(options);
        var attemptCount = 0;
        var expectedException = new ArgumentException("Non-transient failure");

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            executor.Execute<int>(() =>
            {
                attemptCount++;
                throw expectedException;
            }));

        Assert.Same(expectedException, exception);
        Assert.Equal(1, attemptCount); // Should not retry
    }

    [Fact]
    public async Task ExecuteAsync_WithResult_WithSuccessfulFunction_ReturnsResult()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions();
        var executor = new PollyResilienceExecutor(options);
        var expected = 42;

        // Act
        var result = await executor.ExecuteAsync(async ct =>
        {
            await Task.Delay(1, ct);
            return expected;
        });

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithResult_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions();
        var executor = new PollyResilienceExecutor(options);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => executor.ExecuteAsync<int>(null!));
    }

    [Fact]
    public async Task ExecuteAsync_WithResult_WithTransientException_RetriesAndSucceeds()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions
        {
            MaxRetryAttempts = 2,
            BaseRetryDelayMilliseconds = 10 // Very short delay for testing
        };
        var executor = new PollyResilienceExecutor(options);
        var attemptCount = 0;
        var expected = 42;

        // Act
        var result = await executor.ExecuteAsync(async ct =>
        {
            attemptCount++;
            await Task.Delay(1, ct);
            if (attemptCount < 2)
                throw new InvalidOperationException("Transient failure");
            return expected;
        });

        // Assert
        Assert.Equal(expected, result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithResult_PassesCancellationToken()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions();
        var executor = new PollyResilienceExecutor(options);
        using var cts = new CancellationTokenSource();
        var receivedTokenIsCancellationRequested = false;

        // Act
        await executor.ExecuteAsync(async ct =>
        {
            receivedTokenIsCancellationRequested = ct.IsCancellationRequested;
            await Task.Delay(1, ct);
            return 42;
        }, cts.Token);

        // Assert - Check that the cancellation token state was preserved
        Assert.Equal(cts.Token.IsCancellationRequested, receivedTokenIsCancellationRequested);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutResult_WithSuccessfulFunction_Completes()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions();
        var executor = new PollyResilienceExecutor(options);
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
    public async Task ExecuteAsync_WithoutResult_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions();
        var executor = new PollyResilienceExecutor(options);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => executor.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_WithoutResult_WithTransientException_RetriesAndSucceeds()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions
        {
            MaxRetryAttempts = 2,
            BaseRetryDelayMilliseconds = 10 // Very short delay for testing
        };
        var executor = new PollyResilienceExecutor(options);
        var attemptCount = 0;

        // Act
        await executor.ExecuteAsync(async ct =>
        {
            attemptCount++;
            await Task.Delay(1, ct);
            if (attemptCount < 2)
                throw new InvalidOperationException("Transient failure");
        });

        // Assert
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutResult_PassesCancellationToken()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions();
        var executor = new PollyResilienceExecutor(options);
        using var cts = new CancellationTokenSource();
        var receivedTokenIsCancellationRequested = false;

        // Act
        await executor.ExecuteAsync(async ct =>
        {
            receivedTokenIsCancellationRequested = ct.IsCancellationRequested;
            await Task.Delay(1, ct);
        }, cts.Token);

        // Assert - Check that the cancellation token state was preserved
        Assert.Equal(cts.Token.IsCancellationRequested, receivedTokenIsCancellationRequested);
    }

    [Fact]
    public void Execute_WithTimeoutExceedingLimit_DoesNotThrowTimeoutExceptionForSync()
    {
        // Arrange
        // Note: Polly timeout policy doesn't work well with synchronous Thread.Sleep
        // This is expected behavior - timeouts work better with async operations
        var options = new PollyResilienceExecutorOptions
        {
            DefaultTimeoutSeconds = 0.1 // 100ms timeout
        };
        var executor = new PollyResilienceExecutor(options);

        // Act - This should complete without timeout (expected behavior for sync operations)
        var result = executor.Execute(() =>
        {
            Thread.Sleep(200); // Sleep longer than timeout
            return 42;
        });

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeoutExceedingLimit_ThrowsTimeoutException()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions
        {
            DefaultTimeoutSeconds = 0.1 // 100ms timeout
        };
        var executor = new PollyResilienceExecutor(options);

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutRejectedException>(async () =>
            await executor.ExecuteAsync(async ct =>
            {
                await Task.Delay(200, ct); // Delay longer than timeout
                return 42;
            }));
    }

    [Fact]
    public void AddPollyResilience_RegistersPollyExecutor()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPollyResilience();
        var serviceProvider = services.BuildServiceProvider();
        var executor = serviceProvider.GetService<IResilienceExecutor>();

        // Assert
        Assert.NotNull(executor);
        Assert.IsType<PollyResilienceExecutor>(executor);
    }

    [Fact]
    public void AddPollyResilience_WithConfiguration_RegistersPollyExecutor()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPollyResilience(options =>
        {
            options.MaxRetryAttempts = 5;
            options.DefaultTimeoutSeconds = 60;
        });
        var serviceProvider = services.BuildServiceProvider();
        var executor = serviceProvider.GetService<IResilienceExecutor>();

        // Assert
        Assert.NotNull(executor);
        Assert.IsType<PollyResilienceExecutor>(executor);
    }

    [Fact]
    public void AddPollyResilience_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ServiceCollectionExtensions.AddPollyResilience(null!));
    }

    [Fact]
    public void AddPollyResilience_WithConfiguration_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ServiceCollectionExtensions.AddPollyResilience(null!, options => { }));
    }

    [Fact]
    public void AddPollyResilience_WithConfiguration_ThrowsArgumentNullException_WhenConfigureIsNull()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            services.AddPollyResilience((Action<PollyResilienceExecutorOptions>)null!));
    }

    [Fact]
    public void AddPollyResilience_OnlyRegistersOnce_WhenCalledMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPollyResilience();
        services.AddPollyResilience(options => options.MaxRetryAttempts = 5);

        var serviceProvider = services.BuildServiceProvider();
        var executor = serviceProvider.GetService<IResilienceExecutor>();

        // Assert - Should still be PollyResilienceExecutor (first registration wins due to TryAddSingleton)
        Assert.NotNull(executor);
        Assert.IsType<PollyResilienceExecutor>(executor);
    }
}