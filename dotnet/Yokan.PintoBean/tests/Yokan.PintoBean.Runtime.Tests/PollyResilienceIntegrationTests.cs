using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Timeout;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Integration tests for PollyResilienceExecutor with dependency injection and configuration.
/// </summary>
public class PollyResilienceIntegrationTests
{
    [Fact]
    public void ServiceCollection_AddPollyResilience_WithDefaultOptions_ResolvesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPollyResilience();

        // Act
        using var provider = services.BuildServiceProvider();
        var executor = provider.GetRequiredService<IResilienceExecutor>();

        // Assert
        Assert.NotNull(executor);
        Assert.IsType<PollyResilienceExecutor>(executor);
    }

    [Fact]
    public void ServiceCollection_AddPollyResilience_WithCustomOptions_ResolvesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPollyResilience(options =>
        {
            options.MaxRetryAttempts = 5;
            options.DefaultTimeoutSeconds = 60;
            options.BaseRetryDelayMilliseconds = 500;
            options.SetCategoryTimeout(ServiceCategory.Analytics, 30);
            options.SetOperationTimeout("critical-operation", 120);
        });

        // Act
        using var provider = services.BuildServiceProvider();
        var executor = provider.GetRequiredService<IResilienceExecutor>();

        // Assert
        Assert.NotNull(executor);
        Assert.IsType<PollyResilienceExecutor>(executor);
    }

    [Fact]
    public void ServiceCollection_AddPollyResilience_WithConfiguration_ResolvesCorrectly()
    {
        // Arrange
        var configurationData = new[]
        {
            new KeyValuePair<string, string?>("Polly:MaxRetryAttempts", "5"),
            new KeyValuePair<string, string?>("Polly:DefaultTimeoutSeconds", "45"),
            new KeyValuePair<string, string?>("Polly:BaseRetryDelayMilliseconds", "2000"),
            new KeyValuePair<string, string?>("Polly:EnableCircuitBreaker", "true"),
            new KeyValuePair<string, string?>("Polly:CircuitBreakerFailureThreshold", "10")
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        var services = new ServiceCollection();
        services.AddPollyResilience(configuration.GetSection("Polly"));

        // Act
        using var provider = services.BuildServiceProvider();
        var executor = provider.GetRequiredService<IResilienceExecutor>();

        // Assert
        Assert.NotNull(executor);
        Assert.IsType<PollyResilienceExecutor>(executor);
    }

    [Fact]
    public async Task PollyResilienceExecutor_IntegrationWithRetry_WorksAsExpected()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPollyResilience(options =>
        {
            options.MaxRetryAttempts = 3;
            options.BaseRetryDelayMilliseconds = 10; // Very short for testing
        });

        using var provider = services.BuildServiceProvider();
        var executor = provider.GetRequiredService<IResilienceExecutor>();

        var attemptCount = 0;

        // Act - Test retry functionality
        var result = await executor.ExecuteAsync(async ct =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new InvalidOperationException("Simulated transient failure");
            }
            await Task.Delay(1, ct);
            return "success";
        });

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(3, attemptCount); // Should have retried 2 times (3 total attempts)
    }

    [Fact]
    public async Task PollyResilienceExecutor_IntegrationWithTimeout_WorksAsExpected()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPollyResilience(options =>
        {
            options.DefaultTimeoutSeconds = 0.2; // 200ms timeout
        });

        using var provider = services.BuildServiceProvider();
        var executor = provider.GetRequiredService<IResilienceExecutor>();

        // Act & Assert - Test timeout functionality for async operations
        await Assert.ThrowsAsync<TimeoutRejectedException>(async () =>
            await executor.ExecuteAsync(async ct =>
            {
                await Task.Delay(500, ct); // Longer than timeout
                return "should not complete";
            }));
    }

    [Fact]
    public async Task PollyResilienceExecutor_IntegrationWithCategoryTimeout_WorksAsExpected()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPollyResilience(options =>
        {
            options.DefaultTimeoutSeconds = 10; // Long default
            options.SetCategoryTimeout(ServiceCategory.Analytics, 0.1); // Short analytics timeout
        });

        using var provider = services.BuildServiceProvider();
        var executor = provider.GetRequiredService<IResilienceExecutor>();

        // Act - Execute a quick operation (should succeed)
        var quickResult = await executor.ExecuteAsync(async ct =>
        {
            await Task.Delay(10, ct); // Well under timeout
            return "quick success";
        });

        // Assert
        Assert.Equal("quick success", quickResult);
    }

    [Fact]
    public void ServiceCollection_AddPollyResilience_DoesNotOverrideExistingRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var customExecutor = NoOpResilienceExecutor.Instance; // Use the singleton instance
        services.AddSingleton<IResilienceExecutor>(customExecutor);

        // Act - Try to add Polly after existing registration
        services.AddPollyResilience();

        // Assert - Should still use the first registration due to TryAddSingleton
        using var provider = services.BuildServiceProvider();
        var executor = provider.GetRequiredService<IResilienceExecutor>();
        Assert.Same(customExecutor, executor);
    }

    [Fact]
    public void ServiceCollection_AddPollyResilience_BeforeOtherRegistration_TakesPrecedence()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Add Polly first, then try to add NoOp
        services.AddPollyResilience();
        services.AddResilienceExecutor(); // This should not override due to TryAddSingleton

        // Assert - Should use PollyResilienceExecutor (first registration wins)
        using var provider = services.BuildServiceProvider();
        var executor = provider.GetRequiredService<IResilienceExecutor>();
        Assert.IsType<PollyResilienceExecutor>(executor);
    }
}