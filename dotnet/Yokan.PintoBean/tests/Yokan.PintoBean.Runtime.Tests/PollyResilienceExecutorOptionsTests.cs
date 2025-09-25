using System;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Unit tests for PollyResilienceExecutorOptions functionality.
/// </summary>
public class PollyResilienceExecutorOptionsTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        // Act
        var options = new PollyResilienceExecutorOptions();

        // Assert
        Assert.Equal(30.0, options.DefaultTimeoutSeconds);
        Assert.Equal(3, options.MaxRetryAttempts);
        Assert.Equal(1000.0, options.BaseRetryDelayMilliseconds);
        Assert.False(options.EnableCircuitBreaker);
        Assert.Equal(5, options.CircuitBreakerFailureThreshold);
        Assert.Equal(30.0, options.CircuitBreakerDurationOfBreakSeconds);
        Assert.Equal(10.0, options.CircuitBreakerSamplingDurationSeconds);
        Assert.NotNull(options.CategoryTimeouts);
        Assert.Empty(options.CategoryTimeouts);
        Assert.NotNull(options.OperationTimeouts);
        Assert.Empty(options.OperationTimeouts);
    }

    [Fact]
    public void GetTimeoutSeconds_WithoutOverrides_ReturnsDefaultTimeout()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions
        {
            DefaultTimeoutSeconds = 45.0
        };

        // Act
        var timeout = options.GetTimeoutSeconds("someOperation", "someCategory");

        // Assert
        Assert.Equal(45.0, timeout);
    }

    [Fact]
    public void GetTimeoutSeconds_WithOperationOverride_ReturnsOperationTimeout()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions
        {
            DefaultTimeoutSeconds = 30.0
        };
        options.OperationTimeouts["testOperation"] = 60.0;
        options.CategoryTimeouts["testCategory"] = 90.0;

        // Act
        var timeout = options.GetTimeoutSeconds("testOperation", "testCategory");

        // Assert
        Assert.Equal(60.0, timeout); // Operation timeout takes precedence
    }

    [Fact]
    public void GetTimeoutSeconds_WithCategoryOverride_ReturnsCategoryTimeout()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions
        {
            DefaultTimeoutSeconds = 30.0
        };
        options.CategoryTimeouts["testCategory"] = 90.0;

        // Act
        var timeout = options.GetTimeoutSeconds("unknownOperation", "testCategory");

        // Assert
        Assert.Equal(90.0, timeout);
    }

    [Fact]
    public void GetTimeoutSeconds_WithNullOperationName_UsesCategoryOrDefault()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions
        {
            DefaultTimeoutSeconds = 30.0
        };
        options.CategoryTimeouts["testCategory"] = 90.0;

        // Act
        var timeoutWithCategory = options.GetTimeoutSeconds(null, "testCategory");
        var timeoutWithoutCategory = options.GetTimeoutSeconds(null, null);

        // Assert
        Assert.Equal(90.0, timeoutWithCategory);
        Assert.Equal(30.0, timeoutWithoutCategory);
    }

    [Fact]
    public void GetTimeoutSeconds_WithEmptyOperationName_UsesCategoryOrDefault()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions
        {
            DefaultTimeoutSeconds = 30.0
        };
        options.CategoryTimeouts["testCategory"] = 90.0;

        // Act
        var timeoutWithCategory = options.GetTimeoutSeconds("", "testCategory");
        var timeoutWithoutCategory = options.GetTimeoutSeconds("", "");

        // Assert
        Assert.Equal(90.0, timeoutWithCategory);
        Assert.Equal(30.0, timeoutWithoutCategory);
    }

    [Fact]
    public void SetCategoryTimeout_WithServiceCategory_SetsCorrectTimeout()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions();

        // Act
        var result = options.SetCategoryTimeout(ServiceCategory.Analytics, 120.0);

        // Assert
        Assert.Same(options, result); // Should return self for chaining
        Assert.Equal(120.0, options.CategoryTimeouts["Analytics"]);
    }

    [Fact]
    public void SetCategoryTimeout_SupportsMethodChaining()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions();

        // Act
        var result = options
            .SetCategoryTimeout(ServiceCategory.Analytics, 60.0)
            .SetCategoryTimeout(ServiceCategory.Resources, 120.0);

        // Assert
        Assert.Same(options, result);
        Assert.Equal(60.0, options.CategoryTimeouts["Analytics"]);
        Assert.Equal(120.0, options.CategoryTimeouts["Resources"]);
    }

    [Fact]
    public void SetOperationTimeout_WithValidOperationName_SetsCorrectTimeout()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions();

        // Act
        var result = options.SetOperationTimeout("testOperation", 45.0);

        // Assert
        Assert.Same(options, result); // Should return self for chaining
        Assert.Equal(45.0, options.OperationTimeouts["testOperation"]);
    }

    [Fact]
    public void SetOperationTimeout_WithNullOperationName_ThrowsArgumentException()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            options.SetOperationTimeout(null!, 45.0));
        Assert.Equal("operationName", exception.ParamName);
    }

    [Fact]
    public void SetOperationTimeout_WithEmptyOperationName_ThrowsArgumentException()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            options.SetOperationTimeout("", 45.0));
        Assert.Equal("operationName", exception.ParamName);
    }

    [Fact]
    public void SetOperationTimeout_SupportsMethodChaining()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions();

        // Act
        var result = options
            .SetOperationTimeout("operation1", 30.0)
            .SetOperationTimeout("operation2", 60.0);

        // Assert
        Assert.Same(options, result);
        Assert.Equal(30.0, options.OperationTimeouts["operation1"]);
        Assert.Equal(60.0, options.OperationTimeouts["operation2"]);
    }

    [Fact]
    public void GetTimeoutSeconds_PriorityOrder_OperationOverridesCategoryOverridesDefault()
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions
        {
            DefaultTimeoutSeconds = 10.0
        };
        options.CategoryTimeouts["testCategory"] = 20.0;
        options.OperationTimeouts["testOperation"] = 30.0;

        // Act & Assert
        Assert.Equal(30.0, options.GetTimeoutSeconds("testOperation", "testCategory")); // Operation wins
        Assert.Equal(20.0, options.GetTimeoutSeconds("otherOperation", "testCategory")); // Category wins
        Assert.Equal(10.0, options.GetTimeoutSeconds("otherOperation", "otherCategory")); // Default wins
    }

    [Theory]
    [InlineData(ServiceCategory.Analytics)]
    [InlineData(ServiceCategory.Resources)]
    [InlineData(ServiceCategory.SceneFlow)]
    [InlineData(ServiceCategory.AI)]
    public void SetCategoryTimeout_WithAllServiceCategories_SetsCorrectTimeout(ServiceCategory category)
    {
        // Arrange
        var options = new PollyResilienceExecutorOptions();
        var expectedTimeout = 123.45;

        // Act
        options.SetCategoryTimeout(category, expectedTimeout);

        // Assert
        Assert.Equal(expectedTimeout, options.CategoryTimeouts[category.ToString()]);
    }
}