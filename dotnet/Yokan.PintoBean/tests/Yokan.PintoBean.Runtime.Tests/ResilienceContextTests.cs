using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Unit tests for ResilienceContext functionality.
/// </summary>
public class ResilienceContextTests
{
    [Fact]
    public void Constructor_WithDefaultParameters_CreatesInstanceWithDefaults()
    {
        // Act
        var context = new ResilienceContext();

        // Assert
        Assert.Null(context.OperationName);
        Assert.Null(context.Metadata);
        Assert.Equal(CancellationToken.None, context.CancellationToken);
    }

    [Fact]
    public void Constructor_WithOperationName_SetsOperationName()
    {
        // Arrange
        var operationName = "test-operation";

        // Act
        var context = new ResilienceContext(operationName);

        // Assert
        Assert.Equal(operationName, context.OperationName);
        Assert.Null(context.Metadata);
        Assert.Equal(CancellationToken.None, context.CancellationToken);
    }

    [Fact]
    public void Constructor_WithMetadata_SetsMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };

        // Act
        var context = new ResilienceContext(metadata: metadata);

        // Assert
        Assert.Null(context.OperationName);
        Assert.Same(metadata, context.Metadata);
        Assert.Equal(CancellationToken.None, context.CancellationToken);
    }

    [Fact]
    public void Constructor_WithCancellationToken_SetsCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        var context = new ResilienceContext(cancellationToken: token);

        // Assert
        Assert.Null(context.OperationName);
        Assert.Null(context.Metadata);
        Assert.Equal(token, context.CancellationToken);
    }

    [Fact]
    public void Constructor_WithAllParameters_SetsAllProperties()
    {
        // Arrange
        var operationName = "test-operation";
        var metadata = new Dictionary<string, object> { ["key"] = "value" };
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        var context = new ResilienceContext(operationName, metadata, token);

        // Assert
        Assert.Equal(operationName, context.OperationName);
        Assert.Same(metadata, context.Metadata);
        Assert.Equal(token, context.CancellationToken);
    }

    [Fact]
    public void WithCancellationToken_CreatesNewInstanceWithNewToken()
    {
        // Arrange
        var operationName = "test-operation";
        var metadata = new Dictionary<string, object> { ["key"] = "value" };
        using var cts1 = new CancellationTokenSource();
        using var cts2 = new CancellationTokenSource();
        var token1 = cts1.Token;
        var token2 = cts2.Token;

        var originalContext = new ResilienceContext(operationName, metadata, token1);

        // Act
        var newContext = originalContext.WithCancellationToken(token2);

        // Assert
        Assert.NotSame(originalContext, newContext);
        Assert.Equal(operationName, newContext.OperationName);
        Assert.Same(metadata, newContext.Metadata);
        Assert.Equal(token2, newContext.CancellationToken);

        // Original context should be unchanged
        Assert.Equal(token1, originalContext.CancellationToken);
    }

    [Fact]
    public void WithMetadata_CreatesNewInstanceWithAdditionalMetadata()
    {
        // Arrange
        var operationName = "test-operation";
        var originalMetadata = new Dictionary<string, object> { ["key1"] = "value1" };
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var originalContext = new ResilienceContext(operationName, originalMetadata, token);

        // Act
        var newContext = originalContext.WithMetadata("key2", "value2");

        // Assert
        Assert.NotSame(originalContext, newContext);
        Assert.Equal(operationName, newContext.OperationName);
        Assert.Equal(token, newContext.CancellationToken);

        // New context should have both metadata entries
        Assert.NotNull(newContext.Metadata);
        Assert.Equal(2, newContext.Metadata.Count);
        Assert.Equal("value1", newContext.Metadata["key1"]);
        Assert.Equal("value2", newContext.Metadata["key2"]);

        // Original context should be unchanged
        Assert.Single(originalContext.Metadata!);
        Assert.Equal("value1", originalContext.Metadata!["key1"]);
    }

    [Fact]
    public void WithMetadata_WithNullOriginalMetadata_CreatesNewInstanceWithSingleMetadata()
    {
        // Arrange
        var originalContext = new ResilienceContext();

        // Act
        var newContext = originalContext.WithMetadata("key", "value");

        // Assert
        Assert.NotSame(originalContext, newContext);
        Assert.Null(originalContext.Metadata);
        
        Assert.NotNull(newContext.Metadata);
        Assert.Single(newContext.Metadata);
        Assert.Equal("value", newContext.Metadata["key"]);
    }

    [Fact]
    public void WithMetadata_OverwritesExistingKey()
    {
        // Arrange
        var originalMetadata = new Dictionary<string, object> { ["key"] = "original" };
        var originalContext = new ResilienceContext(metadata: originalMetadata);

        // Act
        var newContext = originalContext.WithMetadata("key", "new");

        // Assert
        Assert.NotSame(originalContext, newContext);
        
        // New context should have the updated value
        Assert.NotNull(newContext.Metadata);
        Assert.Single(newContext.Metadata);
        Assert.Equal("new", newContext.Metadata["key"]);

        // Original context should be unchanged
        Assert.Equal("original", originalContext.Metadata!["key"]);
    }

    [Fact]
    public void WithMetadata_ThrowsArgumentNullException_WhenKeyIsNull()
    {
        // Arrange
        var context = new ResilienceContext();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => context.WithMetadata(null!, "value"));
    }

    [Fact]
    public void WithMetadata_AllowsNullValue()
    {
        // Arrange
        var context = new ResilienceContext();

        // Act
        var newContext = context.WithMetadata("key", null!);

        // Assert
        Assert.NotNull(newContext.Metadata);
        Assert.Single(newContext.Metadata);
        Assert.Null(newContext.Metadata["key"]);
    }

    [Fact]
    public void WithCancellationToken_PreservesOtherProperties()
    {
        // Arrange
        var operationName = "test-operation";
        var metadata = new Dictionary<string, object> { ["key"] = "value" };
        using var cts1 = new CancellationTokenSource();
        using var cts2 = new CancellationTokenSource();
        var token1 = cts1.Token;
        var token2 = cts2.Token;

        var originalContext = new ResilienceContext(operationName, metadata, token1);

        // Act
        var newContext = originalContext.WithCancellationToken(token2);

        // Assert
        Assert.Equal(operationName, newContext.OperationName);
        Assert.Same(metadata, newContext.Metadata); // Should be the same reference
        Assert.Equal(token2, newContext.CancellationToken);
    }

    [Fact]
    public void WithMetadata_PreservesOtherProperties()
    {
        // Arrange
        var operationName = "test-operation";
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var originalContext = new ResilienceContext(operationName, null, token);

        // Act
        var newContext = originalContext.WithMetadata("key", "value");

        // Assert
        Assert.Equal(operationName, newContext.OperationName);
        Assert.Equal(token, newContext.CancellationToken);
        Assert.NotNull(newContext.Metadata);
        Assert.Single(newContext.Metadata);
        Assert.Equal("value", newContext.Metadata["key"]);
    }
}