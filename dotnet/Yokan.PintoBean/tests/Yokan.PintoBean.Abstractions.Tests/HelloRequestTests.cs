using Yokan.PintoBean.Abstractions;

namespace Yokan.PintoBean.Abstractions.Tests;

/// <summary>
/// Tests for HelloRequest DTO.
/// </summary>
public class HelloRequestTests
{
    [Fact]
    public void HelloRequest_WithRequiredName_ShouldCreate()
    {
        // Arrange & Act
        var request = new HelloRequest { Name = "World" };

        // Assert
        Assert.Equal("World", request.Name);
        Assert.Null(request.Language);
        Assert.Null(request.Context);
    }

    [Fact]
    public void HelloRequest_WithAllProperties_ShouldCreate()
    {
        // Arrange & Act
        var request = new HelloRequest
        {
            Name = "Alice",
            Language = "fr",
            Context = "formal"
        };

        // Assert
        Assert.Equal("Alice", request.Name);
        Assert.Equal("fr", request.Language);
        Assert.Equal("formal", request.Context);
    }

    [Fact]
    public void HelloRequest_RecordEquality_ShouldWork()
    {
        // Arrange
        var request1 = new HelloRequest { Name = "Bob", Language = "en" };
        var request2 = new HelloRequest { Name = "Bob", Language = "en" };
        var request3 = new HelloRequest { Name = "Charlie", Language = "en" };

        // Act & Assert
        Assert.Equal(request1, request2);
        Assert.NotEqual(request1, request3);
    }
}
